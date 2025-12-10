using FluentValidation;
using Healthcare.Application.DTOs;

namespace Healthcare.API.Validators;

public class BatchEventsRequestValidator : AbstractValidator<BatchEventsRequest>
{
    public BatchEventsRequestValidator()
    {
        RuleFor(x => x.Events)
            .NotEmpty().WithMessage("Events batch cannot be empty.");

        RuleForEach(x => x.Events)
            .SetValidator(new UsageEventRequestValidator());
    }
}

