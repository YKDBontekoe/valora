using System.ComponentModel.DataAnnotations;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.UnitTests.DTOs;

public class WorkspaceDtoTests
{
    [Fact]
    public void InviteMemberDto_ShouldFail_WhenEmailIsTooLong()
    {
        // Arrange
        var longEmail = new string('a', 246) + "@test.com"; // 255 chars

        var dto = new InviteMemberDto(longEmail, WorkspaceRole.Viewer);
        var validationContext = new ValidationContext(dto);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(InviteMemberDto.Email)));
    }

    [Fact]
    public void InviteMemberDto_ShouldPass_WhenEmailIsWithinLimit()
    {
        // Arrange
        var validEmail = new string('a', 244) + "@test.com"; // 253 chars

        var dto = new InviteMemberDto(validEmail, WorkspaceRole.Viewer);
        var validationContext = new ValidationContext(dto);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

        // Assert
        Assert.True(isValid);
    }
}
