using Valora.Application.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Valora.Application.Common.Events;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Enums;
using Valora.Domain.Entities;
using System.Linq;

namespace Valora.Infrastructure.Services;

public class NotificationEventHandlers :
    IEventHandler<WorkspaceInviteAcceptedEvent>,

    IEventHandler<CommentAddedEvent>,
    IEventHandler<ReportSavedToWorkspaceEvent>,
    IEventHandler<BatchJobCompletedEvent>,
    IEventHandler<BatchJobFailedEvent>,
    IEventHandler<AiAnalysisCompletedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly ILogger<NotificationEventHandlers> _logger;

    public NotificationEventHandlers(
        INotificationService notificationService,
        IWorkspaceRepository workspaceRepository,
        ILogger<NotificationEventHandlers> logger)
    {
        _notificationService = notificationService;
        _workspaceRepository = workspaceRepository;
        _logger = logger;
    }

    private async Task CreateIdempotentNotificationAsync(string userId, string title, string body, NotificationType type, string? actionUrl)
    {
        var existingNotifications = await _notificationService.GetUserNotificationsAsync(userId, limit: 100);

        // Check if an identical notification already exists to ensure idempotency and retry-safe behavior
        var isDuplicate = existingNotifications.Any(n =>
            n.Title == title &&
            n.Body == body &&
            n.Type == type &&
            n.ActionUrl == actionUrl);

        if (!isDuplicate)
        {
            await _notificationService.CreateNotificationAsync(userId, title, body, type, actionUrl);
        }
        else
        {
            _logger.LogInformation("Skipped duplicate notification creation for user {UserId}: {Title}", userId, title);
        }
    }

    public async Task HandleAsync(WorkspaceInviteAcceptedEvent domainEvent, CancellationToken cancellationToken)
    {
        await CreateIdempotentNotificationAsync(
            domainEvent.InviterId,
            "Invite Accepted",
            $"{domainEvent.AcceptedByEmail} accepted your invite to workspace '{domainEvent.WorkspaceName}'.",
            NotificationType.Info,
            $"/workspaces/{domainEvent.WorkspaceId}");
    }



    public async Task HandleAsync(CommentAddedEvent domainEvent, CancellationToken cancellationToken)
    {
        // Notify all workspace members except the one who added the comment
        var members = await _workspaceRepository.GetMembersAsync(domainEvent.WorkspaceId, cancellationToken);
        var workspace = await _workspaceRepository.GetByIdAsync(domainEvent.WorkspaceId, cancellationToken);

        foreach (var member in members)
        {
            if (member.UserId != domainEvent.UserId && member.UserId != null)
            {
                await CreateIdempotentNotificationAsync(
                    member.UserId,
                    "New Comment",
                    $"A new comment was added to a listing in '{workspace?.Name ?? "your workspace"}'.",
                    NotificationType.Info,
                    $"/workspaces/{domainEvent.WorkspaceId}/listings/{domainEvent.SavedListingId}");
            }
        }
    }

    public async Task HandleAsync(ReportSavedToWorkspaceEvent domainEvent, CancellationToken cancellationToken)
    {
         var members = await _workspaceRepository.GetMembersAsync(domainEvent.WorkspaceId, cancellationToken);
         var workspace = await _workspaceRepository.GetByIdAsync(domainEvent.WorkspaceId, cancellationToken);

         foreach (var member in members)
         {
             if (member.UserId != domainEvent.UserId && member.UserId != null)
             {
                 await CreateIdempotentNotificationAsync(
                     member.UserId,
                     "Report Saved",
                     $"A new report was saved to workspace '{workspace?.Name ?? "your workspace"}'.",
                     NotificationType.Info,
                     $"/workspaces/{domainEvent.WorkspaceId}/listings");
             }
         }
    }

    public async Task HandleAsync(BatchJobCompletedEvent domainEvent, CancellationToken cancellationToken)
    {
        // System admin notification, hardcode admin id or broad notification logic
        // For simplicity, we could just log or notify a specific role if we had one
        _logger.LogInformation("Batch Job Completed: {JobId}", domainEvent.JobId);
    }

    public async Task HandleAsync(BatchJobFailedEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Batch Job Failed: {JobId}, {Error}", domainEvent.JobId, domainEvent.ErrorMessage);
    }

    public async Task HandleAsync(AiAnalysisCompletedEvent domainEvent, CancellationToken cancellationToken)
    {
        await CreateIdempotentNotificationAsync(
            domainEvent.UserId,
            "Analysis Ready",
            "Your AI neighborhood analysis is ready to view.",
            NotificationType.System,
            $"/reports?q={Uri.EscapeDataString(domainEvent.Query)}");
    }
}
