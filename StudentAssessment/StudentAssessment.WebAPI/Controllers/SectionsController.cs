using Microsoft.AspNetCore.Mvc;
using StudentAssessment.Application.DTOs;
using StudentAssessment.Application.Interfaces;
using StudentAssessment.Core.Entities;

namespace StudentAssessment.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SectionsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public SectionsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Get all sections for a class
        /// </summary>
        [HttpGet("class/{classId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<SectionResponse>>> GetByClass(Guid classId)
        {
            // Verify class exists
            var @class = await _unitOfWork.Repository<Class>().GetByIdAsync(classId);
            if (@class == null)
                return NotFound(new { message = "Class not found" });

            var sections = await _unitOfWork.Repository<Section>().FindAsync(s => s.ClassId == classId);
            var response = sections.Select(s => new SectionResponse
            {
                Id = s.Id,
                ClassId = s.ClassId,
                Name = s.Name
            });

            return Ok(response);
        }

        /// <summary>
        /// Get a section by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SectionDetailResponse>> GetById(Guid id)
        {
            var section = await _unitOfWork.Repository<Section>().GetByIdAsync(id);
            if (section == null)
                return NotFound(new { message = "Section not found" });

            var response = new SectionDetailResponse
            {
                Id = section.Id,
                ClassId = section.ClassId,
                Name = section.Name,
                StudentCount = section.Students?.Count ?? 0,
                TeacherCount = section.TeacherSections?.Count ?? 0
            };

            return Ok(response);
        }

        /// <summary>
        /// Create a new section
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SectionResponse>> Create([FromBody] CreateSectionRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest(new { message = "Section name is required" });

                if (request.ClassId == Guid.Empty)
                    return BadRequest(new { message = "Class ID is required" });

                await _unitOfWork.BeginTransactionAsync();

                // Verify class exists
                var @class = await _unitOfWork.Repository<Class>().GetByIdAsync(request.ClassId);
                if (@class == null)
                    return NotFound(new { message = "Class not found" });

                var section = new Section
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    ClassId = request.ClassId
                };

                var createdSection = await _unitOfWork.Repository<Section>().AddAsync(section);

                await _unitOfWork.CommitAsync();

                var response = new SectionResponse
                {
                    Id = createdSection.Id,
                    ClassId = createdSection.ClassId,
                    Name = createdSection.Name
                };

                return CreatedAtAction(nameof(GetById), new { id = createdSection.Id }, response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred", error = ex.Message });
            }
        }

        /// <summary>
        /// Update a section
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SectionResponse>> Update(Guid id, [FromBody] UpdateSectionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Section name is required" });

            var section = await _unitOfWork.Repository<Section>().GetByIdAsync(id);
            if (section == null)
                return NotFound(new { message = "Section not found" });

            section.Name = request.Name;
            var updatedSection = await _unitOfWork.Repository<Section>().UpdateAsync(section);

            var response = new SectionResponse
            {
                Id = updatedSection.Id,
                ClassId = updatedSection.ClassId,
                Name = updatedSection.Name
            };

            return Ok(response);
        }

        /// <summary>
        /// Delete a section
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

                var section = await _unitOfWork.Repository<Section>().GetByIdAsync(id);
                if (section == null)
                    return NotFound(new { message = "Section not found" });

                await _unitOfWork.Repository<Section>().DeleteAsync(section);
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
