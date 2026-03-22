namespace StudentAssessment.Application.DTOs;

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // Admin, Teacher, Student
}

public class UserResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UserDetailResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<TeacherSectionAssignmentResponse> SectionAssignments { get; set; } = [];
}

public class TeacherSectionAssignmentResponse
{
    public Guid TeacherId { get; set; }
    public Guid SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
}

public class AssignTeacherRequest
{
    public Guid TeacherId { get; set; }
    public Guid SectionId { get; set; }
}
