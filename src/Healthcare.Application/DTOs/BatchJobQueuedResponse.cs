namespace Healthcare.Application.DTOs;

public record BatchJobQueuedResponse(
    Guid JobId,
    string Message,
    int EventCount
);
