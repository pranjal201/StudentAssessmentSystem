using StudentAssessment.Infrastructure.Services;

namespace StudentAssessment.UnitTests;

public class RankingCalculatorTests
{
    [Fact]
    public void AssignRanks_UsesCompetitionRanking_ForClassRanks()
    {
        var sectionId = Guid.NewGuid();
        var students = new List<RankingSnapshot>
        {
            new() { StudentId = Guid.Parse("00000000-0000-0000-0000-000000000001"), SectionId = sectionId, TotalMarks = 100 },
            new() { StudentId = Guid.Parse("00000000-0000-0000-0000-000000000002"), SectionId = sectionId, TotalMarks = 95 },
            new() { StudentId = Guid.Parse("00000000-0000-0000-0000-000000000003"), SectionId = sectionId, TotalMarks = 95 },
            new() { StudentId = Guid.Parse("00000000-0000-0000-0000-000000000004"), SectionId = sectionId, TotalMarks = 90 }
        };

        RankingCalculator.AssignRanks(students);

        var ordered = students.OrderByDescending(s => s.TotalMarks).ThenBy(s => s.StudentId).ToList();

        Assert.Equal(1, ordered[0].ClassRank);
        Assert.Equal(2, ordered[1].ClassRank);
        Assert.Equal(2, ordered[2].ClassRank);
        Assert.Equal(4, ordered[3].ClassRank);
    }

    [Fact]
    public void AssignRanks_CalculatesSectionRanks_IndependentlyFromClassRanks()
    {
        var sectionA = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var sectionB = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var students = new List<RankingSnapshot>
        {
            new() { StudentId = Guid.Parse("00000000-0000-0000-0000-000000000001"), SectionId = sectionA, TotalMarks = 100 },
            new() { StudentId = Guid.Parse("00000000-0000-0000-0000-000000000002"), SectionId = sectionA, TotalMarks = 95 },
            new() { StudentId = Guid.Parse("00000000-0000-0000-0000-000000000003"), SectionId = sectionB, TotalMarks = 95 },
            new() { StudentId = Guid.Parse("00000000-0000-0000-0000-000000000004"), SectionId = sectionB, TotalMarks = 80 }
        };

        RankingCalculator.AssignRanks(students);

        var topSectionA = students.Single(s => s.StudentId == Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var secondSectionA = students.Single(s => s.StudentId == Guid.Parse("00000000-0000-0000-0000-000000000002"));
        var topSectionB = students.Single(s => s.StudentId == Guid.Parse("00000000-0000-0000-0000-000000000003"));
        var secondSectionB = students.Single(s => s.StudentId == Guid.Parse("00000000-0000-0000-0000-000000000004"));

        Assert.Equal(1, topSectionA.SectionRank);
        Assert.Equal(2, secondSectionA.SectionRank);
        Assert.Equal(1, topSectionB.SectionRank);
        Assert.Equal(2, secondSectionB.SectionRank);

        Assert.Equal(1, topSectionA.ClassRank);
        Assert.Equal(2, secondSectionA.ClassRank);
        Assert.Equal(2, topSectionB.ClassRank);
        Assert.Equal(4, secondSectionB.ClassRank);
    }

    [Fact]
    public void AssignRanks_TreatsAbsentStudentWithZeroMarks_AsLowestRank()
    {
        var sectionId = Guid.NewGuid();
        var presentTop = Guid.Parse("00000000-0000-0000-0000-000000000011");
        var presentSecond = Guid.Parse("00000000-0000-0000-0000-000000000012");
        var absentStudent = Guid.Parse("00000000-0000-0000-0000-000000000013");

        var students = new List<RankingSnapshot>
        {
            new() { StudentId = presentTop, SectionId = sectionId, TotalMarks = 88 },
            new() { StudentId = presentSecond, SectionId = sectionId, TotalMarks = 72 },
            new() { StudentId = absentStudent, SectionId = sectionId, TotalMarks = 0 }
        };

        RankingCalculator.AssignRanks(students);

        var absent = students.Single(s => s.StudentId == absentStudent);

        Assert.Equal(0, absent.TotalMarks);
        Assert.Equal(3, absent.ClassRank);
        Assert.Equal(3, absent.SectionRank);
    }
}
