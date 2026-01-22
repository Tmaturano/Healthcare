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

    [Fact]
    public async Task GetDailyAdherenceScoreAsync_ShouldReturnZero_WhenNoEventsExist()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var service = CreateService(db);
        var patientId = Guid.NewGuid();

        // Act
        var score = await service.GetDailyAdherenceScoreAsync(patientId);

        // Assert
        score.Should().Be(0);
    }

    [Fact]
    public async Task GetDailyAdherenceScoreAsync_ShouldReturnCorrectScore_WithPartialAdherence()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var service = CreateService(db);
        var patientId = Guid.NewGuid();

        // Add 2 events for today (50% adherence)
        db.UsageEvents.Add(new UsageEvent
        {
            ExternalEventId = "evt-1",
            Timestamp = DateTime.UtcNow,
            DeviceId = "dev",
            PatientId = patientId,
            EventType = "puff"
        });

        db.UsageEvents.Add(new UsageEvent
        {
            ExternalEventId = "evt-2",
            Timestamp = DateTime.UtcNow,
            DeviceId = "dev",
            PatientId = patientId,
            EventType = "puff"
        });

        await db.SaveChangesAsync();

        // Act
        var score = await service.GetDailyAdherenceScoreAsync(patientId);

        // Assert
        score.Should().Be(50);
    }

    [Fact]
    public async Task GetDailyAdherenceScoreAsync_ShouldReturnFullAdherence_WhenAllEventsExist()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var service = CreateService(db);
        var patientId = Guid.NewGuid();

        // Add 4 events for today (100% adherence)
        for (int i = 1; i <= 4; i++)
        {
            db.UsageEvents.Add(new UsageEvent
            {
                ExternalEventId = $"evt-{i}",
                Timestamp = DateTime.UtcNow,
                DeviceId = "dev",
                PatientId = patientId,
                EventType = "puff"
            });
        }

        await db.SaveChangesAsync();

        // Act
        var score = await service.GetDailyAdherenceScoreAsync(patientId);

        // Assert
        score.Should().Be(100);
    }

    [Fact]
    public async Task GetDailyAdherenceScoreAsync_ShouldOnlyCountTodaysEvents()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var service = CreateService(db);
        var patientId = Guid.NewGuid();

        // Add event from yesterday
        db.UsageEvents.Add(new UsageEvent
        {
            ExternalEventId = "evt-yesterday",
            Timestamp = DateTime.UtcNow.AddDays(-1),
            DeviceId = "dev",
            PatientId = patientId,
            EventType = "puff"
        });

        // Add 2 events for today
        db.UsageEvents.Add(new UsageEvent
        {
            ExternalEventId = "evt-1",
            Timestamp = DateTime.UtcNow,
            DeviceId = "dev",
            PatientId = patientId,
            EventType = "puff"
        });

        db.UsageEvents.Add(new UsageEvent
        {
            ExternalEventId = "evt-2",
            Timestamp = DateTime.UtcNow,
            DeviceId = "dev",
            PatientId = patientId,
            EventType = "puff"
        });

        await db.SaveChangesAsync();

        // Act
        var score = await service.GetDailyAdherenceScoreAsync(patientId);

        // Assert
        score.Should().Be(50); // Only 2 out of 4 required today
    }

    [Fact]
    public async Task GetDailyAdherenceScoreAsync_ShouldOnlyCountEventsForSpecificPatient()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var service = CreateService(db);
        var patientId = Guid.NewGuid();
        var otherPatientId = Guid.NewGuid();

        // Add event for other patient
        db.UsageEvents.Add(new UsageEvent
        {
            ExternalEventId = "evt-other",
            Timestamp = DateTime.UtcNow,
            DeviceId = "dev",
            PatientId = otherPatientId,
            EventType = "puff"
        });

        // Add 2 events for target patient
        db.UsageEvents.Add(new UsageEvent
        {
            ExternalEventId = "evt-1",
            Timestamp = DateTime.UtcNow,
            DeviceId = "dev",
            PatientId = patientId,
            EventType = "puff"
        });

        db.UsageEvents.Add(new UsageEvent
        {
            ExternalEventId = "evt-2",
            Timestamp = DateTime.UtcNow,
            DeviceId = "dev",
            PatientId = patientId,
            EventType = "puff"
        });

        await db.SaveChangesAsync();

        // Act
        var score = await service.GetDailyAdherenceScoreAsync(patientId);

        // Assert
        score.Should().Be(50); // Only 2 out of 4 required for this patient
    }
}
