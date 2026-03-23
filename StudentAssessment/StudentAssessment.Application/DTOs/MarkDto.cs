namespace StudentAssessment.Application.DTOs
{
    public enum MarkSubmissionOperation
    {
        Upsert = 0,
        Delete = 1
    }

    public class CreateMarkRequest
    {
        public Guid StudentId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public Guid ExamId { get; set; }
        public decimal Score { get; set; }
        public string? RequestId { get; set; }
    }

    public class MarkSubmissionPayload
    {
        public Guid StudentId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public Guid ExamId { get; set; }
        public decimal? Score { get; set; }
        public MarkSubmissionOperation Operation { get; set; } = MarkSubmissionOperation.Upsert;
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

    public class MarkSubmissionResponse
    {
        public Guid JobId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public int RetryCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? NextRetryAt { get; set; }
        public bool QueuedInMemory { get; set; }
    }
}
