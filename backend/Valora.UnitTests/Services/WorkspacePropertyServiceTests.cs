using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Extensions;
using Valora.Application.Services;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.UnitTests.Services;

public class WorkspacePropertyServiceTests
{
    private readonly Mock<IWorkspaceMemberRepository> _memberRepoMock;
    private readonly Mock<ISavedPropertyRepository> _savedPropertyRepoMock;
    private readonly Mock<IPropertyRepository> _propertyRepoMock;
    private readonly Mock<IActivityLogRepository> _activityLogRepoMock;
    private readonly Mock<IEventDispatcher> _eventMock;
    private readonly WorkspacePropertyService _service;

    public WorkspacePropertyServiceTests()
    {
        _memberRepoMock = new Mock<IWorkspaceMemberRepository>();
        _savedPropertyRepoMock = new Mock<ISavedPropertyRepository>();
        _propertyRepoMock = new Mock<IPropertyRepository>();
        _activityLogRepoMock = new Mock<IActivityLogRepository>();
        _eventMock = new Mock<IEventDispatcher>();
        _service = new WorkspacePropertyService(
            _memberRepoMock.Object,
            _savedPropertyRepoMock.Object,
            _propertyRepoMock.Object,
            _activityLogRepoMock.Object,
            _eventMock.Object);
    }

    [Fact]
    public async Task SavePropertyAsync_ShouldSucceed_WhenPropertyExists()
    {
        // Arrange
        var userId = "user1";
        var wsId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        
        _memberRepoMock.Setup(r => r.GetUserRoleAsync(wsId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkspaceRole.Owner);
        _savedPropertyRepoMock.Setup(r => r.GetSavedPropertyAsync(wsId, propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SavedProperty?)null);
        _propertyRepoMock.Setup(r => r.GetPropertyAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Property { Id = propertyId, Address = "Test St 1" });

        // Act
        var result = await _service.SavePropertyAsync(userId, wsId, propertyId, "notes");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(propertyId, result.PropertyId);
        _savedPropertyRepoMock.Verify(r => r.AddSavedPropertyAsync(It.IsAny<SavedProperty>(), It.IsAny<CancellationToken>()), Times.Once);
        _activityLogRepoMock.Verify(r => r.LogActivityAsync(It.Is<ActivityLog>(l => l.Type == ActivityLogType.PropertySaved && l.WorkspaceId == wsId && l.ActorId == userId), It.IsAny<CancellationToken>()), Times.Once);
        _savedPropertyRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventMock.Verify(e => e.DispatchAsync(It.IsAny<Valora.Application.Common.Events.ReportSavedToWorkspaceEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
