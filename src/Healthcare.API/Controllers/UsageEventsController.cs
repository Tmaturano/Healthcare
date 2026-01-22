using FluentValidation;
using Healthcare.Application.DTOs;
using Healthcare.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Healthcare.API.Controllers;

[Route("api/events")]
[ApiController]
public class UsageEventsController : ControllerBase
{
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly IUsageEventService _usageEventService;
    private readonly IValidator<BatchEventsRequest> _validator;

    public UsageEventsController(
        IBackgroundJobService backgroundJobService,
        IUsageEventService usageEventService,
        IValidator<BatchEventsRequest> validator)
    {
        _backgroundJobService = backgroundJobService;
        _usageEventService = usageEventService;
        _validator = validator;
    }

    [HttpPost("batch")]
    public async Task<ActionResult<BatchJobQueuedResponse>> PostBatch(
        [FromBody] BatchEventsRequest request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var jobId = await _backgroundJobService.QueueBatchAsync(request);
        
        return Accepted(new BatchJobQueuedResponse(
            JobId: jobId,
            Message: "Batch has been queued for processing",
            EventCount: request.Events.Count()
        ));
    }

    [HttpGet("batch/{jobId}")]
    public async Task<ActionResult<BatchJobStatusResponse>> GetBatchStatus(Guid jobId)
    {
        var status = await _backgroundJobService.GetJobStatusAsync(jobId);

        if (status == null)
        {
            return NotFound(new { Message = $"Job {jobId} not found" });
        }

        return Ok(status);
    }

    [HttpGet("adherence/{patientId}")]
    public async Task<ActionResult<AdherenceScoreResponse>> GetDailyAdherenceScore(Guid patientId)
    {
        var score = await _usageEventService.GetDailyAdherenceScoreAsync(patientId);

        var description = score switch
        {
            >= 100 => "Excellent adherence",
            >= 75 => "Good adherence",
            >= 50 => "Fair adherence",
            _ => "Poor adherence"
        };

        return Ok(new AdherenceScoreResponse(
            PatientId: patientId,
            Score: score,
            CalculatedAt: DateTime.UtcNow,
            Description: description
        ));
    }
}
