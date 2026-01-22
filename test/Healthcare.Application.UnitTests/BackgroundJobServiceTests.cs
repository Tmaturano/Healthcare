using AwesomeAssertions;
using Healthcare.Application.DTOs;
using Healthcare.Application.Services;
using Healthcare.Domain.Entities;
using Healthcare.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Text.Json;

namespace Healthcare.Application.UnitTests;

public class BackgroundJobServiceTests
{
    private BackgroundJobService CreateService(HealthcareDbContext db)
    {
        var logger = Substitute.For<ILogger<BackgroundJobService>>();
        return new BackgroundJobService(db, logger);
    }

    [Fact]
    public async Task QueueBatchAsync_ShouldCreatePendingJob()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var service = CreateService(db);

        var patientId = Guid.NewGuid();
        var request = new BatchEventsRequest(
            [
                new UsageEventRequest("evt-1", DateTime.UtcNow, "dev1", patientId, "puff"),
                new UsageEventRequest("evt-2", DateTime.UtcNow, "dev1", patientId, "puff")
            ]
        );

        // Act
        var jobId = await service.QueueBatchAsync(request);

        // Assert
        jobId.Should().NotBe(Guid.Empty);

        var job = await db.BatchJobs.FindAsync(jobId);
        job.Should().NotBeNull();
        job!.Status.Should().Be(BatchJobStatus.Pending);
        job.Payload.Should().NotBeNullOrEmpty();
        job.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task QueueBatchAsync_ShouldSerializeRequestCorrectly()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var service = CreateService(db);

        var patientId = Guid.NewGuid();
        var request = new BatchEventsRequest(
            [
                new UsageEventRequest("evt-1", DateTime.UtcNow, "dev1", patientId, "puff")
            ]
        );

        // Act
        var jobId = await service.QueueBatchAsync(request);

        // Assert
        var job = await db.BatchJobs.FindAsync(jobId);
        job.Should().NotBeNull();
        job!.Payload.Should().Contain("evt-1");
        job.Payload.Should().Contain("dev1");
        job.Payload.Should().Contain(patientId.ToString());
    }

    [Fact]
    public async Task GetJobStatusAsync_ShouldReturnNull_WhenJobDoesNotExist()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var service = CreateService(db);
        var nonExistentJobId = Guid.NewGuid();

        // Act
        var result = await service.GetJobStatusAsync(nonExistentJobId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetJobStatusAsync_ShouldReturnPendingStatus_WhenJobIsQueued()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var service = CreateService(db);

        var patientId = Guid.NewGuid();
        var request = new BatchEventsRequest(
            [
                new UsageEventRequest("evt-1", DateTime.UtcNow, "dev1", patientId, "puff")
            ]
        );

        var jobId = await service.QueueBatchAsync(request);

        // Act
        var result = await service.GetJobStatusAsync(jobId);

        // Assert
        result.Should().NotBeNull();
        result!.JobId.Should().Be(jobId);
        result.Status.Should().Be(BatchJobStatus.Pending);
        result.Result.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task GetJobStatusAsync_ShouldReturnCompletedStatus_WithResults()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var service = CreateService(db);

        var patientId = Guid.NewGuid();
        var batchResponse = new BatchEventsResponse(
            TotalProcessed: 2,
            ProcessedEventIds: ["evt-1", "evt-2"],
            UpdatedAdherenceScore: 75.0
        );

        var job = new BatchJob
        {
            Payload = "{}",
            Status = BatchJobStatus.Completed,
            ProcessedAt = DateTime.UtcNow,
            Result = JsonSerializer.Serialize(batchResponse)
        };

        db.BatchJobs.Add(job);
        await db.SaveChangesAsync();

        // Act
        var result = await service.GetJobStatusAsync(job.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(BatchJobStatus.Completed);
        result.ProcessedAt.Should().NotBeNull();
        result.Result.Should().NotBeNull();
        result.Result!.TotalProcessed.Should().Be(2);
        result.Result.ProcessedEventIds.Should().Contain("evt-1");
        result.Result.ProcessedEventIds.Should().Contain("evt-2");
        result.Result.UpdatedAdherenceScore.Should().Be(75.0);
    }

    [Fact]
    public async Task GetJobStatusAsync_ShouldReturnFailedStatus_WithErrorMessage()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var service = CreateService(db);

        var job = new BatchJob
        {
            Payload = "{}",
            Status = BatchJobStatus.Failed,
            ErrorMessage = "Test error message",
            RetryCount = 3
        };

        db.BatchJobs.Add(job);
        await db.SaveChangesAsync();

        // Act
        var result = await service.GetJobStatusAsync(job.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(BatchJobStatus.Failed);
        result.ErrorMessage.Should().Be("Test error message");
        result.RetryCount.Should().Be(3);
        result.Result.Should().BeNull();
    }
}
