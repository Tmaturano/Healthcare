namespace Healthcare.Application.DTOs;

public record BatchEventsResponse(
    int TotalProcessed,
    IEnumerable<string> ProcessedEventIds,
    double UpdatedAdherenceScore
);
