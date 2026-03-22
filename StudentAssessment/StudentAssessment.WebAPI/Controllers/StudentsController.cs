using Microsoft.AspNetCore.Mvc;
using StudentAssessment.Application.DTOs;
using StudentAssessment.Application.Interfaces;
using StudentAssessment.Core.Entities;

namespace StudentAssessment.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public StudentsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Get all students
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<StudentResponse>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var (students, total) = await _unitOfWork.Repository<Student>().GetPagedAsync(pageNumber, pageSize);
            
            var response = students.Select(s => new StudentResponse
            {
                Id = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName,
                ClassId = s.ClassId,
                SectionId = s.SectionId,
                UserId = s.UserId,
                CreatedAt = s.CreatedAt
            });

            return Ok(new { items = response, total, pageNumber, pageSize });
        }

        /// <summary>
        /// Get a student by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StudentDetailResponse>> GetById(Guid id)
        {
            var student = await _unitOfWork.Repository<Student>().GetByIdAsync(id);
            if (student == null)
                return NotFound(new { message = "Student not found" });

            var response = new StudentDetailResponse
            {
                Id = student.Id,
                FirstName = student.FirstName,
                LastName = student.LastName,
                ClassId = student.ClassId,
                SectionId = student.SectionId,
                UserId = student.UserId,
                CreatedAt = student.CreatedAt,
                MarksCount = student.Marks?.Count ?? 0
            };

            return Ok(response);
        }

        /// <summary>
        /// Create a new student
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<StudentResponse>> Create([FromBody] CreateStudentRequest request)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(request.FirstName))
                    return BadRequest(new { message = "First name is required" });

                if (string.IsNullOrWhiteSpace(request.LastName))
                    return BadRequest(new { message = "Last name is required" });

                if (request.ClassId == Guid.Empty)
                    return BadRequest(new { message = "Class ID is required" });

                if (request.SectionId == Guid.Empty)
                    return BadRequest(new { message = "Section ID is required" });

                await _unitOfWork.BeginTransactionAsync();

                // Verify class and section exist
                var @class = await _unitOfWork.Repository<Class>().GetByIdAsync(request.ClassId);
                if (@class == null)
                    return NotFound(new { message = "Class not found" });

                var section = await _unitOfWork.Repository<Section>().GetByIdAsync(request.SectionId);
                if (section == null)
                    return NotFound(new { message = "Section not found" });

                // Verify section belongs to class
                if (section.ClassId != request.ClassId)
                    return BadRequest(new { message = "Section does not belong to the specified class" });

                // If UserId is provided, verify user exists
                if (request.UserId.HasValue && request.UserId != Guid.Empty)
                {
                    var user = await _unitOfWork.Repository<User>().GetByIdAsync(request.UserId.Value);
                    if (user == null)
                        return NotFound(new { message = "User not found" });
                }

                var student = new Student
                {
                    Id = Guid.NewGuid(),
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    ClassId = request.ClassId,
                    SectionId = request.SectionId,
                    UserId = request.UserId,
                    CreatedAt = DateTime.UtcNow
                };

                var createdStudent = await _unitOfWork.Repository<Student>().AddAsync(student);
                await _unitOfWork.CommitAsync();

                var response = new StudentResponse
                {
                    Id = createdStudent.Id,
                    FirstName = createdStudent.FirstName,
                    LastName = createdStudent.LastName,
                    ClassId = createdStudent.ClassId,
                    SectionId = createdStudent.SectionId,
                    UserId = createdStudent.UserId,
                    CreatedAt = createdStudent.CreatedAt
                };

                return CreatedAtAction(nameof(GetById), new { id = createdStudent.Id }, response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred", error = ex.Message });
            }
        }

        /// <summary>
        /// Update a student
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<StudentResponse>> Update(Guid id, [FromBody] UpdateStudentRequest request)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(request.FirstName))
                    return BadRequest(new { message = "First name is required" });

                if (string.IsNullOrWhiteSpace(request.LastName))
                    return BadRequest(new { message = "Last name is required" });

                if (request.ClassId == Guid.Empty)
                    return BadRequest(new { message = "Class ID is required" });

                if (request.SectionId == Guid.Empty)
                    return BadRequest(new { message = "Section ID is required" });

                await _unitOfWork.BeginTransactionAsync();

                var student = await _unitOfWork.Repository<Student>().GetByIdAsync(id);
                if (student == null)
                    return NotFound(new { message = "Student not found" });

                // Verify class and section exist
                var @class = await _unitOfWork.Repository<Class>().GetByIdAsync(request.ClassId);
                if (@class == null)
                    return NotFound(new { message = "Class not found" });

                var section = await _unitOfWork.Repository<Section>().GetByIdAsync(request.SectionId);
                if (section == null)
                    return NotFound(new { message = "Section not found" });

                // Verify section belongs to class
                if (section.ClassId != request.ClassId)
                    return BadRequest(new { message = "Section does not belong to the specified class" });

                // If UserId is provided, verify user exists
                if (request.UserId.HasValue && request.UserId != Guid.Empty)
                {
                    var user = await _unitOfWork.Repository<User>().GetByIdAsync(request.UserId.Value);
                    if (user == null)
                        return NotFound(new { message = "User not found" });
                }

                student.FirstName = request.FirstName;
                student.LastName = request.LastName;
                student.ClassId = request.ClassId;
                student.SectionId = request.SectionId;
                student.UserId = request.UserId;

                var updatedStudent = await _unitOfWork.Repository<Student>().UpdateAsync(student);
                await _unitOfWork.CommitAsync();

                var response = new StudentResponse
                {
                    Id = updatedStudent.Id,
                    FirstName = updatedStudent.FirstName,
                    LastName = updatedStudent.LastName,
                    ClassId = updatedStudent.ClassId,
                    SectionId = updatedStudent.SectionId,
                    UserId = updatedStudent.UserId,
                    CreatedAt = updatedStudent.CreatedAt
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
        /// Delete a student
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var student = await _unitOfWork.Repository<Student>().GetByIdAsync(id);
                if (student == null)
                    return NotFound(new { message = "Student not found" });

                await _unitOfWork.Repository<Student>().DeleteAsync(student);
                await _unitOfWork.CommitAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred", error = ex.Message });
            }
        }
    }
}
