using System.Collections.Concurrent;
using System.Threading.Channels;
using StudentAssessment.Application.Interfaces;

namespace StudentAssessment.Infrastructure.Services;

public class MarkSubmissionQueue : IMarkSubmissionQueue
{
    private readonly Channel<Guid> _channel;
    private readonly ConcurrentDictionary<Guid, byte> _scheduledJobs = new();

    public MarkSubmissionQueue(int capacity)
    {
        _channel = Channel.CreateBounded<Guid>(new BoundedChannelOptions(capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public bool TryQueue(Guid jobId)
    {
        if (!_scheduledJobs.TryAdd(jobId, 0))
        {
            return true;
        }

        if (_channel.Writer.TryWrite(jobId))
        {
            return true;
        }

        _scheduledJobs.TryRemove(jobId, out _);
        return false;
    }

    public async ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
    {
        var jobId = await _channel.Reader.ReadAsync(cancellationToken);
        _scheduledJobs.TryRemove(jobId, out _);
        return jobId;
    }
}
