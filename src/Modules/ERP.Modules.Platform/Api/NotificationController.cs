// <copyright file="NotificationController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.Platform.Api;

[ApiController]
[Route("api/v1/platform/notifications")]
public class NotificationController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public IActionResult GetNotifications([FromQuery] Guid? companyId)
    {
        var notifications = new List<NotificationDto>();
        return Ok(ApiResponse<object>.Success(new
        {
            Notifications = notifications,
            UnreadCount = notifications.Count(n => !n.IsRead),
            Summary = new
            {
                PendingApprovals = 0,
                OverdueItems = 0,
                SystemAlerts = 0,
                UpcomingDeadlines = 0,
            }
        }));
    }

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public IActionResult MarkAsRead(Guid id)
    {
        return Ok(ApiResponse<object>.Success(new { message = "Marked as read" }));
    }

    [HttpPost("read-all")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public IActionResult MarkAllAsRead()
    {
        return Ok(ApiResponse<object>.Success(new { message = "All marked as read" }));
    }
}

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public Uri? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public string? Priority { get; set; }
}
