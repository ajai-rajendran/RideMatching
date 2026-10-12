using System.Threading.Channels;
using RideMatching.Application.Interfaces;

namespace RideMatching.Infrastructure.Services;

public class RideMatchingChannel : IRideMatchingChannel
{
    private readonly Channel<RideMatchingTask> _channel;

    public RideMatchingChannel()
    {
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };
        _channel = Channel.CreateBounded<RideMatchingTask>(options);
    }

    public async ValueTask<bool> EnqueueAsync(RideMatchingTask task, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(task, cancellationToken);
        return true;
    }

    public IAsyncEnumerable<RideMatchingTask> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
