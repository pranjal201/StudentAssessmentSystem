namespace StudentAssessment.Application.DTOs
{
    public class CreateMarkRequest
    {
        public Guid StudentId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public Guid ExamId { get; set; }
        public decimal Score { get; set; }
    }

    public class MarkResponse
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public Guid ExamId { get; set; }
        public decimal Score { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class MarkDetailResponse
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public Guid ExamId { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class QueryMarksRequest
    {
        public Guid? StudentId { get; set; }
        public Guid? ExamId { get; set; }
        public string? SubjectCode { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
