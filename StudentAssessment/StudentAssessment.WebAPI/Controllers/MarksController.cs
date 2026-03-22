using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using StudentAssessment.Application.DTOs;
using StudentAssessment.Application.Interfaces;
using StudentAssessment.Core.Entities;
using StudentAssessment.Core.Enums;

namespace StudentAssessment.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MarksController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMarkSubmissionQueue _submissionQueue;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        public MarksController(IUnitOfWork unitOfWork, IMarkSubmissionQueue submissionQueue)
        {
            _unitOfWork = unitOfWork;
            _submissionQueue = submissionQueue;
        }

        /// <summary>
        /// Query marks with filters
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<MarkResponse>>> GetMarks([FromQuery] Guid? studentId, [FromQuery] Guid? examId, [FromQuery] string? subjectCode, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (HttpContext.User.IsInRole("Student"))
            {
                var currentStudent = await GetCurrentStudentAsync();
                if (currentStudent == null)
                    return Forbid();

                if (studentId.HasValue && studentId.Value != currentStudent.Id)
                    return Forbid();

                studentId = currentStudent.Id;
            }

            var predicate = (System.Linq.Expressions.Expression<System.Func<Mark, bool>>)(m =>
                (studentId == null || m.StudentId == studentId) &&
                (examId == null || m.ExamId == examId) &&
                (string.IsNullOrEmpty(subjectCode) || m.SubjectCode == subjectCode)
            );

            var (marks, total) = await _unitOfWork.Repository<Mark>().GetPagedAsync(pageNumber, pageSize, predicate);

            var response = marks.Select(m => new MarkResponse
            {
                Id = m.Id,
                StudentId = m.StudentId,
                SubjectCode = m.SubjectCode,
                ExamId = m.ExamId,
                Score = m.Score,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            });

            return Ok(new { items = response, total, pageNumber, pageSize });
        }

        /// <summary>
        /// Get a mark by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<MarkResponse>> GetById(Guid id)
        {
            var mark = await _unitOfWork.Repository<Mark>().GetByIdAsync(id);
            if (mark == null)
                return NotFound(new { message = "Mark not found" });

            if (!await CanAccessStudentAsync(mark.StudentId))
                return Forbid();

            var response = new MarkResponse
            {
                Id = mark.Id,
                StudentId = mark.StudentId,
                SubjectCode = mark.SubjectCode,
                ExamId = mark.ExamId,
                Score = mark.Score,
                CreatedAt = mark.CreatedAt,
                UpdatedAt = mark.UpdatedAt
            };

            return Ok(response);
        }

        /// <summary>
        /// Submit marks (async processing)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Teacher")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> SubmitMarks([FromBody] CreateMarkRequest request)
        {
            try
            {
                // Validation
                if (request.StudentId == Guid.Empty)
                    return BadRequest(new { message = "Student ID is required" });

                if (string.IsNullOrWhiteSpace(request.SubjectCode))
                    return BadRequest(new { message = "Subject code is required" });

                if (request.ExamId == Guid.Empty)
                    return BadRequest(new { message = "Exam ID is required" });

                if (request.Score < 0)
                    return BadRequest(new { message = "Score cannot be negative" });

                // Verify student exists
                var student = await _unitOfWork.Repository<Student>().GetByIdAsync(request.StudentId);
                if (student == null)
                    return NotFound(new { message = "Student not found" });

                // Verify subject exists
                var subject = await _unitOfWork.Repository<Subject>().FirstOrDefaultAsync(s => s.Code == request.SubjectCode);
                if (subject == null)
                    return NotFound(new { message = "Subject not found" });

                // Verify exam exists
                var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(request.ExamId);
                if (exam == null)
                    return NotFound(new { message = "Exam not found" });

                if (student.ClassId != exam.ClassId)
                {
                    return BadRequest(new { message = "Exam does not belong to the student's class" });
                }

                var correlationId = string.IsNullOrWhiteSpace(request.RequestId)
                    ? Guid.NewGuid().ToString()
                    : request.RequestId.Trim();

                var existingJob = await _unitOfWork.Repository<MarkSubmission>()
                    .FirstOrDefaultAsync(j => j.CorrelationId == correlationId);

                if (existingJob != null)
                {
                    var existingQueued = _submissionQueue.TryQueue(existingJob.Id);
                    return AcceptedAtAction(nameof(GetJobStatus), new { jobId = existingJob.Id }, MapJobResponse(existingJob, existingQueued));
                }

                var job = new MarkSubmission
                {
                    Id = Guid.NewGuid(),
                    Payload = JsonSerializer.Serialize(request, _jsonOptions),
                    Status = JobStatus.Pending,
                    RetryCount = 0,
                    CorrelationId = correlationId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<MarkSubmission>().AddAsync(job);
                var queuedInMemory = _submissionQueue.TryQueue(job.Id);

                return AcceptedAtAction(nameof(GetJobStatus), new { jobId = job.Id }, MapJobResponse(job, queuedInMemory));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred", error = ex.Message });
            }
        }

        [HttpGet("jobs/{jobId:guid}")]
        [Authorize(Roles = "Teacher")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MarkSubmissionResponse>> GetJobStatus(Guid jobId)
        {
            var job = await _unitOfWork.Repository<MarkSubmission>().GetByIdAsync(jobId);
            if (job == null)
                return NotFound(new { message = "Mark submission job not found" });

            return Ok(MapJobResponse(job, false));
        }

        /// <summary>
        /// Update a mark
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Teacher,Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MarkResponse>> Update(Guid id, [FromBody] CreateMarkRequest request)
        {
            try
            {
                if (request.Score < 0)
                    return BadRequest(new { message = "Score cannot be negative" });

                await _unitOfWork.BeginTransactionAsync();

                var mark = await _unitOfWork.Repository<Mark>().GetByIdAsync(id);
                if (mark == null)
                    return NotFound(new { message = "Mark not found" });

                // Verify subject and exam still exist
                var subject = await _unitOfWork.Repository<Subject>().FirstOrDefaultAsync(s => s.Code == request.SubjectCode);
                if (subject == null)
                    return NotFound(new { message = "Subject not found" });

                var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(request.ExamId);
                if (exam == null)
                    return NotFound(new { message = "Exam not found" });

                mark.Score = request.Score;
                mark.UpdatedAt = DateTime.UtcNow;

                var updatedMark = await _unitOfWork.Repository<Mark>().UpdateAsync(mark);
                await _unitOfWork.CommitAsync();

                var response = new MarkResponse
                {
                    Id = updatedMark.Id,
                    StudentId = updatedMark.StudentId,
                    SubjectCode = updatedMark.SubjectCode,
                    ExamId = updatedMark.ExamId,
                    Score = updatedMark.Score,
                    CreatedAt = updatedMark.CreatedAt,
                    UpdatedAt = updatedMark.UpdatedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a mark
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var mark = await _unitOfWork.Repository<Mark>().GetByIdAsync(id);
            if (mark == null)
                return NotFound(new { message = "Mark not found" });

            await _unitOfWork.Repository<Mark>().DeleteAsync(mark);
            return NoContent();
        }

        /// <summary>
        /// Get marks for a student in an exam
        /// </summary>
        [HttpGet("student/{studentId}/exam/{examId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<MarkResponse>>> GetStudentExamMarks(Guid studentId, Guid examId)
        {
            if (!await CanAccessStudentAsync(studentId))
                return Forbid();

            // Verify student exists
            var student = await _unitOfWork.Repository<Student>().GetByIdAsync(studentId);
            if (student == null)
                return NotFound(new { message = "Student not found" });

            // Verify exam exists
            var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);
            if (exam == null)
                return NotFound(new { message = "Exam not found" });

            var marks = await _unitOfWork.Repository<Mark>().FindAsync(m =>
                m.StudentId == studentId && m.ExamId == examId
            );

            var response = marks.Select(m => new MarkResponse
            {
                Id = m.Id,
                StudentId = m.StudentId,
                SubjectCode = m.SubjectCode,
                ExamId = m.ExamId,
                Score = m.Score,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            });

            return Ok(response);
        }

        private string? GetCurrentUserId()
        {
            return HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        private async Task<Student?> GetCurrentStudentAsync()
        {
            var currentUserId = GetCurrentUserId();
            if (!Guid.TryParse(currentUserId, out var userId))
                return null;

            return await _unitOfWork.Repository<Student>().FirstOrDefaultAsync(s => s.UserId == userId);
        }

        private async Task<bool> CanAccessStudentAsync(Guid studentId)
        {
            if (!HttpContext.User.IsInRole("Student"))
                return true;

            var currentStudent = await GetCurrentStudentAsync();
            return currentStudent != null && currentStudent.Id == studentId;
        }

        private static MarkSubmissionResponse MapJobResponse(MarkSubmission job, bool queuedInMemory)
        {
            return new MarkSubmissionResponse
            {
                JobId = job.Id,
                Status = job.Status.ToString(),
                CorrelationId = job.CorrelationId,
                RetryCount = job.RetryCount,
                CreatedAt = job.CreatedAt,
                ProcessedAt = job.ProcessedAt,
                NextRetryAt = job.NextRetryAt,
                QueuedInMemory = queuedInMemory
            };
        }
    }
}
