namespace Healthcare.Application.DTOs;

public record UsageEventRequest(
    string ExternalEventId,
    DateTime Timestamp,
    string DeviceId,
    Guid PatientId,
    string EventType
);
