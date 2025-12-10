namespace Healthcare.Domain.Entities;

public class UsageEvent
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public required string DeviceId { get; set; }
    public DateTime Timestamp { get; set; }
    public required string EventType { get; set; }

    // The batch may contain the same event multiple times
    // A unique identifier per event should be sent from the device to ensure idempotency
    public required string ExternalEventId { get; set; }
}
