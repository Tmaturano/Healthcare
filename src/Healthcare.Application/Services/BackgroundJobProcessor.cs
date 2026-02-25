using Healthcare.Application.DTOs;
using Healthcare.Application.Interfaces;
using Healthcare.Domain.Entities;
using Healthcare.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Healthcare.Application.Services;

public class BackgroundJobProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundJobProcessor> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);

    public BackgroundJobProcessor(
        IServiceProvider serviceProvider,
        ILogger<BackgroundJobProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BackgroundJobProcessor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingJobs(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing background jobs");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("BackgroundJobProcessor stopped");
    }

    private async Task ProcessPendingJobs(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HealthcareDbContext>();
        var usageEventService = scope.ServiceProvider.GetRequiredService<IUsageEventService>();

        var pendingJobs = await db.BatchJobs
            .Where(j => j.Status == BatchJobStatus.Pending)
            .OrderBy(j => j.CreatedAt)
            .Take(10)//configurable
            .ToListAsync(cancellationToken);

        foreach (var job in pendingJobs)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await ProcessJob(job, db, usageEventService, cancellationToken);
        }
    }

    private async Task ProcessJob(
        BatchJob job,
        HealthcareDbContext db,
        IUsageEventService usageEventService,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing job {JobId}", job.Id);

            job.Status = BatchJobStatus.Processing;
            await db.SaveChangesAsync(cancellationToken);

            var request = JsonSerializer.Deserialize<BatchEventsRequest>(job.Payload);
            
            if (request == null)
            {
                throw new InvalidOperationException("Failed to deserialize batch request");
            }

            var result = await usageEventService.ProcessBatchAsync(request);

            job.Status = BatchJobStatus.Completed;
            job.ProcessedAt = DateTime.UtcNow;
            job.Result = JsonSerializer.Serialize(result);
            await db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully processed job {JobId}", job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process job {JobId}", job.Id);

            job.Status = BatchJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.RetryCount++;
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
