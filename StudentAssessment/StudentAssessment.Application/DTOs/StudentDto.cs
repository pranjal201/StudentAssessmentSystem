namespace StudentAssessment.Application.DTOs
{
    public class CreateStudentRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public Guid ClassId { get; set; }
        public Guid SectionId { get; set; }
        public Guid? UserId { get; set; }
    }

    public class UpdateStudentRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public Guid ClassId { get; set; }
        public Guid SectionId { get; set; }
        public Guid? UserId { get; set; }
    }

    public class StudentResponse
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public Guid SectionId { get; set; }
        public string SectionName { get; set; } = string.Empty;
        public Guid? UserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class StudentDetailResponse
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public Guid ClassId { get; set; }
        public Guid SectionId { get; set; }
        public Guid? UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public int MarksCount { get; set; }
    }
}
