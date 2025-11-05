using ApprovalSystem.Models.Entities;
using ApprovalSystem.ViewModels;

namespace ApprovalSystem.Core.Interfaces;

/// <summary>
/// واجهة خدمة إدارة الإشعارات
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// الحصول على قائمة الإشعارات
    /// </summary>
    Task<(List<Notification> notifications, int totalCount)> GetNotificationsAsync(
        string userId, Guid tenantId, int pageNumber, int pageSize,
        bool? unreadOnly = null, string? priority = null, string? type = null);

    /// <summary>
    /// الحصول على إشعار محدد
    /// </summary>
    Task<Notification?> GetNotificationByIdAsync(Guid notificationId, string userId, Guid tenantId);

    /// <summary>
    /// إنشاء إشعار جديد
    /// </summary>
    Task<List<Notification>> CreateNotificationAsync(
        string title, string message, List<string> userIds, string type, string priority,
        Dictionary<string, object>? data, Guid tenantId, string senderId);

    /// <summary>
    /// وضع علامة قراءة على إشعار
    /// </summary>
    Task<bool> MarkAsReadAsync(Guid notificationId, string userId, Guid tenantId);

    /// <summary>
    /// وضع علامة قراءة على جميع الإشعارات
    /// </summary>
    Task<int> MarkAllAsReadAsync(string userId, Guid tenantId);

    /// <summary>
    /// حذف إشعار
    /// </summary>
    Task<bool> DeleteNotificationAsync(Guid notificationId, string userId, Guid tenantId);

    /// <summary>
    /// تنظيف الإشعارات المقروءة
    /// </summary>
    Task<int> CleanupReadNotificationsAsync(string userId, Guid tenantId);

    /// <summary>
    /// الحصول على عدد الإشعارات غير المقروءة
    /// </summary>
    Task<int> GetUnreadCountAsync(string userId, Guid tenantId);

    /// <summary>
    /// إرسال إشعار بموافقة الطلب
    /// </summary>
    Task SendApprovalNotificationAsync(Guid requestId, string approverId, string action, string? comments);

    /// <summary>
    /// إرسال إشعارات الموافقات للقائمين عليها
    /// </summary>
    Task SendApprovalNotificationsAsync(Request request);

    /// <summary>
    /// إرسال إشعار حالة الطلب
    /// </summary>
    Task SendRequestStatusNotificationAsync(Guid requestId, string fromStatus, string toStatus, string actorId);

    /// <summary>
    /// إرسال إشعار تذكير بالموافقة
    /// </summary>
    Task SendApprovalReminderNotificationAsync(Guid approvalId);

    /// <summary>
    /// إرسال إشعار تصعيد
    /// </summary>
    Task SendEscalationNotificationAsync(Guid requestId, string escalatedBy, string escalatedTo, string reason);

    /// <summary>
    /// إرسال إشعار النظام العام
    /// </summary>
    Task SendSystemNotificationAsync(string title, string message, Guid tenantId, string? senderId = null);

    /// <summary>
    /// إرسال إشعار اختبار
    /// </summary>
    Task<Notification?> SendTestNotificationAsync(
        string userId, Guid tenantId, string message, string type = "info", string priority = "medium");

    /// <summary>
    /// الحصول على إعدادات الإشعارات
    /// </summary>
    Task<NotificationSettings?> GetNotificationSettingsAsync(string userId, Guid tenantId);

    /// <summary>
    /// تحديث إعدادات الإشعارات
    /// </summary>
    Task<bool> UpdateNotificationSettingsAsync(
        string userId, Guid tenantId, bool emailEnabled, bool pushEnabled, bool smsEnabled, List<string>? types);

    /// <summary>
    /// الحصول على إحصائيات الإشعارات
    /// </summary>
    Task<NotificationStatsViewModel> GetNotificationStatsAsync(Guid tenantId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// إنشاء قاعدة إشعار
    /// </summary>
    Task<NotificationRule?> CreateNotificationRuleAsync(
        string name, string triggerEvent, Dictionary<string, object>? conditions,
        List<NotificationAction> actions, bool isActive, int priority, Guid tenantId);

    /// <summary>
    /// الحصول على قواعد الإشعارات
    /// </summary>
    Task<List<NotificationRule>> GetNotificationRulesAsync(Guid tenantId, bool? activeOnly = null);

    /// <summary>
    /// تحديث قاعدة إشعار
    /// </summary>
    Task<bool> UpdateNotificationRuleAsync(Guid ruleId, string name, string triggerEvent,
        Dictionary<string, object>? conditions, List<NotificationAction> actions, bool isActive, int priority);

    /// <summary>
    /// حذف قاعدة إشعار
    /// </summary>
    Task<bool> DeleteNotificationRuleAsync(Guid ruleId);

    /// <summary>
    /// تنفيذ قواعد الإشعارات
    /// </summary>
    Task ProcessNotificationRulesAsync(string eventType, Dictionary<string, object> eventData, Guid tenantId);

    /// <summary>
    /// إرسال إشعارات دورية
    /// </summary>
    Task SendPeriodicNotificationsAsync(Guid tenantId);
}

/// <summary>
/// نموذج إعدادات الإشعارات
/// </summary>
public class NotificationSettings
{
    public bool EmailEnabled { get; set; } = true;
    public bool PushEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = false;
    public List<string> Types { get; set; } = new() { "all" };
    public int? WorkStartHour { get; set; } = 8;
    public int? WorkEndHour { get; set; } = 17;
    public bool QuietHoursEnabled { get; set; } = false;
    public int? QuietMinutes { get; set; } = 30;
}

/// <summary>
/// نموذج قاعدة الإشعار
/// </summary>
public class NotificationRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string TriggerEvent { get; set; } = string.Empty;
    public Dictionary<string, object>? Conditions { get; set; }
    public List<NotificationAction> Actions { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}

/// <summary>
/// نموذج إجراء الإشعار
/// </summary>
public class NotificationAction
{
    public string Type { get; set; } = string.Empty; // send_email, send_sms, send_push
    public List<string> Recipients { get; set; } = new();
    public string? TemplateId { get; set; }
    public Dictionary<string, object>? Variables { get; set; }
    public int? DelaySeconds { get; set; }
}

/// <summary>
/// نموذج إحصائيات الإشعارات
/// </summary>
public class NotificationStats
{
    public int TotalNotifications { get; set; }
    public int ReadNotifications { get; set; }
    public int UnreadNotifications { get; set; }
    public Dictionary<string, int> ByType { get; set; } = new();
    public Dictionary<string, int> ByPriority { get; set; } = new();
    public Dictionary<string, int> DailyCounts { get; set; } = new();
}
