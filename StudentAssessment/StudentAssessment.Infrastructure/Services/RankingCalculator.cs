namespace StudentAssessment.Infrastructure.Services;

internal sealed class RankingSnapshot
{
    public Guid StudentId { get; init; }
    public Guid SectionId { get; init; }
    public decimal TotalMarks { get; set; }
    public int SectionRank { get; set; }
    public int ClassRank { get; set; }
}

internal static class RankingCalculator
{
    public static void AssignRanks(List<RankingSnapshot> students)
    {
        ApplyCompetitionRanks(
            students.OrderByDescending(s => s.TotalMarks).ThenBy(s => s.StudentId).ToList(),
            (student, rank) => student.ClassRank = rank);

        foreach (var sectionGroup in students.GroupBy(s => s.SectionId))
        {
            ApplyCompetitionRanks(
                sectionGroup.OrderByDescending(s => s.TotalMarks).ThenBy(s => s.StudentId).ToList(),
                (student, rank) => student.SectionRank = rank);
        }
    }

    private static void ApplyCompetitionRanks(
        List<RankingSnapshot> orderedStudents,
        Action<RankingSnapshot, int> assignRank)
    {
        decimal? previousTotal = null;
        var currentRank = 0;

        for (var i = 0; i < orderedStudents.Count; i++)
        {
            var student = orderedStudents[i];
            if (previousTotal == null || student.TotalMarks != previousTotal.Value)
            {
                currentRank = i + 1;
                previousTotal = student.TotalMarks;
            }

            assignRank(student, currentRank);
        }
    }
}
