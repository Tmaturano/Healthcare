namespace Healthcare.Domain.Entities;

public class BatchJob
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public BatchJobStatus Status { get; set; } = BatchJobStatus.Pending;
    public required string Payload { get; set; }
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
}

public enum BatchJobStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
