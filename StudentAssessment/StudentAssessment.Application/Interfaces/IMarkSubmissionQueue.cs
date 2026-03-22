namespace StudentAssessment.Application.Interfaces;

public interface IMarkSubmissionQueue
{
    bool TryQueue(Guid jobId);
    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
}
