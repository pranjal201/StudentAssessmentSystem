using Bogus;
using StudentAssessment.Application.Interfaces;
using StudentAssessment.Core.Entities;
using StudentAssessment.Core.Enums;
using StudentAssessment.Infrastructure.Database;

namespace StudentAssessment.Infrastructure.Data;

public static class DevelopmentDataSeeder
{
    private static readonly string[] ClassNames = ["9th", "10th"];
    private static readonly string[] SectionNames = ["A", "B"];
    private static readonly ExamType[] ExamTypes = [ExamType.Quarterly, ExamType.HalfYearly, ExamType.Final];
    private static readonly (string Code, string Name)[] Subjects =
    [
        ("ENG", "English"),
        ("MATH", "Mathematics"),
        ("HINDI", "Hindi"),
        ("SCI", "Science"),
        ("SOC", "Social")
    ];

    public static async Task SeedIfMissingAsync(ApplicationDbContext db, IAuthService authService)
    {
        if (!NeedsSeedData(db))
        {
            return;
        }

        Randomizer.Seed = new Random(20260322);

        await SeedAdminAsync(db, authService);
        await SeedSubjectsAsync(db);
        var classes = await SeedClassesAsync(db);
        var sections = await SeedSectionsAsync(db, classes);
        var teachers = await SeedTeachersAsync(db, authService);
        await SeedTeacherAssignmentsAsync(db, teachers, sections);
        await SeedStudentsAsync(db, authService, sections);
        var exams = await SeedExamsAsync(db, classes);
        await SeedMarksAsync(db, exams);
    }

    private static bool NeedsSeedData(ApplicationDbContext db)
    {
        var classCount = db.Class.Count(c => ClassNames.Contains(c.Name));
        var sectionCount = db.Section.Count(s => SectionNames.Contains(s.Name));
        var subjectCount = db.Subject.Count(s => Subjects.Select(x => x.Code).Contains(s.Code));
        var teacherCount = db.User.Count(u => u.Role == UserRole.Teacher && u.Username.StartsWith("teacher"));
        var studentCount = db.Student.Count();
        var teacherSectionCount = db.TeacherSection.Count();
        var examCount = db.Exam.Count();
        var markCount = db.Mark.Count();

        return classCount < 2
            || sectionCount < 4
            || subjectCount < 5
            || teacherCount < 5
            || studentCount < 40
            || teacherSectionCount < 20
            || examCount < 6
            || markCount < 600;
    }

