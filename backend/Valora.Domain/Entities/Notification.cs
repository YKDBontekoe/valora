using System;
using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public class Notification : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public string? ActionUrl { get; set; }
    public NotificationType Type { get; set; }
}

public enum NotificationType
{
    Info,
    PriceDrop,
    NewListing,
    System
}
