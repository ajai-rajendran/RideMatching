namespace RideMatching.Application.Interfaces;

public record RideMatchingTask(
    Guid RideId,
    int AttemptCount = 1,
    DateTime CreatedAt = default)
{
    public DateTime CreatedAt { get; init; } = CreatedAt == default ? DateTime.UtcNow : CreatedAt;
}

public interface IRideMatchingChannel
{
    ValueTask<bool> EnqueueAsync(RideMatchingTask task, CancellationToken cancellationToken = default);
    IAsyncEnumerable<RideMatchingTask> ReadAllAsync(CancellationToken cancellationToken = default);
}
