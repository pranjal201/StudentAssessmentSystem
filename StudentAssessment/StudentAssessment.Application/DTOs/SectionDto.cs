namespace StudentAssessment.Application.DTOs
{
    public class CreateSectionRequest
    {
        public Guid ClassId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateSectionRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class SectionResponse
    {
        public Guid Id { get; set; }
        public Guid ClassId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class SectionDetailResponse
    {
        public Guid Id { get; set; }
        public Guid ClassId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public int TeacherCount { get; set; }
    }
}
