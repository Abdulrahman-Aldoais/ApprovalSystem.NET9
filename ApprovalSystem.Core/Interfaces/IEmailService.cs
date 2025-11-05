using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApprovalSystem.Models.Entities;

namespace ApprovalSystem.Core.Interfaces;

/// <summary>
/// واجهة خدمة البريد الإلكتروني
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// إرسال رسالة بريد إلكتروني
    /// </summary>
    Task<bool> SendEmailAsync(string to, string subject, string htmlBody, string? textBody = null, List<string>? attachments = null);

    /// <summary>
    /// إرسال رسالة HTML
    /// </summary>
    Task<bool> SendHtmlEmailAsync(string to, string subject, string htmlBody, string? fromName = null);

    /// <summary>
    /// إرسال رسالة نصية
    /// </summary>
    Task<bool> SendTextEmailAsync(string to, string subject, string textBody);

    /// <summary>
    /// إرسال رسائل متعددة
    /// </summary>
    Task<bool> SendBulkEmailsAsync(List<EmailMessage> emails);

    /// <summary>
    /// إرسال رسالة تجريبية
    /// </summary>
    Task<bool> SendTestEmailAsync(string to, string subject, string message);

    /// <summary>
    /// إرسال إشعار موافقة
    /// </summary>
    Task<bool> SendApprovalEmailAsync(Request request, Approval approval, string action);

    /// <summary>
    /// إرسال إشعار رفض
    /// </summary>
    Task<bool> SendRejectionEmailAsync(Request request, Approval approval, string reason);

    /// <summary>
    /// إرسال إشعار تصعيد
    /// </summary>
    Task<bool> SendEscalationEmailAsync(Request request, ApprovalEscalation escalation);

    /// <summary>
    /// إرسال تذكير بالموافقة
    /// </summary>
    Task<bool> SendApprovalReminderEmailAsync(Approval approval);

    /// <summary>
    /// إرسال إشعار النظام العام
    /// </summary>
    Task<bool> SendSystemEmailAsync(string subject, string message, List<string> recipients);

    /// <summary>
    /// إرسال إشعار جماعي للإشعارات
    /// </summary>
    Task<bool> SendBulkNotificationsAsync(List<Notification> notifications);

    /// <summary>
    /// إرسال إشعار اختبار
    /// </summary>
    Task<bool> SendTestNotificationAsync(Notification notification);

    /// <summary>
    /// التحقق من صحة إعدادات البريد
    /// </summary>
    Task<bool> ValidateEmailSettingsAsync();

    /// <summary>
    /// الحصول على حالة خدمة البريد
    /// </summary>
    Task<EmailServiceStatus> GetServiceStatusAsync();

    /// <summary>
    /// الحصول على سجل الرسائل المرسلة
    /// </summary>
    Task<List<EmailLog>> GetEmailLogsAsync(DateTime startDate, DateTime endDate, int page = 1, int pageSize = 50);

    /// <summary>
    /// إعادة محاولة إرسال رسالة فاشلة
    /// </summary>
    Task<bool> RetryFailedEmailAsync(Guid emailId);

    /// <summary>
    /// إلغاء رسالة معلقة
    /// </summary>
    Task<bool> CancelPendingEmailAsync(Guid emailId);
}

/// <summary>
/// نموذج رسالة البريد الإلكتروني
/// </summary>
public class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? TextBody { get; set; }
    public string? From { get; set; }
    public string? FromName { get; set; }
    public List<string> Attachments { get; set; } = new();
    public Dictionary<string, object> TemplateData { get; set; } = new();
    public string? TemplateId { get; set; }
    public DateTime? ScheduledTime { get; set; }
    public string Priority { get; set; } = "Normal"; // Normal, High, Low
}

/// <summary>
/// نموذج حالة خدمة البريد
/// </summary>
public class EmailServiceStatus
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public string LastCheckTime { get; set; } = string.Empty;
    public int FailedEmailsCount { get; set; }
    public int PendingEmailsCount { get; set; }
    public int SentEmailsToday { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// نموذج سجل البريد الإلكتروني
/// </summary>
public class EmailLog
{
    public Guid Id { get; set; }
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Sent, Failed, Pending, Cancelled
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public string? FromAddress { get; set; }
    public string Priority { get; set; } = "Normal";
    public int AttachmentCount { get; set; }
}
