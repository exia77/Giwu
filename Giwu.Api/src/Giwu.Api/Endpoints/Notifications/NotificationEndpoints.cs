using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Notifications;
using MediatR;

namespace Giwu.Api.Endpoints.Notifications;

public sealed class ListNotificationsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/notifications", Handle)
           .RequireAuthorization()
           .WithTags("Notifications");

    private static async Task<IResult> Handle(
        IMediator m, bool unreadOnly = false, int limit = 50, CancellationToken ct = default) =>
        (await m.Send(new ListNotificationsQuery(unreadOnly, limit), ct)).ToHttp();
}

public sealed class MarkNotificationReadEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/notifications/{id:guid}/mark-read", Handle)
           .RequireAuthorization()
           .WithTags("Notifications");

    private static async Task<IResult> Handle(Guid id, IMediator m, CancellationToken ct) =>
        (await m.Send(new MarkNotificationReadCommand(id), ct)).ToHttp();
}

public sealed class MarkAllNotificationsReadEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/notifications/mark-all-read", Handle)
           .RequireAuthorization()
           .WithTags("Notifications");

    private static async Task<IResult> Handle(IMediator m, CancellationToken ct) =>
        (await m.Send(new MarkAllNotificationsReadCommand(), ct)).ToHttp();
}
