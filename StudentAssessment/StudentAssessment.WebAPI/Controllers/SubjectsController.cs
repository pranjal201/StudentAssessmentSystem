using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentAssessment.Application.DTOs;
using StudentAssessment.Application.Interfaces;
using StudentAssessment.Core.Entities;

namespace StudentAssessment.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubjectsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public SubjectsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Teacher")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SubjectResponse>>> GetAll()
    {
        var subjects = await _unitOfWork.Repository<Subject>().GetAllAsync();
        var response = subjects
            .OrderBy(s => s.Code)
            .Select(s => new SubjectResponse
            {
                Code = s.Code,
                Name = s.Name
            });

        return Ok(response);
    }

    [HttpGet("{code}")]
    [Authorize(Roles = "Admin,Teacher")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectResponse>> GetByCode(string code)
    {
        var subject = await _unitOfWork.Repository<Subject>().FirstOrDefaultAsync(s => s.Code == code);
        if (subject == null)
        {
            return NotFound(new { message = "Subject not found" });
        }

        return Ok(new SubjectResponse
        {
            Code = subject.Code,
            Name = subject.Name
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubjectResponse>> Create([FromBody] CreateSubjectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new { message = "Subject code is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Subject name is required" });
        }

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var existingSubject = await _unitOfWork.Repository<Subject>().FirstOrDefaultAsync(s => s.Code == normalizedCode);
        if (existingSubject != null)
        {
            return BadRequest(new { message = "Subject code already exists" });
        }

        var subject = new Subject
        {
            Code = normalizedCode,
            Name = request.Name.Trim()
        };

        var createdSubject = await _unitOfWork.Repository<Subject>().AddAsync(subject);

        return CreatedAtAction(nameof(GetByCode), new { code = createdSubject.Code }, new SubjectResponse
        {
            Code = createdSubject.Code,
            Name = createdSubject.Name
        });
    }

    [HttpPut("{code}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectResponse>> Update(string code, [FromBody] UpdateSubjectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Subject name is required" });
        }

        var subject = await _unitOfWork.Repository<Subject>().FirstOrDefaultAsync(s => s.Code == code);
        if (subject == null)
        {
            return NotFound(new { message = "Subject not found" });
        }

        subject.Name = request.Name.Trim();
        var updatedSubject = await _unitOfWork.Repository<Subject>().UpdateAsync(subject);

        return Ok(new SubjectResponse
        {
            Code = updatedSubject.Code,
            Name = updatedSubject.Name
        });
    }

    [HttpDelete("{code}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string code)
    {
        var subject = await _unitOfWork.Repository<Subject>().FirstOrDefaultAsync(s => s.Code == code);
        if (subject == null)
        {
            return NotFound(new { message = "Subject not found" });
        }

        await _unitOfWork.Repository<Subject>().DeleteAsync(subject);
        return NoContent();
    }
}
