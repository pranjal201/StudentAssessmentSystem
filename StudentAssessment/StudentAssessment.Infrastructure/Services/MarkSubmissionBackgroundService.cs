using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StudentAssessment.Application.DTOs;
using StudentAssessment.Application.Interfaces;
using StudentAssessment.Core.Entities;
using StudentAssessment.Core.Enums;
using StudentAssessment.Infrastructure.Database;

namespace StudentAssessment.Infrastructure.Services;

public class MarkSubmissionBackgroundService : BackgroundService
{
    private const int MaxRetryCount = 3;
    private const int PendingBatchSize = 100;

    private readonly IMarkSubmissionQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MarkSubmissionBackgroundService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public MarkSubmissionBackgroundService(
        IMarkSubmissionQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<MarkSubmissionBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EnqueuePendingJobsAsync(stoppingToken);

                using var pollCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                var dequeueTask = _queue.DequeueAsync(pollCts.Token).AsTask();
                var delayTask = Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                var completedTask = await Task.WhenAny(dequeueTask, delayTask);

                if (completedTask == dequeueTask)
                {
                    await ProcessJobAsync(await dequeueTask, stoppingToken);
                }
                else
                {
                    await pollCts.CancelAsync();
                    try
                    {
                        await dequeueTask;
                    }
                    catch (OperationCanceledException)
                    {
                        // No job became available during this poll interval.
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing mark submissions.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private async Task EnqueuePendingJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;

        var pendingJobs = await db.MarkSubmission
            .Where(j =>
                (j.Status == JobStatus.Pending || j.Status == JobStatus.Failed) &&
                (j.NextRetryAt == null || j.NextRetryAt <= now))
            .OrderBy(j => j.CreatedAt)
            .Take(PendingBatchSize)
            .Select(j => j.Id)
            .ToListAsync(cancellationToken);

        foreach (var jobId in pendingJobs)
        {
            if (!_queue.TryQueue(jobId))
            {
                break;
            }
        }
    }

    private async Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;

        var job = await db.MarkSubmission.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
        if (job == null || job.Status == JobStatus.Completed || job.Status == JobStatus.PermanentFailure)
        {
            return;
        }

        if (job.NextRetryAt.HasValue && job.NextRetryAt.Value > now)
        {
            return;
        }

        job.Status = JobStatus.Processing;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var request = JsonSerializer.Deserialize<CreateMarkRequest>(job.Payload, _jsonOptions)
                ?? throw new InvalidOperationException("Mark submission payload is invalid.");

            var examId = await ProcessMarkAsync(db, request, job.CorrelationId, cancellationToken);
            await RecalculateRankingsAsync(db, examId, cancellationToken);

            job.Status = JobStatus.Completed;
            job.ProcessedAt = DateTime.UtcNow;
            job.NextRetryAt = null;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            job.RetryCount += 1;
            job.ProcessedAt = null;

            if (job.RetryCount >= MaxRetryCount)
            {
                job.Status = JobStatus.PermanentFailure;
                job.NextRetryAt = null;
            }
            else
            {
                job.Status = JobStatus.Failed;
                job.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, job.RetryCount));
            }

            await db.SaveChangesAsync(cancellationToken);
            _logger.LogError(ex, "Failed to process mark submission job {JobId}", jobId);
        }
    }

    private static async Task<Guid> ProcessMarkAsync(
        ApplicationDbContext db,
        CreateMarkRequest request,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var student = await db.Student.FirstOrDefaultAsync(s => s.Id == request.StudentId, cancellationToken)
            ?? throw new InvalidOperationException("Student not found.");

        var subject = await db.Subject.FirstOrDefaultAsync(s => s.Code == request.SubjectCode, cancellationToken)
            ?? throw new InvalidOperationException("Subject not found.");

        var exam = await db.Exam.FirstOrDefaultAsync(e => e.Id == request.ExamId, cancellationToken)
            ?? throw new InvalidOperationException("Exam not found.");

        if (student.ClassId != exam.ClassId)
        {
            throw new InvalidOperationException("Exam does not belong to the student's class.");
        }

        var existingMark = await db.Mark.FirstOrDefaultAsync(m =>
            m.StudentId == request.StudentId &&
            m.SubjectCode == request.SubjectCode &&
            m.ExamId == request.ExamId, cancellationToken);

        if (existingMark != null)
        {
            existingMark.Score = request.Score;
            existingMark.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            db.Mark.Add(new Mark
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                SubjectCode = subject.Code,
                ExamId = exam.Id,
                Score = request.Score,
                IdempotencyKey = correlationId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return exam.Id;
    }

    private static async Task RecalculateRankingsAsync(
        ApplicationDbContext db,
        Guid examId,
        CancellationToken cancellationToken)
    {
        var exam = await db.Exam
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == examId, cancellationToken)
            ?? throw new InvalidOperationException("Exam not found for ranking generation.");

        var students = await db.Student
            .Where(s => s.ClassId == exam.ClassId)
            .Select(s => new StudentRankingSnapshot
            {
                StudentId = s.Id,
                SectionId = s.SectionId
            })
            .ToListAsync(cancellationToken);

        if (students.Count == 0)
        {
            return;
        }

        var subjectCodes = await db.Subject
            .AsNoTracking()
            .Select(s => s.Code)
            .ToListAsync(cancellationToken);

        if (subjectCodes.Count == 0)
        {
            return;
        }

        var marks = await db.Mark
            .AsNoTracking()
            .Where(m => m.ExamId == examId)
            .Select(m => new
            {
                m.StudentId,
                m.SubjectCode,
                m.Score
            })
            .ToListAsync(cancellationToken);

        var markLookup = marks.ToDictionary(
            m => (m.StudentId, m.SubjectCode),
            m => m.Score);

        foreach (var student in students)
        {
            decimal total = 0;
            foreach (var subjectCode in subjectCodes)
            {
                if (markLookup.TryGetValue((student.StudentId, subjectCode), out var score))
                {
                    total += score;
                }
            }

            student.TotalMarks = total;
        }

        ApplyCompetitionRanks(
            students.OrderByDescending(s => s.TotalMarks).ThenBy(s => s.StudentId).ToList(),
            (student, rank) => student.ClassRank = rank);

        foreach (var sectionGroup in students.GroupBy(s => s.SectionId))
        {
            ApplyCompetitionRanks(
                sectionGroup.OrderByDescending(s => s.TotalMarks).ThenBy(s => s.StudentId).ToList(),
                (student, rank) => student.SectionRank = rank);
        }

        var existingRankings = await db.Ranking
            .Where(r => r.ExamId == examId)
            .ToDictionaryAsync(r => r.StudentId, cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var student in students)
        {
            if (existingRankings.TryGetValue(student.StudentId, out var ranking))
            {
                ranking.TotalMarks = student.TotalMarks;
                ranking.ClassRank = student.ClassRank;
                ranking.SectionRank = student.SectionRank;
                ranking.UpdatedAt = now;
            }
            else
            {
                db.Ranking.Add(new Ranking
                {
                    StudentId = student.StudentId,
                    ExamId = examId,
                    TotalMarks = student.TotalMarks,
                    ClassRank = student.ClassRank,
                    SectionRank = student.SectionRank,
                    UpdatedAt = now
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyCompetitionRanks(
        List<StudentRankingSnapshot> orderedStudents,
        Action<StudentRankingSnapshot, int> assignRank)
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

    private sealed class StudentRankingSnapshot
    {
        public Guid StudentId { get; init; }
        public Guid SectionId { get; init; }
        public decimal TotalMarks { get; set; }
        public int SectionRank { get; set; }
        public int ClassRank { get; set; }
    }
}
