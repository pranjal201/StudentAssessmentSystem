using Microsoft.AspNetCore.Mvc;
using StudentAssessment.Application.DTOs;
using StudentAssessment.Application.Interfaces;
using StudentAssessment.Core.Entities;

namespace StudentAssessment.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClassesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public ClassesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Get all classes
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ClassResponse>>> GetAll()
        {
            var classes = await _unitOfWork.Repository<Class>().GetAllAsync();
            var response = classes.Select(c => new ClassResponse { Id = c.Id, Name = c.Name });
            return Ok(response);
        }

        /// <summary>
        /// Get a class by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClassDetailResponse>> GetById(Guid id)
        {
            var @class = await _unitOfWork.Repository<Class>().GetByIdAsync(id);
            if (@class == null)
                return NotFound(new { message = "Class not found" });

            var response = new ClassDetailResponse
            {
                Id = @class.Id,
                Name = @class.Name,
                SectionCount = @class.Sections?.Count ?? 0,
                StudentCount = @class.Students?.Count ?? 0
            };

            return Ok(response);
        }

        /// <summary>
        /// Create a new class
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ClassResponse>> Create([FromBody] CreateClassRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Class name is required" });

            var @class = new Class { Id = Guid.NewGuid(), Name = request.Name };
            var createdClass = await _unitOfWork.Repository<Class>().AddAsync(@class);

            var response = new ClassResponse { Id = createdClass.Id, Name = createdClass.Name };
            return CreatedAtAction(nameof(GetById), new { id = createdClass.Id }, response);
        }

        /// <summary>
        /// Update a class
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ClassResponse>> Update(Guid id, [FromBody] UpdateClassRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Class name is required" });

            var @class = await _unitOfWork.Repository<Class>().GetByIdAsync(id);
            if (@class == null)
                return NotFound(new { message = "Class not found" });

            @class.Name = request.Name;
            var updatedClass = await _unitOfWork.Repository<Class>().UpdateAsync(@class);

            var response = new ClassResponse { Id = updatedClass.Id, Name = updatedClass.Name };
            return Ok(response);
        }

        /// <summary>
        /// Delete a class
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

                var @class = await _unitOfWork.Repository<Class>().GetByIdAsync(id);
                if (@class == null)
                    return NotFound(new { message = "Class not found" });

                await _unitOfWork.Repository<Class>().DeleteAsync(@class);
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
