using Microsoft.AspNetCore.Mvc;
using StudentAssessment.Application.DTOs;
using StudentAssessment.Application.Interfaces;
using StudentAssessment.Core.Entities;

namespace StudentAssessment.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MarksController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public MarksController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Query marks with filters
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<MarkResponse>>> GetMarks([FromQuery] Guid? studentId, [FromQuery] Guid? examId, [FromQuery] string? subjectCode, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
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
        public async Task<ActionResult<MarkResponse>> GetById(Guid id)
        {
            var mark = await _unitOfWork.Repository<Mark>().GetByIdAsync(id);
            if (mark == null)
                return NotFound(new { message = "Mark not found" });

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

                await _unitOfWork.BeginTransactionAsync();

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

                // Check if mark already exists
                var existingMark = await _unitOfWork.Repository<Mark>().FirstOrDefaultAsync(m =>
                    m.StudentId == request.StudentId &&
                    m.SubjectCode == request.SubjectCode &&
                    m.ExamId == request.ExamId
                );

                if (existingMark != null)
                {
                    // Update existing mark
                    existingMark.Score = request.Score;
                    existingMark.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Repository<Mark>().UpdateAsync(existingMark);
                    await _unitOfWork.CommitAsync();

                    return AcceptedAtAction(nameof(GetById), new { id = existingMark.Id }, new
                    {
                        id = existingMark.Id,
                        status = "updated",
                        message = "Mark submitted for processing"
                    });
                }

                // Create new mark
                var mark = new Mark
                {
                    Id = Guid.NewGuid(),
                    StudentId = request.StudentId,
                    SubjectCode = request.SubjectCode,
                    ExamId = request.ExamId,
                    Score = request.Score,
                    IdempotencyKey = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdMark = await _unitOfWork.Repository<Mark>().AddAsync(mark);
                await _unitOfWork.CommitAsync();

                // In a real scenario, you would queue this for async processing
                // For now, we're returning 202 Accepted to indicate async processing
                return AcceptedAtAction(nameof(GetById), new { id = createdMark.Id }, new
                {
                    id = createdMark.Id,
                    status = "pending",
                    message = "Mark submitted for processing"
                });
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred", error = ex.Message });
            }
        }

        /// <summary>
        /// Update a mark
        /// </summary>
        [HttpPut("{id}")]
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
        public async Task<ActionResult<IEnumerable<MarkResponse>>> GetStudentExamMarks(Guid studentId, Guid examId)
        {
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
    }
}
