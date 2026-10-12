using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RideMatching.Application.Interfaces;

namespace RideMatching.Infrastructure.BackgroundServices;

public class RideMatchingBackgroundWorker : BackgroundService
{
    private readonly IRideMatchingChannel _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RideMatchingBackgroundWorker> _logger;
    private const int MaxAttempts = 3;

    public RideMatchingBackgroundWorker(
        IRideMatchingChannel channel,
        IServiceProvider serviceProvider,
        ILogger<RideMatchingBackgroundWorker> logger)
    {
        _channel = channel;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Ride Matching Background Worker started.");

        try
        {
            await foreach (var task in _channel.ReadAllAsync(stoppingToken))
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                _ = ProcessTaskAsync(task, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Ride Matching Background Worker is stopping gracefully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Ride Matching Background Worker.");
        }
    }

    private async Task ProcessTaskAsync(RideMatchingTask task, CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Processing matching task for Ride: {RideId}, Attempt: {Attempt}", task.RideId, task.AttemptCount);

            using var scope = _serviceProvider.CreateScope();
            var rideService = scope.ServiceProvider.GetRequiredService<IRideService>();

            var matched = await rideService.ProcessMatchingAttemptAsync(task.RideId, task.AttemptCount, stoppingToken);

            if (!matched && task.AttemptCount < MaxAttempts)
            {
                // Wait briefly before retrying with expanded search radius
                var delayMs = task.AttemptCount switch
                {
                    1 => 3000,
                    2 => 5000,
                    _ => 8000
                };

                _logger.LogInformation("No driver matched for Ride {RideId}. Scheduling Attempt {NextAttempt} in {DelayMs}ms",
                    task.RideId, task.AttemptCount + 1, delayMs);

                await Task.Delay(delayMs, stoppingToken);

                await _channel.EnqueueAsync(new RideMatchingTask(task.RideId, task.AttemptCount + 1), stoppingToken);
            }
            else if (!matched && task.AttemptCount >= MaxAttempts)
            {
                _logger.LogWarning("Ride {RideId} reached max matching attempts ({MaxAttempts}) with no driver found.",
                    task.RideId, MaxAttempts);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Ignore on shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing matching task for Ride {RideId}", task.RideId);
        }
    }
}
