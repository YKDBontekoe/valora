using Microsoft.EntityFrameworkCore;
using Valora.Infrastructure.Persistence;

namespace Valora.Infrastructure.Utilities;

public static class UserCleanupUtility
{
    public static async Task CleanupUserDataAsync(ValoraDbContext context, string userId)
    {
        if (context.Database.IsRelational())
        {
            await context.Workspaces
                .Where(w => w.OwnerId == userId)
                .ExecuteDeleteAsync();

            await context.WorkspaceMembers
                .Where(wm => wm.UserId == userId)
                .ExecuteDeleteAsync();

            await context.SavedProperties
                .Where(sl => sl.AddedByUserId == userId)
                .ExecuteDeleteAsync();

            var userCommentIds = context.PropertyComments
                .Where(c => c.UserId == userId)
                .Select(c => c.Id);

            await context.PropertyComments
                .Where(c => c.ParentCommentId != null && userCommentIds.Contains(c.ParentCommentId.Value))
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.ParentCommentId, (Guid?)null));

            await context.PropertyComments
                .Where(c => c.UserId == userId)
                .ExecuteDeleteAsync();

            await context.ActivityLogs
                .Where(l => l.ActorId == userId)
                .ExecuteDeleteAsync();

            await context.UserAiProfiles
                .Where(p => p.UserId == userId)
                .ExecuteDeleteAsync();

            await context.RefreshTokens
                .Where(rt => rt.UserId == userId)
                .ExecuteDeleteAsync();

            await context.Notifications
                .Where(n => n.UserId == userId)
                .ExecuteDeleteAsync();
        }
        else
        {
            var ownedWorkspaces = context.Workspaces.Where(w => w.OwnerId == userId);
            context.Workspaces.RemoveRange(ownedWorkspaces);

            var memberships = context.WorkspaceMembers.Where(wm => wm.UserId == userId);
            context.WorkspaceMembers.RemoveRange(memberships);

            var savedProperties = context.SavedProperties.Where(sl => sl.AddedByUserId == userId);
            context.SavedProperties.RemoveRange(savedProperties);

            var userCommentIds = context.PropertyComments
                .Where(c => c.UserId == userId)
                .Select(c => c.Id)
                .ToList();

            var childComments = context.PropertyComments
                .Where(c => c.ParentCommentId != null && userCommentIds.Contains(c.ParentCommentId.Value))
                .ToList();

            foreach (var child in childComments)
            {
                child.ParentCommentId = null;
            }

            var comments = context.PropertyComments.Where(c => c.UserId == userId);
            context.PropertyComments.RemoveRange(comments);

            var logs = context.ActivityLogs.Where(l => l.ActorId == userId);
            context.ActivityLogs.RemoveRange(logs);

            var profiles = context.UserAiProfiles.Where(p => p.UserId == userId);
            context.UserAiProfiles.RemoveRange(profiles);

            var tokens = context.RefreshTokens.Where(rt => rt.UserId == userId);
            context.RefreshTokens.RemoveRange(tokens);

            var notifications = context.Notifications.Where(n => n.UserId == userId);
            context.Notifications.RemoveRange(notifications);

            await context.SaveChangesAsync();
        }
    }
}
