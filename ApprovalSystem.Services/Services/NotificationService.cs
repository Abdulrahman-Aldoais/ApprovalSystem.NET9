using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApprovalSystem.Core.Interfaces;
using ApprovalSystem.Infrastructure.Data;
using ApprovalSystem.Models.Entities;
using ApprovalSystem.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ApprovalSystem.Services.Services;

/// <summary>
/// خدمة إدارة الإشعارات
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(List<Notification> notifications, int totalCount)> GetNotificationsAsync(
        string userId, Guid tenantId, int pageNumber, int pageSize,
        bool? unreadOnly = null, string? priority = null, string? type = null)
    {
        try
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId && n.TenantId == tenantId);

            if (unreadOnly == true)
                query = query.Where(n => !n.IsRead);

            if (!string.IsNullOrEmpty(priority))
                query = query.Where(n => n.Priority == priority);

            if (!string.IsNullOrEmpty(type))
                query = query.Where(n => n.Type == type);

            var totalCount = await query.CountAsync();

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} notifications for user {UserId}", notifications.Count, userId);

            return (notifications, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Notification?> GetNotificationByIdAsync(Guid notificationId, string userId, Guid tenantId)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId && n.TenantId == tenantId);

            if (notification != null)
            {
                _logger.LogInformation("Retrieved notification {NotificationId}", notificationId);
            }

            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification {NotificationId}", notificationId);
            throw;
        }
    }

    public async Task<List<Notification>> CreateNotificationAsync(
        string title, string message, List<string> userIds, string type, string priority,
        Dictionary<string, object>? data, Guid tenantId, string senderId)
    {
        try
        {
            var notifications = new List<Notification>();

            foreach (var userId in userIds)
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    Priority = priority,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    TenantId = tenantId,
                    Data = data != null ? System.Text.Json.JsonSerializer.Serialize(data) : null,
                    CreatedBy = senderId
                };

                notifications.Add(notification);
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Created {Count} notifications for {UserCount} users", notifications.Count, userIds.Count);

            return notifications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notifications for users");
            throw;
        }
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId, string userId, Guid tenantId)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId && n.TenantId == tenantId);

            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found for user {UserId}", notificationId, userId);
                return false;
            }

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Marked notification {NotificationId} as read", notificationId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
            return false;
        }
    }

    public async Task<int> MarkAllAsReadAsync(string userId, Guid tenantId)
    {
        try
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.TenantId == tenantId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Marked {Count} notifications as read for user {UserId}", unreadNotifications.Count, userId);

            return unreadNotifications.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            return 0;
        }
    }

    public async Task<bool> DeleteNotificationAsync(Guid notificationId, string userId, Guid tenantId)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId && n.TenantId == tenantId);

            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found for user {UserId}", notificationId, userId);
                return false;
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted notification {NotificationId}", notificationId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId}", notificationId);
            return false;
        }
    }

    public async Task<int> CleanupReadNotificationsAsync(string userId, Guid tenantId)
    {
        try
        {
            // Keep only notifications from last 30 days that are read
            var cutoffDate = DateTime.UtcNow.AddDays(-30);
            
            var readNotificationsToDelete = await _context.Notifications
                .Where(n => n.UserId == userId && 
                           n.TenantId == tenantId && 
                           n.IsRead && 
                           n.ReadAt < cutoffDate)
                .ToListAsync();

            _context.Notifications.RemoveRange(readNotificationsToDelete);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} read notifications for user {UserId}", readNotificationsToDelete.Count, userId);

            return readNotificationsToDelete.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up read notifications for user {UserId}", userId);
            return 0;
        }
    }

    public async Task<int> GetUnreadCountAsync(string userId, Guid tenantId)
    {
        try
        {
            var count = await _context.Notifications
                .CountAsync(n => n.UserId == userId && n.TenantId == tenantId && !n.IsRead);

            _logger.LogDebug("User {UserId} has {Count} unread notifications", userId, count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
            return 0;
        }
    }

    public async Task SendApprovalNotificationAsync(Guid requestId, string approverId, string action, string? comments)
    {
        try
        {
            var request = await _context.Requests
                .Include(r => r.Requester)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null) return;

            var actionText = action switch
            {
                "Approved" => "تمت الموافقة على",
                "Rejected" => "تم رفض",
                "Escalated" => "تم تصعيد",
                _ => "تم تحديث حالة"
            };

            var title = $"إشعار موافقة";
            var message = $"{actionText} طلبك: {request.Title}";
            
            if (!string.IsNullOrEmpty(comments))
                message += $"\nالتعليق: {comments}";

            var data = new Dictionary<string, object>
            {
                { "requestId", requestId },
                { "action", action },
                { "comments", comments ?? "" }
            };

            await CreateNotificationAsync(title, message, new List<string> { approverId }, 
                "approval", "medium", data, request.TenantId, request.RequesterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending approval notification for request {RequestId}", requestId);
        }
    }

    public async Task SendApprovalNotificationsAsync(Request request)
    {
        try
        {
            var approvals = await _context.Approvals
                .Include(a => a.Approver)
                .Where(a => a.RequestId == request.Id && a.Status == "Pending")
                .ToListAsync();

            var userIds = approvals.Select(a => a.ApproverId).Distinct().ToList();

            if (!userIds.Any()) return;

            var title = "طلب يحتاج موافقة";
            var message = $"لديك طلب جديد يحتاج موافقة: {request.Title}";
            
            var data = new Dictionary<string, object>
            {
                { "requestId", request.Id },
                { "requestTitle", request.Title },
                { "priority", request.Priority },
                { "dueDate", request.DueDate?.ToString("yyyy-MM-dd") ?? "" }
            };

            await CreateNotificationAsync(title, message, userIds, "approval", request.Priority, 
                data, request.TenantId, request.RequesterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending approval notifications for request {RequestId}", request.Id);
        }
    }

    public async Task SendRequestStatusNotificationAsync(Guid requestId, string fromStatus, string toStatus, string actorId)
    {
        try
        {
            var request = await _context.Requests
                .Include(r => r.Requester)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null) return;

            var title = "تحديث حالة الطلب";
            var message = $"تم تحديث حالة طلبك من '{fromStatus}' إلى '{toStatus}': {request.Title}";

            var data = new Dictionary<string, object>
            {
                { "requestId", requestId },
                { "fromStatus", fromStatus },
                { "toStatus", toStatus },
                { "actorId", actorId }
            };

            await CreateNotificationAsync(title, message, new List<string> { request.RequesterId }, 
                "status", "medium", data, request.TenantId, actorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending status notification for request {RequestId}", requestId);
        }
    }

    public async Task SendApprovalReminderNotificationAsync(Guid approvalId)
    {
        try
        {
            var approval = await _context.Approvals
                .Include(a => a.Approver)
                .Include(a => a.Request)
                .FirstOrDefaultAsync(a => a.Id == approvalId);

            if (approval == null) return;

            var title = "تذكير بموافقة معلقة";
            var message = $"لديك موافقة معلقة للطلب: {approval.Request.Title}";
            
            if (approval.Request.DueDate.HasValue)
            {
                var daysLeft = (approval.Request.DueDate.Value - DateTime.UtcNow).Days;
                message += $"\nالمتبقي {daysLeft} يوم على الموعد النهائي";
            }

            var data = new Dictionary<string, object>
            {
                { "approvalId", approvalId },
                { "requestId", approval.RequestId },
                { "requestTitle", approval.Request.Title }
            };

            await CreateNotificationAsync(title, message, new List<string> { approval.ApproverId }, 
                "reminder", "high", data, approval.TenantId, "system");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reminder notification for approval {ApprovalId}", approvalId);
        }
    }

    public async Task SendEscalationNotificationAsync(Guid requestId, string escalatedBy, string escalatedTo, string reason)
    {
        try
        {
            var request = await _context.Requests
                .Include(r => r.Requester)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null) return;

            // Notify escalated to
            var toTitle = "طلب تم تصعيده إليك";
            var toMessage = $"تم تصعيد طلب إليك: {request.Title}\nالسبب: {reason}";
            
            var toData = new Dictionary<string, object>
            {
                { "requestId", requestId },
                { "escalatedBy", escalatedBy },
                { "escalatedTo", escalatedTo },
                { "reason", reason }
            };

            await CreateNotificationAsync(toTitle, toMessage, new List<string> { escalatedTo }, 
                "escalation", "high", toData, request.TenantId, escalatedBy);

            // Notify escalated by
            var byTitle = "تم تصعيد طلب";
            var byMessage = $"تم تصعيد طلبك إلى {escalatedTo}: {request.Title}";

            await CreateNotificationAsync(byTitle, byMessage, new List<string> { escalatedBy }, 
                "escalation", "medium", toData, request.TenantId, escalatedTo);

            // Notify requester
            var requesterTitle = "تم تصعيد طلبك";
            var requesterMessage = $"تم تصعيد طلبك لمزيد من المراجعة: {request.Title}";

            await CreateNotificationAsync(requesterTitle, requesterMessage, new List<string> { request.RequesterId }, 
                "escalation", "medium", toData, request.TenantId, escalatedBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending escalation notification for request {RequestId}", requestId);
        }
    }

    public async Task SendSystemNotificationAsync(string title, string message, Guid tenantId, string? senderId = null)
    {
        try
        {
            var users = await _context.Users
                .Where(u => u.TenantId == tenantId)
                .Select(u => u.Id)
                .ToListAsync();

            if (!users.Any()) return;

            await CreateNotificationAsync(title, message, users, "system", "medium", 
                null, tenantId, senderId ?? "system");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending system notification");
        }
    }

    public async Task<Notification?> SendTestNotificationAsync(
        string userId, Guid tenantId, string message, string type = "info", string priority = "medium")
    {
        try
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = "إشعار تجريبي",
                Message = message,
                Type = type,
                Priority = priority,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                TenantId = tenantId,
                CreatedBy = userId
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Sent test notification to user {UserId}", userId);

            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test notification to user {UserId}", userId);
            return null;
        }
    }

    public async Task<NotificationSettings?> GetNotificationSettingsAsync(string userId, Guid tenantId)
    {
        try
        {
            // For now, return default settings since NotificationPreferences table might not exist
            // In a real implementation, this would fetch from database
            return new NotificationSettings
            {
                EmailEnabled = true,
                PushEnabled = true,
                SmsEnabled = false,
                Types = new List<string> { "all" },
                WorkStartHour = 8,
                WorkEndHour = 17,
                QuietHoursEnabled = false,
                QuietMinutes = 30
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification settings for user {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> UpdateNotificationSettingsAsync(
        string userId, Guid tenantId, bool emailEnabled, bool pushEnabled, bool smsEnabled, List<string>? types)
    {
        try
        {
            // In a real implementation, this would update the database
            // For now, just log the settings
            _logger.LogInformation("Updating notification settings for user {UserId}: Email={EmailEnabled}, Push={PushEnabled}, SMS={SmsEnabled}", 
                userId, emailEnabled, pushEnabled, smsEnabled);

            // TODO: Implement actual database update when NotificationPreferences entity is added
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification settings for user {UserId}", userId);
            return false;
        }
    }

    public async Task<NotificationStatsViewModel> GetNotificationStatsAsync(Guid tenantId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var notifications = await _context.Notifications
                .Where(n => n.TenantId == tenantId && n.CreatedAt >= startDate && n.CreatedAt <= endDate)
                .ToListAsync();

            var stats = new NotificationStatsViewModel
            {
                TotalNotifications = notifications.Count,
                ReadNotifications = notifications.Count(n => n.IsRead),
                UnreadNotifications = notifications.Count(n => !n.IsRead),
                ByType = notifications.GroupBy(n => n.Type).ToDictionary(g => g.Key, g => g.Count()),
                ByPriority = notifications.GroupBy(n => n.Priority).ToDictionary(g => g.Key, g => g.Count()),
                DailyCounts = notifications.GroupBy(n => n.CreatedAt.Date)
                    .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count())
            };

            _logger.LogInformation("Retrieved notification stats for tenant {TenantId}", tenantId);

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification stats for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<NotificationRule?> CreateNotificationRuleAsync(
        string name, string triggerEvent, Dictionary<string, object>? conditions, 
        List<NotificationAction> actions, bool isActive, int priority, Guid tenantId)
    {
        try
        {
            // This would be implemented when NotificationRules entity exists
            _logger.LogInformation("Creating notification rule: {Name} for event: {Event}", name, triggerEvent);
            
            // Return a mock rule for now
            return new NotificationRule
            {
                Id = Guid.NewGuid(),
                Name = name,
                TriggerEvent = triggerEvent,
                Conditions = conditions,
                Actions = actions,
                IsActive = isActive,
                Priority = priority,
                TenantId = tenantId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification rule");
            throw;
        }
    }

    public async Task<List<NotificationRule>> GetNotificationRulesAsync(Guid tenantId, bool? activeOnly = null)
    {
        try
        {
            // This would query NotificationRules table
            _logger.LogInformation("Getting notification rules for tenant {TenantId}", tenantId);
            
            return new List<NotificationRule>(); // Return empty list for now
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification rules for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<bool> UpdateNotificationRuleAsync(Guid ruleId, string name, string triggerEvent,
        Dictionary<string, object>? conditions, List<NotificationAction> actions, bool isActive, int priority)
    {
        try
        {
            _logger.LogInformation("Updating notification rule {RuleId}", ruleId);
            
            // TODO: Implement actual update
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification rule {RuleId}", ruleId);
            return false;
        }
    }

    public async Task<bool> DeleteNotificationRuleAsync(Guid ruleId)
    {
        try
        {
            _logger.LogInformation("Deleting notification rule {RuleId}", ruleId);
            
            // TODO: Implement actual deletion
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification rule {RuleId}", ruleId);
            return false;
        }
    }

    public async Task ProcessNotificationRulesAsync(string eventType, Dictionary<string, object> eventData, Guid tenantId)
    {
        try
        {
            _logger.LogInformation("Processing notification rules for event: {EventType}", eventType);
            
            // Get active rules for this event
            var rules = await GetNotificationRulesAsync(tenantId, true);
            
            foreach (var rule in rules.Where(r => r.TriggerEvent == eventType))
            {
                // TODO: Implement rule processing logic
                _logger.LogDebug("Processing rule: {RuleName}", rule.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notification rules for event {EventType}", eventType);
        }
    }

    public async Task SendPeriodicNotificationsAsync(Guid tenantId)
    {
        try
        {
            // Send daily digest
            await SendSystemNotificationAsync("تقرير يومي", "إليك ملخص اليوم", tenantId);
            
            // Send pending approval reminders
            var pendingApprovals = await _context.Approvals
                .Include(a => a.Request)
                .Where(a => a.TenantId == tenantId && a.Status == "Pending")
                .ToListAsync();

            foreach (var approval in pendingApprovals.Where(a => a.Request.DueDate.HasValue && 
                a.Request.DueDate <= DateTime.UtcNow.AddHours(24)))
            {
                await SendApprovalReminderNotificationAsync(approval.Id);
            }

            _logger.LogInformation("Sent periodic notifications for tenant {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending periodic notifications for tenant {TenantId}", tenantId);
        }
    }
}