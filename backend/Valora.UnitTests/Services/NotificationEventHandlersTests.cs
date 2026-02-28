using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Valora.Application.Common.Events;
using Valora.Application.Common.Interfaces;
using Valora.Application.Services;
using Valora.Domain.Entities;
using Valora.Domain.Enums;
using Valora.Infrastructure.Services;
using Xunit;

namespace Valora.UnitTests.Services;

public class NotificationEventHandlersTests
{
    private readonly Mock<INotificationService> _notificationServiceMock = new();
    private readonly Mock<IWorkspaceRepository> _workspaceRepositoryMock = new();
    private readonly Mock<ILogger<NotificationEventHandlers>> _loggerMock = new();
    private readonly NotificationEventHandlers _handlers;

    public NotificationEventHandlersTests()
    {
        _handlers = new NotificationEventHandlers(
            _notificationServiceMock.Object,
            _workspaceRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleWorkspaceInviteAcceptedEvent_ShouldCreateNotification_IfNotDuplicate()
    {
        var domainEvent = new WorkspaceInviteAcceptedEvent(Guid.NewGuid(), "My Workspace", "inviter1", "accepter1", "test@test.com");
        _notificationServiceMock.Setup(x => x.ExistsAsync("inviter1", It.IsAny<string>())).ReturnsAsync(false);

        await _handlers.HandleAsync(domainEvent, CancellationToken.None);

        _notificationServiceMock.Verify(x => x.CreateNotificationAsync(
            "inviter1",
            "Invite Accepted",
            It.Is<string>(s => s.Contains("test@test.com") && s.Contains("My Workspace")),
            NotificationType.Info,
            $"/workspaces/{domainEvent.WorkspaceId}",
            It.Is<string>(s => s.Contains("inviteAccepted:"))), Times.Once);
    }

    [Fact]
    public async Task HandleWorkspaceInviteAcceptedEvent_ShouldNotCreateNotification_IfDuplicate()
    {
        var domainEvent = new WorkspaceInviteAcceptedEvent(Guid.NewGuid(), "My Workspace", "inviter1", "accepter1", "test@test.com");
        _notificationServiceMock.Setup(x => x.ExistsAsync("inviter1", It.IsAny<string>())).ReturnsAsync(true);

        await _handlers.HandleAsync(domainEvent, CancellationToken.None);

        _notificationServiceMock.Verify(x => x.CreateNotificationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleCommentAddedEvent_ShouldNotifyOtherMembers()
    {
        var workspaceId = Guid.NewGuid();
        var listingId = Guid.NewGuid();
        var commenterId = "commenter";
        var otherMemberId = "other1";

        var domainEvent = new CommentAddedEvent(workspaceId, listingId, Guid.NewGuid(), commenterId, "Test", null);

        _workspaceRepositoryMock.Setup(x => x.GetMembersAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkspaceMember>
            {
                new() { UserId = commenterId },
                new() { UserId = otherMemberId }
            });

        _workspaceRepositoryMock.Setup(x => x.GetByIdAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Workspace { Id = workspaceId, Name = "Test WS", OwnerId = "owner" });

        _notificationServiceMock.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

        await _handlers.HandleAsync(domainEvent, CancellationToken.None);

        _notificationServiceMock.Verify(x => x.CreateNotificationAsync(
            otherMemberId,
            "New Comment",
            It.IsAny<string>(),
            NotificationType.Info,
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);

        // Commenter should not get notified
        _notificationServiceMock.Verify(x => x.CreateNotificationAsync(
            commenterId,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<NotificationType>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleReportSavedEvent_ShouldNotifyOtherMembers()
    {
        var workspaceId = Guid.NewGuid();
        var listingId = Guid.NewGuid();
        var saverId = "saver";
        var otherMemberId = "other1";

        var domainEvent = new ReportSavedToWorkspaceEvent(workspaceId, listingId, saverId);

        _workspaceRepositoryMock.Setup(x => x.GetMembersAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkspaceMember>
            {
                new() { UserId = saverId },
                new() { UserId = otherMemberId }
            });

        _workspaceRepositoryMock.Setup(x => x.GetByIdAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Workspace { Id = workspaceId, Name = "Test WS", OwnerId = "owner" });

        _notificationServiceMock.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

        await _handlers.HandleAsync(domainEvent, CancellationToken.None);

        _notificationServiceMock.Verify(x => x.CreateNotificationAsync(
            otherMemberId,
            "Report Saved",
            It.IsAny<string>(),
            NotificationType.Info,
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HandleBatchJobCompletedEvent_ShouldLogOnly()
    {
        var domainEvent = new BatchJobCompletedEvent(Guid.NewGuid(), BatchJobType.CityIngestion, "target");

        await _handlers.HandleAsync(domainEvent, CancellationToken.None);

        // Verify it didn't crash, logging is verified by lack of exceptions in basic test
        _notificationServiceMock.Verify(x => x.CreateNotificationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleBatchJobFailedEvent_ShouldLogOnly()
    {
        var domainEvent = new BatchJobFailedEvent(Guid.NewGuid(), BatchJobType.CityIngestion, "target", "error");

        await _handlers.HandleAsync(domainEvent, CancellationToken.None);

        _notificationServiceMock.Verify(x => x.CreateNotificationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAiAnalysisCompletedEvent_ShouldCreateNotification()
    {
        var userId = "user1";
        var domainEvent = new AiAnalysisCompletedEvent(userId);

        _notificationServiceMock.Setup(x => x.ExistsAsync(userId, It.IsAny<string>())).ReturnsAsync(false);

        await _handlers.HandleAsync(domainEvent, CancellationToken.None);

        _notificationServiceMock.Verify(x => x.CreateNotificationAsync(
            userId,
            "Analysis Ready",
            It.IsAny<string>(),
            NotificationType.System,
            "/reports",
            It.IsAny<string>()), Times.Once);
    }
}
