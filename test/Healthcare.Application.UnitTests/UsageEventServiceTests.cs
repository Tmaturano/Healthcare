using AwesomeAssertions;
using Healthcare.Application.DTOs;
using Healthcare.Application.Services;
using Healthcare.Domain.Entities;
using Healthcare.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Healthcare.Application.UnitTests;

public class UsageEventServiceTests
{
    private UsageEventService CreateService(HealthcareDbContext db)
    {
        var logger = Substitute.For<ILogger<UsageEventService>>();
        return new UsageEventService(db, logger);
    }

    [Fact]
    public async Task ProcessBatch_ShouldIgnoreDuplicateEvents()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var service = CreateService(db);

        var patientId = Guid.NewGuid();

        var request = new BatchEventsRequest(
            [
                new UsageEventRequest("evt-1", DateTime.UtcNow, "dev1", patientId, "puff"),
                new UsageEventRequest("evt-1", DateTime.UtcNow, "dev1", patientId, "puff")
            ]
        );

        // Act
        var result = await service.ProcessBatchAsync(request);

        // Assert
        result.TotalProcessed.Should().Be(1);
        result.ProcessedEventIds.Should().HaveCount(1);

        var storedEvents = await db.UsageEvents.ToListAsync();
        storedEvents.Should().HaveCount(1);
    }
    
    [Fact]
    public async Task CalculateAdherence_ShouldReturnCorrectPercentage()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var service = CreateService(db);

        var patientId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Two existing events for today (out of required 4)
        db.UsageEvents.Add(new UsageEvent
        {
            ExternalEventId = "evt-1",
            Timestamp = now,
            DeviceId = "dev",
            PatientId = patientId,
            EventType = "puff"
        });

        db.UsageEvents.Add(new UsageEvent
        {
            ExternalEventId = "evt-2",
            Timestamp = now,
            DeviceId = "dev",
            PatientId = patientId,
            EventType = "puff"
        });

        await db.SaveChangesAsync();

        var expectedScore = 75;

        // Act
        var request = new BatchEventsRequest(
            [
                new UsageEventRequest("evt-10", DateTime.UtcNow, "dev", patientId, "puff")
            ]
        );

        var response = await service.ProcessBatchAsync(request);

        // Assert
        response.UpdatedAdherenceScore.Should().Be(expectedScore);
    }

    [Fact]
    public async Task ProcessBatch_ShouldReturnCorrectResponse()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var service = CreateService(db);

        var patientId = Guid.NewGuid();

        var request = new BatchEventsRequest(
            [
                new UsageEventRequest("evt-10", DateTime.UtcNow, "dev", patientId, "puff")
            ]
        );

        // Act
        var response = await service.ProcessBatchAsync(request);

        // Assert
        response.TotalProcessed.Should().Be(1);
        response.ProcessedEventIds.Should().Contain("evt-10");
        response.UpdatedAdherenceScore.Should().BeGreaterThan(0);
    }
}
