namespace Healthcare.Application.DTOs;

public record BatchEventsRequest(IEnumerable<UsageEventRequest> Events);
