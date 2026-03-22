namespace StudentAssessment.Application.DTOs;

public class CreateSubjectRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class UpdateSubjectRequest
{
    public string Name { get; set; } = string.Empty;
}

public class SubjectResponse
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
