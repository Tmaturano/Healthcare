using Healthcare.Application.DTOs;

namespace Healthcare.Application.Interfaces;

public interface IUsageEventService
{
    Task<BatchEventsResponse> ProcessBatchAsync(BatchEventsRequest request);
}

