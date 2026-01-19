using AwesomeAssertions;
using Healthcare.API.Validators;
using Healthcare.Application.DTOs;

namespace Healthcare.Application.UnitTests.Validators;

public class BatchEventsRequestValidatorTests
{
    [Fact]
    public async Task BatchEventsRequest_WhenEventsAreEmpty_ShouldBeInvalid()
    {
        // Arrange
        var request = new BatchEventsRequest([]);
        var validator = new BatchEventsRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();        
    }

    [Fact]
    public async Task BatchEventsRequest_WhenExternalEventIdIsEmpty_ShouldBeInvalid()
    {
        // Arrange
        var request = new BatchEventsRequest(new List<UsageEventRequest>
        {
            new UsageEventRequest("", DateTime.UtcNow, "dev1", Guid.NewGuid(), "puff")
        });

        var validator = new BatchEventsRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
