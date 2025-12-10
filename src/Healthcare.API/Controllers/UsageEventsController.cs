using FluentValidation;
using Healthcare.Application.DTOs;
using Healthcare.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Healthcare.API.Controllers;

[Route("api/events")]
[ApiController]
public class UsageEventsController : ControllerBase
{
    private readonly IUsageEventService _service;
    private readonly IValidator<BatchEventsRequest> _validator;

    public UsageEventsController(
        IUsageEventService service, 
        IValidator<BatchEventsRequest> validator)
    {
        _service = service;
        _validator = validator;
    }

    [HttpPost("batch")]
    public async Task<ActionResult<BatchEventsResponse>> PostBatch(
        [FromBody] BatchEventsRequest request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var result = await _service.ProcessBatchAsync(request);
        return Ok(result);
    }
}
