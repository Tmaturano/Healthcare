using Healthcare.Application.DTOs;

namespace Healthcare.Application.Interfaces;

public interface IBackgroundJobService
{
    Task<Guid> QueueBatchAsync(BatchEventsRequest request);
    Task<BatchJobStatusResponse?> GetJobStatusAsync(Guid jobId);
}
