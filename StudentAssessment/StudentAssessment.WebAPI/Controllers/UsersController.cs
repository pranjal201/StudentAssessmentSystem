using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentAssessment.Application.DTOs;
using StudentAssessment.Application.Interfaces;
using StudentAssessment.Core.Entities;
using StudentAssessment.Core.Enums;

namespace StudentAssessment.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;

        public UsersController(IUnitOfWork unitOfWork, IAuthService authService)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
        }

        /// <summary>
        /// Get all users
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<UserResponse>>> GetAll()
        {
            var users = await _unitOfWork.Repository<User>().GetAllAsync();
            var response = users.Select(u => new UserResponse
            {
                Id = u.Id,
                Username = u.Username,
                Role = u.Role.ToString(),
                CreatedAt = u.CreatedAt
            });
            return Ok(response);
        }

        /// <summary>
        /// Get a user by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDetailResponse>> GetById(Guid id)
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            // Get teacher section assignments if user is a teacher
            var assignments = new List<TeacherSectionAssignmentResponse>();
            if (user.Role == UserRole.Teacher && user.TeacherSections != null)
            {
                foreach (var assignment in user.TeacherSections)
                {
                    var section = await _unitOfWork.Repository<Section>().GetByIdAsync(assignment.SectionId);
                    if (section != null)
                    {
                        var @class = await _unitOfWork.Repository<Class>().GetByIdAsync(section.ClassId);
                        assignments.Add(new TeacherSectionAssignmentResponse
                        {
                            TeacherId = user.Id,
                            SectionId = section.Id,
                            SectionName = section.Name,
                            ClassName = @class?.Name ?? string.Empty
                        });
                    }
                }
            }

            var response = new UserDetailResponse
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt,
                SectionAssignments = assignments
            };

            return Ok(response);
        }

        /// <summary>
        /// Create a new user
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(request.Username))
                    return BadRequest(new { message = "Username is required" });

                if (string.IsNullOrWhiteSpace(request.Password))
                    return BadRequest(new { message = "Password is required" });

                if (request.Password.Length < 6)
                    return BadRequest(new { message = "Password must be at least 6 characters" });

                if (string.IsNullOrWhiteSpace(request.Role))
                    return BadRequest(new { message = "Role is required" });

                // Validate role
                if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var roleEnum))
                    return BadRequest(new { message = "Invalid role. Must be: Admin, Teacher, or Student" });

                await _unitOfWork.BeginTransactionAsync();

                // Check if username already exists
                var existingUser = (await _unitOfWork.Repository<User>().FindAsync(u => u.Username == request.Username)).FirstOrDefault();
                if (existingUser != null)
                {
                    await _unitOfWork.RollbackAsync();
                    return BadRequest(new { message = "Username already exists" });
                }

                // Create user with hashed password
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = request.Username,
                    PasswordHash = _authService.HashPassword(request.Password),
                    Role = roleEnum,
                    CreatedAt = DateTime.UtcNow
                };

                var createdUser = await _unitOfWork.Repository<User>().AddAsync(user);
                await _unitOfWork.CommitAsync();

                var response = new UserResponse
                {
                    Id = createdUser.Id,
                    Username = createdUser.Username,
                    Role = createdUser.Role.ToString(),
                    CreatedAt = createdUser.CreatedAt
                };

                return CreatedAtAction(nameof(GetById), new { id = createdUser.Id }, response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred", error = ex.Message });
            }
        }

        /// <summary>
        /// Assign a teacher to a section
        /// </summary>
        [HttpPost("{teacherId}/sections/{sectionId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> AssignTeacherToSection(Guid teacherId, Guid sectionId)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Verify teacher exists and is a teacher
                var teacher = await _unitOfWork.Repository<User>().GetByIdAsync(teacherId);
                if (teacher == null)
                    return NotFound(new { message = "Teacher not found" });

                if (teacher.Role != UserRole.Teacher)
                {
                    await _unitOfWork.RollbackAsync();
                    return BadRequest(new { message = "User is not a teacher" });
                }

                // Verify section exists
                var section = await _unitOfWork.Repository<Section>().GetByIdAsync(sectionId);
                if (section == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return NotFound(new { message = "Section not found" });
                }

                // Check if assignment already exists
                var existingAssignment = (await _unitOfWork.Repository<TeacherSection>().FindAsync(ts =>
                    ts.TeacherId == teacherId && ts.SectionId == sectionId)).FirstOrDefault();

                if (existingAssignment != null)
                {
                    await _unitOfWork.RollbackAsync();
                    return BadRequest(new { message = "Teacher is already assigned to this section" });
                }

                // Create assignment
                var assignment = new TeacherSection
                {
                    TeacherId = teacherId,
                    SectionId = sectionId
                };

                await _unitOfWork.Repository<TeacherSection>().AddAsync(assignment);
                await _unitOfWork.CommitAsync();

                return Ok(new
                {
                    message = "Teacher assigned to section successfully",
                    teacherId = teacherId,
                    sectionId = sectionId
                });
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred", error = ex.Message });
            }
        }

        /// <summary>
        /// Remove a teacher from a section
        /// </summary>
        [HttpDelete("{teacherId}/sections/{sectionId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RemoveTeacherFromSection(Guid teacherId, Guid sectionId)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Find and delete assignment
                var assignment = (await _unitOfWork.Repository<TeacherSection>().FindAsync(ts =>
                    ts.TeacherId == teacherId && ts.SectionId == sectionId)).FirstOrDefault();

                if (assignment == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return NotFound(new { message = "Teacher assignment not found" });
                }

                await _unitOfWork.Repository<TeacherSection>().DeleteAsync(assignment);
                await _unitOfWork.CommitAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all teachers assigned to a section
        /// </summary>
        [HttpGet("section/{sectionId}/teachers")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<UserResponse>>> GetTeachersBySection(Guid sectionId)
        {
            // Get all teacher assignments for this section
            var assignments = await _unitOfWork.Repository<TeacherSection>().FindAsync(ts => ts.SectionId == sectionId);
            
            var response = new List<UserResponse>();
            foreach (var assignment in assignments)
            {
                var teacher = await _unitOfWork.Repository<User>().GetByIdAsync(assignment.TeacherId);
                if (teacher != null)
                {
                    response.Add(new UserResponse
                    {
                        Id = teacher.Id,
                        Username = teacher.Username,
                        Role = teacher.Role.ToString(),
                        CreatedAt = teacher.CreatedAt
                    });
                }
            }

            return Ok(response);
        }

        /// <summary>
        /// Get all sections assigned to a teacher
        /// </summary>
        [HttpGet("{teacherId}/sections")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<TeacherSectionAssignmentResponse>>> GetTeacherSections(Guid teacherId)
        {
            // Verify teacher exists
            var teacher = await _unitOfWork.Repository<User>().GetByIdAsync(teacherId);
            if (teacher == null)
                return NotFound(new { message = "Teacher not found" });

            // Get all section assignments for this teacher
            var assignments = await _unitOfWork.Repository<TeacherSection>().FindAsync(ts => ts.TeacherId == teacherId);
            
            var response = new List<TeacherSectionAssignmentResponse>();
            foreach (var assignment in assignments)
            {
                var section = await _unitOfWork.Repository<Section>().GetByIdAsync(assignment.SectionId);
                if (section != null)
                {
                    var @class = await _unitOfWork.Repository<Class>().GetByIdAsync(section.ClassId);
                    response.Add(new TeacherSectionAssignmentResponse
                    {
                        TeacherId = teacherId,
                        SectionId = section.Id,
                        SectionName = section.Name,
                        ClassName = @class?.Name ?? string.Empty
                    });
                }
            }

            return Ok(response);
        }
    }
}
