using FluentValidation;
using Healthcare.Application.DTOs;

namespace Healthcare.API.Validators;

public class UsageEventRequestValidator : AbstractValidator<UsageEventRequest>
{
    public UsageEventRequestValidator()
    {
        RuleFor(e => e.ExternalEventId).NotEmpty();
        RuleFor(e => e.Timestamp).NotEmpty();
        RuleFor(e => e.DeviceId).NotEmpty();
        RuleFor(e => e.PatientId).NotEmpty();
        RuleFor(e => e.EventType).NotEmpty();
    }
}
