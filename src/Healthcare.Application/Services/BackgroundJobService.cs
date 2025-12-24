using Healthcare.Application.DTOs;
using Healthcare.Application.Interfaces;
using Healthcare.Domain.Entities;
using Healthcare.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Healthcare.Application.Services;

public class BackgroundJobService : IBackgroundJobService
{
    private readonly HealthcareDbContext _db;
    private readonly ILogger<BackgroundJobService> _logger;

    public BackgroundJobService(HealthcareDbContext db, ILogger<BackgroundJobService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Guid> QueueBatchAsync(BatchEventsRequest request)
    {
        var payload = JsonSerializer.Serialize(request);

        var job = new BatchJob
        {
            Payload = payload,
            Status = BatchJobStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.BatchJobs.Add(job);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Queued batch job {JobId} with {EventCount} events", 
            job.Id, request.Events.Count());

        return job.Id;
    }

    public async Task<BatchJobStatusResponse?> GetJobStatusAsync(Guid jobId)
    {
        var job = await _db.BatchJobs.FindAsync(jobId);

        if (job == null)
        {
            return null;
        }

        BatchEventsResponse? result = null;
        if (!string.IsNullOrEmpty(job.Result))
        {
            result = JsonSerializer.Deserialize<BatchEventsResponse>(job.Result);
        }

        return new BatchJobStatusResponse(
            JobId: job.Id,
            Status: job.Status,
            CreatedAt: job.CreatedAt,
            ProcessedAt: job.ProcessedAt,
            Result: result,
            ErrorMessage: job.ErrorMessage,
            RetryCount: job.RetryCount
        );
    }
}
