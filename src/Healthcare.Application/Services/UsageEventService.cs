using Healthcare.Application.DTOs;
using Healthcare.Application.Interfaces;
using Healthcare.Domain.Entities;
using Healthcare.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Healthcare.Application.Services;

public class UsageEventService : IUsageEventService
{
    private readonly HealthcareDbContext _db;
    private readonly ILogger<UsageEventService> _logger;

    public UsageEventService(HealthcareDbContext db, ILogger<UsageEventService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<BatchEventsResponse> ProcessBatchAsync(BatchEventsRequest request)
    {
        var processedIds = new List<string>();

        foreach (var e in request.Events)
        {
            var existsInLocal = _db.UsageEvents.Local
                .Any(existing => existing.ExternalEventId == e.ExternalEventId);

            var existsInDb = await _db.UsageEvents
                .AnyAsync(existing => existing.ExternalEventId == e.ExternalEventId);


            var exists = existsInLocal || existsInDb;

            if (exists)
                continue; // idempotent skip

            var entity = new UsageEvent
            {
                ExternalEventId = e.ExternalEventId,
                Timestamp = e.Timestamp,
                DeviceId = e.DeviceId,
                PatientId = e.PatientId,
                EventType = e.EventType,
            };

            _db.UsageEvents.Add(entity);
            processedIds.Add(e.ExternalEventId);
        }

        await _db.SaveChangesAsync();

        // Recalculate adherence
        var score = await GetDailyAdherenceScoreAsync(request.Events.First().PatientId);

        return new BatchEventsResponse(
            TotalProcessed: processedIds.Count,
            ProcessedEventIds: processedIds,
            UpdatedAdherenceScore: score
        );
    }

    /// <summary>
    /// Daily Adherence Score Calculation
    /// </summary>
    /// <param name="patientId"></param>
    /// <returns>How well did the patient adhere Today to the expected usage schedule</returns>
    public async Task<double> GetDailyAdherenceScoreAsync(Guid patientId)
    {
        var today = DateTime.UtcNow.Date;

        var eventsToday = await _db.UsageEvents
            .Where(e => e.PatientId == patientId && e.Timestamp.Date == today)
            .CountAsync();

        const int requiredDailyPuffs = 4; // 2 puffs × 2 times per day

        return (double)eventsToday / requiredDailyPuffs * 100;
    }
}
