using Healthcare.Domain.Entities;

namespace Healthcare.Application.DTOs;

public record BatchJobStatusResponse(
    Guid JobId,
    BatchJobStatus Status,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    BatchEventsResponse? Result,
    string? ErrorMessage,
    int RetryCount
);