    private static async Task SeedAdminAsync(ApplicationDbContext db, IAuthService authService)
    {
        if (db.User.Any(u => u.Username == "admin"))
        {
            return;
        }

        db.User.Add(new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            PasswordHash = authService.HashPassword("admin123"),
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedSubjectsAsync(ApplicationDbContext db)
    {
        foreach (var (code, name) in Subjects)
        {
            if (db.Subject.Any(s => s.Code == code))
            {
                continue;
            }

            db.Subject.Add(new Subject
            {
                Code = code,
                Name = name
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task<Dictionary<string, Class>> SeedClassesAsync(ApplicationDbContext db)
    {
        var classes = new Dictionary<string, Class>();

        foreach (var className in ClassNames)
        {
            var existingClass = db.Class.FirstOrDefault(c => c.Name == className);
            if (existingClass == null)
            {
                existingClass = new Class
                {
                    Id = Guid.NewGuid(),
                    Name = className
                };
                db.Class.Add(existingClass);
            }

            classes[className] = existingClass;
        }

        await db.SaveChangesAsync();
        return classes;
    }

    private static async Task<Dictionary<string, Section>> SeedSectionsAsync(ApplicationDbContext db, Dictionary<string, Class> classes)
    {
        var sections = new Dictionary<string, Section>();

        foreach (var @class in classes.Values)
        {
            foreach (var sectionName in SectionNames)
            {
                var key = $"{@class.Name}-{sectionName}";
                var existingSection = db.Section.FirstOrDefault(s => s.ClassId == @class.Id && s.Name == sectionName);
                if (existingSection == null)
                {
                    existingSection = new Section
                    {
                        Id = Guid.NewGuid(),
                        ClassId = @class.Id,
                        Name = sectionName
                    };
                    db.Section.Add(existingSection);
                }

                sections[key] = existingSection;
            }
        }

        await db.SaveChangesAsync();
        return sections;
    }

    private static async Task<List<User>> SeedTeachersAsync(ApplicationDbContext db, IAuthService authService)
    {
        var teachers = new List<User>();

        for (var i = 1; i <= 5; i++)
        {
            var username = $"teacher{i}";
            var existingTeacher = db.User.FirstOrDefault(u => u.Username == username);
            if (existingTeacher == null)
            {
                existingTeacher = new User
                {
                    Id = Guid.NewGuid(),
                    Username = username,
                    PasswordHash = authService.HashPassword($"teacher{i}123"),
                    Role = UserRole.Teacher,
                    CreatedAt = DateTime.UtcNow
                };

                db.User.Add(existingTeacher);
            }

            teachers.Add(existingTeacher);
        }

        await db.SaveChangesAsync();
        return teachers;
    }

    private static async Task SeedTeacherAssignmentsAsync(ApplicationDbContext db, List<User> teachers, Dictionary<string, Section> sections)
    {
        foreach (var teacher in teachers)
        {
            foreach (var section in sections.Values)
            {
                var exists = db.TeacherSection.Any(ts => ts.TeacherId == teacher.Id && ts.SectionId == section.Id);
                if (exists)
                {
                    continue;
                }

                db.TeacherSection.Add(new TeacherSection
                {
                    TeacherId = teacher.Id,
                    SectionId = section.Id
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedStudentsAsync(ApplicationDbContext db, IAuthService authService, Dictionary<string, Section> sections)
    {
        var studentFaker = new Faker();

        foreach (var entry in sections)
        {
            var section = entry.Value;
            var className = entry.Key.Split('-')[0];
            var sectionName = entry.Key.Split('-')[1];

            for (var i = 1; i <= 10; i++)
            {
                var username = $"student_{className}_{sectionName}_{i:00}".ToLowerInvariant();
                var existingUser = db.User.FirstOrDefault(u => u.Username == username);
                if (existingUser == null)
                {
                    existingUser = new User
                    {
                        Id = Guid.NewGuid(),
                        Username = username,
                        PasswordHash = authService.HashPassword($"{username}@123"),
                        Role = UserRole.Student,
                        CreatedAt = DateTime.UtcNow
                    };
                    db.User.Add(existingUser);
                    await db.SaveChangesAsync();
                }

                var existingStudent = db.Student.FirstOrDefault(s => s.UserId == existingUser.Id);
                if (existingStudent != null)
                {
                    continue;
                }

                db.Student.Add(new Student
                {
                    Id = Guid.NewGuid(),
                    FirstName = studentFaker.Name.FirstName(),
                    LastName = studentFaker.Name.LastName(),
                    ClassId = section.ClassId,
                    SectionId = section.Id,
                    UserId = existingUser.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task<Dictionary<string, Exam>> SeedExamsAsync(ApplicationDbContext db, Dictionary<string, Class> classes)
    {
        var exams = new Dictionary<string, Exam>();

        foreach (var @class in classes.Values)
        {
            foreach (var examType in ExamTypes)
            {
                var examName = $"{@class.Name} {examType}";
                var key = $"{@class.Id}:{examType}";
                var existingExam = db.Exam.FirstOrDefault(e => e.ClassId == @class.Id && e.Type == examType);

                if (existingExam == null)
                {
                    existingExam = new Exam
                    {
                        Id = Guid.NewGuid(),
                        ClassId = @class.Id,
                        Name = examName,
                        Type = examType
                    };
                    db.Exam.Add(existingExam);
                }

                exams[key] = existingExam;
            }
        }

        await db.SaveChangesAsync();
        return exams;
    }

    private static async Task SeedMarksAsync(ApplicationDbContext db, Dictionary<string, Exam> exams)
    {
        var marksFaker = new Faker();
        var students = db.Student.ToList();
        var subjects = Subjects.Select(s => s.Code).ToList();

        foreach (var student in students)
        {
            var studentExams = exams.Values.Where(e => e.ClassId == student.ClassId).ToList();

            foreach (var exam in studentExams)
            {
                foreach (var subjectCode in subjects)
                {
                    var existingMark = db.Mark.FirstOrDefault(m =>
                        m.StudentId == student.Id &&
                        m.ExamId == exam.Id &&
                        m.SubjectCode == subjectCode);

                    if (existingMark != null)
                    {
                        continue;
                    }

                    db.Mark.Add(new Mark
                    {
                        Id = Guid.NewGuid(),
                        StudentId = student.Id,
                        SubjectCode = subjectCode,
                        ExamId = exam.Id,
                        Score = marksFaker.Random.Decimal(45, 98),
                        IdempotencyKey = Guid.NewGuid().ToString(),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await db.SaveChangesAsync();
    }
}
