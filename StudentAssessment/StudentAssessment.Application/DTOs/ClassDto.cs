namespace StudentAssessment.Application.DTOs
{
    public class CreateClassRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateClassRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class ClassResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ClassDetailResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int SectionCount { get; set; }
        public int StudentCount { get; set; }
    }
}
