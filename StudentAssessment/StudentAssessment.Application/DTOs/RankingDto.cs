namespace StudentAssessment.Application.DTOs;

public class RankingResponse
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public Guid ExamId { get; set; }
    public decimal TotalMarks { get; set; }
    public int? SectionRank { get; set; }
    public int? ClassRank { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class StudentRankingResponse
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public Guid ExamId { get; set; }
    public string ExamName { get; set; } = string.Empty;
    public decimal TotalMarks { get; set; }
    public int? SectionRank { get; set; }
    public int? ClassRank { get; set; }
    public int TotalStudentsInSection { get; set; }
    public int TotalStudentsInClass { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ClassRankingResponse
{
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public Guid ExamId { get; set; }
    public string ExamName { get; set; } = string.Empty;
    public List<RankingResponse> Rankings { get; set; } = [];
}

public class SectionRankingResponse
{
    public Guid SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public Guid ExamId { get; set; }
    public string ExamName { get; set; } = string.Empty;
    public List<RankingResponse> Rankings { get; set; } = [];
}
