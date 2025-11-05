using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ApprovalSystem.Models.Entities;
using ApprovalSystem.Models.ViewModels;
using ApprovalSystem.ViewModels;
using ApprovalSystem.Core.Interfaces;
using ApprovalSystem.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ApprovalSystem.API.Controllers;

/// <summary>
/// Controller لإدارة الإشعارات
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        IEmailService emailService,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// الحصول على قائمة الإشعارات
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? unreadOnly = null,
        [FromQuery] string? priority = null,
        [FromQuery] string? type = null)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

            if (string.IsNullOrEmpty(userId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            var (notifications, totalCount) = await _notificationService.GetNotificationsAsync(
                userId, tenantId, pageNumber, pageSize, unreadOnly, priority, type);

            var notificationViewModels = notifications.Select(n => new NotificationViewModel
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                Priority = n.Priority,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                ReadAt = n.ReadAt,
                Data = n.GetData<Dictionary<string, object>>(),
                Age = n.Age
            }).ToList();

            var response = new PagedResponse<NotificationViewModel>
            {
                Items = notificationViewModels,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications");
            return StatusCode(500, new { message = "حدث خطأ أثناء جلب الإشعارات" });
        }
    }

    /// <summary>
    /// الحصول على إشعار محدد
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetNotification(Guid id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

            if (string.IsNullOrEmpty(userId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            var notification = await _notificationService.GetNotificationByIdAsync(id, userId, tenantId);
            if (notification == null)
            {
                return NotFound(new { message = "الإشعار غير موجود" });
            }

            var notificationViewModel = new NotificationDetailViewModel
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                Priority = notification.Priority,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt,
                ReadAt = notification.ReadAt,
                Data = notification.GetData<Dictionary<string, object>>(),
                Age = notification.Age,
                IsOverdue = notification.IsOverdue
            };

            return Ok(notificationViewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification {NotificationId}", id);
            return StatusCode(500, new { message = "حدث خطأ أثناء جلب الإشعار" });
        }
    }

    /// <summary>
    /// وضع علامة قراءة على الإشعار
    /// </summary>
    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

            if (string.IsNullOrEmpty(userId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            var result = await _notificationService.MarkAsReadAsync(id, userId, tenantId);
            if (!result)
            {
                return NotFound(new { message = "الإشعار غير موجود أو ليس لديك صلاحية للوصول إليه" });
            }

            _logger.LogInformation("Notification {NotificationId} marked as read by user {UserId}", id, userId);

            return Ok(new { message = "تم وضع علامة قراءة على الإشعار" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
            return StatusCode(500, new { message = "حدث خطأ أثناء تحديث الإشعار" });
        }
    }

    /// <summary>
    /// وضع علامة قراءة على جميع الإشعارات
    /// </summary>
    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

            if (string.IsNullOrEmpty(userId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            var count = await _notificationService.MarkAllAsReadAsync(userId, tenantId);

            _logger.LogInformation("Marked {Count} notifications as read for user {UserId}", count, userId);

            return Ok(new { message = $"تم وضع علامة قراءة على {count} إشعار", count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return StatusCode(500, new { message = "حدث خطأ أثناء تحديث الإشعارات" });
        }
    }

    /// <summary>
    /// حذف إشعار
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(Guid id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

            if (string.IsNullOrEmpty(userId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            var result = await _notificationService.DeleteNotificationAsync(id, userId, tenantId);
            if (!result)
            {
                return NotFound(new { message = "الإشعار غير موجود أو ليس لديك صلاحية لحذفه" });
            }

            _logger.LogInformation("Notification {NotificationId} deleted by user {UserId}", id, userId);

            return Ok(new { message = "تم حذف الإشعار بنجاح" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId}", id);
            return StatusCode(500, new { message = "حدث خطأ أثناء حذف الإشعار" });
        }
    }

    /// <summary>
    /// حذف جميع الإشعارات المقروءة
    /// </summary>
    [HttpDelete("cleanup-read")]
    public async Task<IActionResult> CleanupReadNotifications()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

            if (string.IsNullOrEmpty(userId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            var count = await _notificationService.CleanupReadNotificationsAsync(userId, tenantId);

            _logger.LogInformation("Cleaned up {Count} read notifications for user {UserId}", count, userId);

            return Ok(new { message = $"تم حذف {count} إشعار مقروء", count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up read notifications");
            return StatusCode(500, new { message = "حدث خطأ أثناء تنظيف الإشعارات" });
        }
    }

    /// <summary>
    /// الحصول على عدد الإشعارات غير المقروءة
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

            if (string.IsNullOrEmpty(userId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            var count = await _notificationService.GetUnreadCountAsync(userId, tenantId);

            return Ok(new { unreadCount = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread notifications count");
            return StatusCode(500, new { message = "حدث خطأ أثناء جلب عدد الإشعارات غير المقروءة" });
        }
    }

    /// <summary>
    /// إنشاء إشعار مخصص
    /// </summary>
    [HttpPost("create")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "بيانات الإشعار غير صحيحة", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
        }

        try
        {
            var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

            if (string.IsNullOrEmpty(senderId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            var notification = await _notificationService.CreateNotificationAsync(
                model.Title, model.Message, model.UserIds, model.Type, model.Priority, 
                model.Data, tenantId, senderId);

            if (notification == null || notification.Count == 0)
            {
                return BadRequest(new { message = "فشل في إنشاء الإشعارات. تحقق من صحة بيانات المستخدمين." });
            }

            // Send email if configured
            if (model.SendEmail)
            {
                await _emailService.SendBulkNotificationsAsync(notification);
            }

            _logger.LogInformation("Created {Count} custom notifications by {UserId}", notification.Count, senderId);

            return CreatedAtAction(nameof(GetNotifications), new { pageNumber = 1 }, new
            {
                message = $"تم إنشاء {notification.Count} إشعار بنجاح",
                createdCount = notification.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating custom notification");
            return StatusCode(500, new { message = "حدث خطأ أثناء إنشاء الإشعار" });
        }
    }

    /// <summary>
    /// تحديث إعدادات الإشعارات للمستخدم
    /// </summary>
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateNotificationSettings([FromBody] UpdateNotificationSettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "بيانات الإعدادات غير صحيحة", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
        }

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

            if (string.IsNullOrEmpty(userId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            var result = await _notificationService.UpdateNotificationSettingsAsync(
                userId, tenantId, model.EmailEnabled, model.PushEnabled, model.SmsEnabled, model.Types);

            if (!result)
            {
                return BadRequest(new { message = "فشل في تحديث إعدادات الإشعارات" });
            }

            _logger.LogInformation("Notification settings updated for user {UserId}", userId);

            return Ok(new { message = "تم تحديث إعدادات الإشعارات بنجاح" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification settings");
            return StatusCode(500, new { message = "حدث خطأ أثناء تحديث الإعدادات" });
        }
    }

    /// <summary>
    /// الحصول على إعدادات الإشعارات للمستخدم
    /// </summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetNotificationSettings()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

            if (string.IsNullOrEmpty(userId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            var settings = await _notificationService.GetNotificationSettingsAsync(userId, tenantId);

            var response = new NotificationSettingsViewModel
            {
                EmailEnabled = settings.EmailEnabled,
                PushEnabled = settings.PushEnabled,
                SmsEnabled = settings.SmsEnabled,
                Types = settings.Types ?? new List<string> { "all" }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification settings");
            return StatusCode(500, new { message = "حدث خطأ أثناء جلب إعدادات الإشعارات" });
        }
    }

    /// <summary>
    /// إرسال إشعار اختبار
    /// </summary>
    [HttpPost("send-test")]
    public async Task<IActionResult> SendTestNotification([FromBody] SendTestNotificationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "بيانات الإشعار الاختباري غير صحيحة", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
        }

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

            if (string.IsNullOrEmpty(userId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            var notification = await _notificationService.SendTestNotificationAsync(
                userId, tenantId, model.Message, model.Type, model.Priority);

            if (notification == null)
            {
                return BadRequest(new { message = "فشل في إرسال الإشعار الاختباري" });
            }

            // Send email if requested
            if (model.SendEmail)
            {
                await _emailService.SendTestNotificationAsync(notification);
            }

            _logger.LogInformation("Test notification sent to user {UserId}", userId);

            return Ok(new { message = "تم إرسال الإشعار الاختباري بنجاح" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test notification");
            return StatusCode(500, new { message = "حدث خطأ أثناء إرسال الإشعار الاختباري" });
        }
    }

    /// <summary>
    /// الحصول على إحصائيات الإشعارات
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetNotificationStats(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

            if (string.IsNullOrEmpty(userId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            var stats = await _notificationService.GetNotificationStatsAsync(
                tenantId, startDate ?? DateTime.UtcNow.AddDays(-30), endDate ?? DateTime.UtcNow);

            var response = new NotificationStatsViewModel
            {
                TotalNotifications = stats.TotalNotifications,
                ReadNotifications = stats.ReadNotifications,
                UnreadNotifications = stats.UnreadNotifications,
                ByType = stats.ByType,
                ByPriority = stats.ByPriority,
                DailyCounts = stats.DailyCounts?.Select(d => new DailyCountViewModel
                {
                    Date = d.Date,
                    Count = d.Count
                }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification stats");
            return StatusCode(500, new { message = "حدث خطأ أثناء جلب إحصائيات الإشعارات" });
        }
    }
}
