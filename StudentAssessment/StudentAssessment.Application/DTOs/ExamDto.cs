namespace StudentAssessment.Application.DTOs;

public class CreateExamRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Guid ClassId { get; set; }
}

public class UpdateExamRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Guid ClassId { get; set; }
}

public class ExamResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Guid ClassId { get; set; }
}

public class ExamDetailResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Guid ClassId { get; set; }
    public int MarksCount { get; set; }
    public int RankingsCount { get; set; }
}
