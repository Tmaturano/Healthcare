using AwesomeAssertions;
using Healthcare.API.Validators;
using Healthcare.Application.DTOs;
using Xunit;

namespace Healthcare.Application.UnitTests.Validators;

public class UsageEventRequestValidatorTests
{
    [Fact]
    public async Task UsageEventRequest_WhenAnyRequiredFieldIsEmpty_ShouldBeInvalid()
    {
        // Arrange
        var request = new UsageEventRequest("", DateTime.UtcNow, "", Guid.Empty, "");
        var validator = new UsageEventRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task UsageEventRequest_WhenAllFieldsAreProvided_ShouldBeValid()
    {
        // Arrange
        var request = new UsageEventRequest("ext1", DateTime.UtcNow, "dev1", Guid.NewGuid(), "start");
        var validator = new UsageEventRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
