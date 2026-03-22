using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentAssessment.Application.DTOs;
using StudentAssessment.Application.Interfaces;
using StudentAssessment.Core.Entities;
using StudentAssessment.Core.Enums;

namespace StudentAssessment.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExamsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ExamsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Teacher")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ExamResponse>>> GetAll()
    {
        var exams = await _unitOfWork.Repository<Exam>().GetAllAsync();
        var response = exams
            .OrderBy(e => e.Name)
            .Select(MapExamResponse);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExamDetailResponse>> GetById(Guid id)
    {
        var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(id);
        if (exam == null)
        {
            return NotFound(new { message = "Exam not found" });
        }

        return Ok(new ExamDetailResponse
        {
            Id = exam.Id,
            Name = exam.Name,
            Type = exam.Type.ToString(),
            ClassId = exam.ClassId,
            MarksCount = exam.Marks.Count,
            RankingsCount = exam.Rankings.Count
        });
    }

    [HttpGet("class/{classId:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ExamResponse>>> GetByClass(Guid classId)
    {
        var existingClass = await _unitOfWork.Repository<Class>().GetByIdAsync(classId);
        if (existingClass == null)
        {
            return NotFound(new { message = "Class not found" });
        }

        var exams = await _unitOfWork.Repository<Exam>().FindAsync(e => e.ClassId == classId);
        var response = exams
            .OrderBy(e => e.Name)
            .Select(MapExamResponse);

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExamResponse>> Create([FromBody] CreateExamRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Exam name is required" });
        }

        if (request.ClassId == Guid.Empty)
        {
            return BadRequest(new { message = "Class ID is required" });
        }

        if (!Enum.TryParse<ExamType>(request.Type, ignoreCase: true, out var examType))
        {
            return BadRequest(new { message = "Invalid exam type. Must be Quarterly, HalfYearly, or Final" });
        }

        var existingClass = await _unitOfWork.Repository<Class>().GetByIdAsync(request.ClassId);
        if (existingClass == null)
        {
            return NotFound(new { message = "Class not found" });
        }

        var exam = new Exam
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Type = examType,
            ClassId = request.ClassId
        };

        var createdExam = await _unitOfWork.Repository<Exam>().AddAsync(exam);

        return CreatedAtAction(nameof(GetById), new { id = createdExam.Id }, MapExamResponse(createdExam));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExamResponse>> Update(Guid id, [FromBody] UpdateExamRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Exam name is required" });
        }

        if (request.ClassId == Guid.Empty)
        {
            return BadRequest(new { message = "Class ID is required" });
        }

        if (!Enum.TryParse<ExamType>(request.Type, ignoreCase: true, out var examType))
        {
            return BadRequest(new { message = "Invalid exam type. Must be Quarterly, HalfYearly, or Final" });
        }

        var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(id);
        if (exam == null)
        {
            return NotFound(new { message = "Exam not found" });
        }

        var existingClass = await _unitOfWork.Repository<Class>().GetByIdAsync(request.ClassId);
        if (existingClass == null)
        {
            return NotFound(new { message = "Class not found" });
        }

        exam.Name = request.Name.Trim();
        exam.Type = examType;
        exam.ClassId = request.ClassId;

        var updatedExam = await _unitOfWork.Repository<Exam>().UpdateAsync(exam);
        return Ok(MapExamResponse(updatedExam));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(id);
        if (exam == null)
        {
            return NotFound(new { message = "Exam not found" });
        }

        await _unitOfWork.Repository<Exam>().DeleteAsync(exam);
        return NoContent();
    }

    private static ExamResponse MapExamResponse(Exam exam)
    {
        return new ExamResponse
        {
            Id = exam.Id,
            Name = exam.Name,
            Type = exam.Type.ToString(),
            ClassId = exam.ClassId
        };
    }
}
