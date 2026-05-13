using Giwu.Domain.Notifications;

namespace Giwu.Contracts.Notifications;

public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Body,
    Guid? RelatedEntityId,
    string? RelatedEntityType,
    DateTimeOffset? ReadAt,
    DateTimeOffset CreatedAt)
{
    public bool IsRead => ReadAt.HasValue;
}

public sealed record NotificationSummary(
    int UnreadCount,
    IReadOnlyList<NotificationDto> Items);
