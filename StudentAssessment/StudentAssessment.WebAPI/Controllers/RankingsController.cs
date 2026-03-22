using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentAssessment.Application.DTOs;
using StudentAssessment.Application.Interfaces;
using StudentAssessment.Core.Entities;

namespace StudentAssessment.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RankingsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public RankingsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Get class-wide ranking for an exam
        /// </summary>
        [HttpGet("class/{classId}/exam/{examId}")]
        [Authorize(Roles = "Admin,Teacher")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClassRankingResponse>> GetClassRanking(Guid classId, Guid examId)
        {
            // Verify class exists
            var @class = await _unitOfWork.Repository<Class>().GetByIdAsync(classId);
            if (@class == null)
                return NotFound(new { message = "Class not found" });

            // Verify exam exists
            var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);
            if (exam == null)
                return NotFound(new { message = "Exam not found" });

            // Get all rankings for this class and exam
            var rankings = await _unitOfWork.Repository<Ranking>().FindAsync(r =>
                r.ExamId == examId
            );

            var classStudents = await _unitOfWork.Repository<Student>().FindAsync(s => s.ClassId == classId);
            var studentIds = classStudents.Select(s => s.Id).ToList();

            var classRankings = rankings.Where(r => studentIds.Contains(r.StudentId)).ToList();

            var response = new ClassRankingResponse
            {
                ClassId = classId,
                ClassName = @class.Name,
                ExamId = examId,
                ExamName = exam.Name,
                Rankings = new List<RankingResponse>()
            };

            // Build ranking responses
            foreach (var ranking in classRankings.OrderBy(r => r.ClassRank))
            {
                var student = classStudents.FirstOrDefault(s => s.Id == ranking.StudentId);
                if (student != null)
                {
                    response.Rankings.Add(new RankingResponse
                    {
                        StudentId = ranking.StudentId,
                        StudentName = $"{student.FirstName} {student.LastName}",
                        ExamId = ranking.ExamId,
                        TotalMarks = ranking.TotalMarks,
                        SectionRank = ranking.SectionRank,
                        ClassRank = ranking.ClassRank,
                        UpdatedAt = ranking.UpdatedAt
                    });
                }
            }

            return Ok(response);
        }

        /// <summary>
        /// Get section-wise ranking for an exam
        /// </summary>
        [HttpGet("section/{sectionId}/exam/{examId}")]
        [Authorize(Roles = "Admin,Teacher")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SectionRankingResponse>> GetSectionRanking(Guid sectionId, Guid examId)
        {
            // Verify section exists
            var section = await _unitOfWork.Repository<Section>().GetByIdAsync(sectionId);
            if (section == null)
                return NotFound(new { message = "Section not found" });

            // Verify class exists
            var @class = await _unitOfWork.Repository<Class>().GetByIdAsync(section.ClassId);
            if (@class == null)
                return NotFound(new { message = "Class not found" });

            // Verify exam exists
            var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);
            if (exam == null)
                return NotFound(new { message = "Exam not found" });

            // Get all rankings for this exam
            var rankings = await _unitOfWork.Repository<Ranking>().FindAsync(r =>
                r.ExamId == examId
            );

            var sectionStudents = await _unitOfWork.Repository<Student>().FindAsync(s => s.SectionId == sectionId);
            var studentIds = sectionStudents.Select(s => s.Id).ToList();

            var sectionRankings = rankings.Where(r => studentIds.Contains(r.StudentId)).ToList();

            var response = new SectionRankingResponse
            {
                SectionId = sectionId,
                SectionName = section.Name,
                ClassId = section.ClassId,
                ClassName = @class.Name,
                ExamId = examId,
                ExamName = exam.Name,
                Rankings = new List<RankingResponse>()
            };

            // Build ranking responses
            foreach (var ranking in sectionRankings.OrderBy(r => r.SectionRank))
            {
                var student = sectionStudents.FirstOrDefault(s => s.Id == ranking.StudentId);
                if (student != null)
                {
                    response.Rankings.Add(new RankingResponse
                    {
                        StudentId = ranking.StudentId,
                        StudentName = $"{student.FirstName} {student.LastName}",
                        ExamId = ranking.ExamId,
                        TotalMarks = ranking.TotalMarks,
                        SectionRank = ranking.SectionRank,
                        ClassRank = ranking.ClassRank,
                        UpdatedAt = ranking.UpdatedAt
                    });
                }
            }

            return Ok(response);
        }

        /// <summary>
        /// Get top N students in a class for an exam
        /// </summary>
        [HttpGet("class/{classId}/exam/{examId}/top/{n}")]
        [Authorize(Roles = "Admin,Teacher")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ClassRankingResponse>> GetTopStudentsInClass(Guid classId, Guid examId, int n)
        {
            if (n <= 0)
                return BadRequest(new { message = "N must be greater than 0" });

            // Verify class exists
            var @class = await _unitOfWork.Repository<Class>().GetByIdAsync(classId);
            if (@class == null)
                return NotFound(new { message = "Class not found" });

            // Verify exam exists
            var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);
            if (exam == null)
                return NotFound(new { message = "Exam not found" });

            // Get all rankings for this class and exam
            var rankings = await _unitOfWork.Repository<Ranking>().FindAsync(r =>
                r.ExamId == examId
            );

            var classStudents = await _unitOfWork.Repository<Student>().FindAsync(s => s.ClassId == classId);
            var studentIds = classStudents.Select(s => s.Id).ToList();

            var classRankings = rankings
                .Where(r => studentIds.Contains(r.StudentId))
                .OrderBy(r => r.ClassRank)
                .Take(n)
                .ToList();

            var response = new ClassRankingResponse
            {
                ClassId = classId,
                ClassName = @class.Name,
                ExamId = examId,
                ExamName = exam.Name,
                Rankings = new List<RankingResponse>()
            };

            // Build ranking responses
            foreach (var ranking in classRankings)
            {
                var student = classStudents.FirstOrDefault(s => s.Id == ranking.StudentId);
                if (student != null)
                {
                    response.Rankings.Add(new RankingResponse
                    {
                        StudentId = ranking.StudentId,
                        StudentName = $"{student.FirstName} {student.LastName}",
                        ExamId = ranking.ExamId,
                        TotalMarks = ranking.TotalMarks,
                        SectionRank = ranking.SectionRank,
                        ClassRank = ranking.ClassRank,
                        UpdatedAt = ranking.UpdatedAt
                    });
                }
            }

            return Ok(response);
        }

        /// <summary>
        /// Get a student's rank in an exam
        /// </summary>
        [HttpGet("student/{studentId}/exam/{examId}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<StudentRankingResponse>> GetStudentRank(Guid studentId, Guid examId)
        {
            // Authorization: Student can only view own ranking, Admin/Teacher can view any
            var currentUserId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = HttpContext.User.IsInRole("Admin");
            var isTeacher = HttpContext.User.IsInRole("Teacher");

            // Verify student exists
            var student = await _unitOfWork.Repository<Student>().GetByIdAsync(studentId);
            if (student == null)
                return NotFound(new { message = "Student not found" });

            // Check authorization for students
            if (!isAdmin && !isTeacher)
            {
                // Student can only view their own ranking
                if (student.UserId.ToString() != currentUserId)
                {
                    return Forbid();
                }
            }

            // Verify exam exists
            var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);
            if (exam == null)
                return NotFound(new { message = "Exam not found" });

            // Get ranking
            var ranking = (await _unitOfWork.Repository<Ranking>().FindAsync(r =>
                r.StudentId == studentId && r.ExamId == examId
            )).FirstOrDefault();

            if (ranking == null)
                return NotFound(new { message = "Ranking not found for this student and exam" });

            // Get total students in section and class for context
            var sectionStudents = await _unitOfWork.Repository<Student>().FindAsync(s => s.SectionId == student.SectionId);
            var classStudents = await _unitOfWork.Repository<Student>().FindAsync(s => s.ClassId == student.ClassId);

            var response = new StudentRankingResponse
            {
                StudentId = studentId,
                StudentName = $"{student.FirstName} {student.LastName}",
                ExamId = examId,
                ExamName = exam.Name,
                TotalMarks = ranking.TotalMarks,
                SectionRank = ranking.SectionRank,
                ClassRank = ranking.ClassRank,
                TotalStudentsInSection = sectionStudents.Count(),
                TotalStudentsInClass = classStudents.Count(),
                UpdatedAt = ranking.UpdatedAt
            };

            return Ok(response);
        }
    }
}
