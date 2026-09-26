using System.Threading.Channels;

namespace JobMatch.Api.Processing;

// in-memory queue of posting ids waiting to be processed
public class ProcessingQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public ValueTask EnqueueAsync(Guid postingId, CancellationToken ct = default)
    {
        return _channel.Writer.WriteAsync(postingId, ct);
    }

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken ct)
    {
        return _channel.Reader.ReadAllAsync(ct);
    }
}
