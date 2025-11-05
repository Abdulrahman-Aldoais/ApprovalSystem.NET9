using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ApprovalSystem.ViewModels;

/// <summary>
/// ViewModels لإدارة الإشعارات
/// </summary>

/// <summary>
/// نموذج إنشاء إشعار جديد
/// </summary>
public class CreateNotificationViewModel
{
    [Required(ErrorMessage = "عنوان الإشعار مطلوب")]
    [Display(Name = "عنوان الإشعار")]
    [MaxLength(255, ErrorMessage = "عنوان الإشعار يجب ألا يزيد عن 255 حرف")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "نص الرسالة مطلوب")]
    [Display(Name = "نص الرسالة")]
    [MaxLength(2000, ErrorMessage = "نص الرسالة يجب ألا يزيد عن 2000 حرف")]
    public string Message { get; set; } = string.Empty;

    [Display(Name = "معرفات المستخدمين")]
    public List<string> UserIds { get; set; } = new();

    [Display(Name = "نوع الإشعار")]
    public string Type { get; set; } = "info"; // info, warning, success, error

    [Display(Name = "الأولوية")]
    public string Priority { get; set; } = "medium"; // low, medium, high, urgent

    // بيانات إضافية (JSON)
    public Dictionary<string, object>? Data { get; set; }

    [Display(Name = "إرسال عبر البريد الإلكتروني")]
    public bool SendEmail { get; set; } = false;

    [Display(Name = "إرسال عبر الرسائل النصية")]
    public bool SendSms { get; set; } = false;

    [Display(Name = "إرسال عبر الإشعارات الفورية")]
    public bool SendPush { get; set; } = true;
}

/// <summary>
/// نموذج عرض الإشعار
/// </summary>
public class NotificationViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "info"; // info, warning, success, error
    public string Priority { get; set; } = "medium"; // low, medium, high, urgent
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public Dictionary<string, object>? Data { get; set; }
    public TimeSpan Age { get; set; }

    // Computed properties
    public string TypeDisplayName => Type switch
    {
        "info" => "معلومة",
        "warning" => "تحذير",
        "success" => "نجاح",
        "error" => "خطأ",
        "approval" => "موافقة",
        "rejection" => "رفض",
        "escalation" => "تصعيد",
        _ => Type
    };

    public string PriorityDisplayName => Priority switch
    {
        "low" => "منخفضة",
        "medium" => "متوسطة",
        "high" => "عالية",
        "urgent" => "عاجلة",
        _ => Priority
    };

    public string TimeAgo => CreatedAt switch
    {
        var dt when DateTime.UtcNow.Subtract(dt).TotalMinutes < 1 => "منذ لحظات",
        var dt when DateTime.UtcNow.Subtract(dt).TotalMinutes < 60 => $"منذ {Math.Floor(DateTime.UtcNow.Subtract(dt).TotalMinutes)} دقيقة",
        var dt when DateTime.UtcNow.Subtract(dt).TotalHours < 24 => $"منذ {Math.Floor(DateTime.UtcNow.Subtract(dt).TotalHours)} ساعة",
        var dt when DateTime.UtcNow.Subtract(dt).TotalDays < 7 => $"منذ {Math.Floor(DateTime.UtcNow.Subtract(dt).TotalDays)} أيام",
        _ => CreatedAt.ToString("yyyy/MM/dd")
    };

    public bool IsNew => DateTime.UtcNow.Subtract(CreatedAt).TotalHours < 24;
    public bool IsUrgent => Priority == "urgent" || Priority == "high";

    // CSS Classes for styling
    public string CssClass => $"notification notification--{Type} notification--{Priority}";
    public string IconClass => Type switch
    {
        "info" => "icon-info",
        "warning" => "icon-warning",
        "success" => "icon-success",
        "error" => "icon-error",
        "approval" => "icon-check",
        "rejection" => "icon-close",
        "escalation" => "icon-arrow-up",
        _ => "icon-bell"
    };
}

/// <summary>
/// نموذج تفاصيل الإشعار الكامل
/// </summary>
public class NotificationDetailViewModel : NotificationViewModel
{
    public bool IsOverdue { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }

    // Computed properties
    public bool HasAction => !string.IsNullOrEmpty(ActionUrl) && !string.IsNullOrEmpty(ActionText);
    public string PriorityBadgeClass => Priority switch
    {
        "low" => "badge badge-low",
        "medium" => "badge badge-medium",
        "high" => "badge badge-high",
        "urgent" => "badge badge-urgent",
        _ => "badge badge-medium"
    };
}

/// <summary>
/// نموذج تحديث إعدادات الإشعارات
/// </summary>
public class UpdateNotificationSettingsViewModel
{
    [Display(Name = "تفعيل إشعارات البريد الإلكتروني")]
    public bool EmailEnabled { get; set; } = true;

    [Display(Name = "تفعيل الإشعارات الفورية")]
    public bool PushEnabled { get; set; } = true;

    [Display(Name = "تفعيل الرسائل النصية")]
    public bool SmsEnabled { get; set; } = false;

    [Display(Name = "أنواع الإشعارات")]
    public List<string> Types { get; set; } = new() { "all" };

    [Display(Name = "ساعات العمل (بدءاً)")]
    public int? WorkStartHour { get; set; } = 8;

    [Display(Name = "ساعات العمل (انتهاءً)")]
    public int? WorkEndHour { get; set; } = 17;

    [Display(Name = "توقيت عدم الإزعاج")]
    public bool QuietHoursEnabled { get; set; } = false;

    [Display(Name = "دقائق عدم الإزعاج")]
    public int? QuietMinutes { get; set; } = 30;
}

/// <summary>
/// نموذج عرض إعدادات الإشعارات
/// </summary>
public class NotificationSettingsViewModel
{
    public bool EmailEnabled { get; set; }
    public bool PushEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public List<string> Types { get; set; } = new() { "all" };
    public int? WorkStartHour { get; set; }
    public int? WorkEndHour { get; set; }
    public bool QuietHoursEnabled { get; set; }
    public int? QuietMinutes { get; set; }
}

/// <summary>
/// نموذج إرسال إشعار اختبار
/// </summary>
public class SendTestNotificationViewModel
{
    [Required(ErrorMessage = "نص الرسالة مطلوب")]
    [Display(Name = "نص الرسالة")]
    [MaxLength(500, ErrorMessage = "نص الرسالة يجب ألا يزيد عن 500 حرف")]
    public string Message { get; set; } = string.Empty;

    [Display(Name = "نوع الإشعار")]
    public string Type { get; set; } = "info";

    [Display(Name = "الأولوية")]
    public string Priority { get; set; } = "medium";

    [Display(Name = "إرسال عبر البريد الإلكتروني")]
    public bool SendEmail { get; set; } = false;

    [Display(Name = "إرسال عبر الرسائل النصية")]
    public bool SendSms { get; set; } = false;
}

/// <summary>
/// نموذج إحصائيات الإشعارات
/// </summary>
public class NotificationStatsViewModel
{
    public int TotalNotifications { get; set; }
    public int ReadNotifications { get; set; }
    public int UnreadNotifications { get; set; }
    public Dictionary<string, int>? ByType { get; set; }
    public Dictionary<string, int>? ByPriority { get; set; }
    public List<DailyCountViewModel>? DailyCounts { get; set; }

    // Computed properties
    public double ReadRate => TotalNotifications > 0 ? (double)ReadNotifications / TotalNotifications * 100 : 0;
    public double UnreadRate => TotalNotifications > 0 ? (double)UnreadNotifications / TotalNotifications * 100 : 0;

    // Charts data
    public ChartDataViewModel[] TypeChartData => ByType?.Select(kvp => new ChartDataViewModel
    {
        Label = kvp.Key,
        Value = kvp.Value
    }).ToArray() ?? Array.Empty<ChartDataViewModel>();

    public ChartDataViewModel[] PriorityChartData => ByPriority?.Select(kvp => new ChartDataViewModel
    {
        Label = kvp.Key,
        Value = kvp.Value
    }).ToArray() ?? Array.Empty<ChartDataViewModel>();
}

/// <summary>
/// نموذج عدد يومي
/// </summary>
public class DailyCountViewModel
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// نموذج إعدادات البريد الإلكتروني
/// </summary>
public class EmailSettingsViewModel
{
    [Required]
    [Display(Name = "خادم SMTP")]
    public string SmtpServer { get; set; } = string.Empty;

    [Required]
    [Display(Name = "منفذ SMTP")]
    public int SmtpPort { get; set; } = 587;

    [Display(Name = "استخدام SSL")]
    public bool UseSsl { get; set; } = true;

    [Required]
    [Display(Name = "اسم المستخدم")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [Display(Name = "كلمة المرور")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Display(Name = "البريد المرسل")]
    public string FromEmail { get; set; } = string.Empty;

    [Required]
    [Display(Name = "اسم المرسل")]
    public string FromName { get; set; } = string.Empty;

    [Display(Name = "عدد المحاولات")]
    public int RetryAttempts { get; set; } = 3;

    [Display(Name = "فاصل المحاولات (بالثواني)")]
    public int RetryDelaySeconds { get; set; } = 30;
}

/// <summary>
/// نموذج قالب البريد الإلكتروني
/// </summary>
public class EmailTemplateViewModel
{
    [Required(ErrorMessage = "اسم القالب مطلوب")]
    [Display(Name = "اسم القالب")]
    [MaxLength(100, ErrorMessage = "اسم القالب يجب ألا يزيد عن 100 حرف")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "عنوان القالب مطلوب")]
    [Display(Name = "عنوان القالب")]
    [MaxLength(255, ErrorMessage = "عنوان القالب يجب ألا يزيد عن 255 حرف")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "محتوى القالب مطلوب")]
    [Display(Name = "محتوى القالب")]
    public string BodyHtml { get; set; } = string.Empty;

    [Display(Name = "محتوى نصي")]
    public string? BodyText { get; set; }

    [Display(Name = "متغيرات القالب")]
    public List<string> Variables { get; set; } = new();

    [Display(Name = "نشط")]
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// نموذج قاعدة الإشعارات
/// </summary>
public class NotificationRuleViewModel
{
    [Required(ErrorMessage = "اسم القاعدة مطلوب")]
    [Display(Name = "اسم القاعدة")]
    [MaxLength(100, ErrorMessage = "اسم القاعدة يجب ألا يزيد عن 100 حرف")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "الوصف")]
    [MaxLength(500, ErrorMessage = "الوصف يجب ألا يزيد عن 500 حرف")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "حدث القاعدة مطلوب")]
    [Display(Name = "حدث القاعدة")]
    public string TriggerEvent { get; set; } = string.Empty; // request_created, request_approved, etc.

    [Display(Name = "شروط القاعدة")]
    public Dictionary<string, object>? Conditions { get; set; }

    [Display(Name = "الإجراءات")]
    public List<NotificationActionViewModel> Actions { get; set; } = new();

    [Display(Name = "نشط")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "أولوية القاعدة")]
    public int Priority { get; set; } = 0;
}

/// <summary>
/// نموذج إجراء إشعار
/// </summary>
public class NotificationActionViewModel
{
    [Required(ErrorMessage = "نوع الإجراء مطلوب")]
    [Display(Name = "نوع الإجراء")]
    public string Type { get; set; } = string.Empty; // send_email, send_sms, send_push, etc.

    [Display(Name = "المستهدفون")]
    public List<string> Recipients { get; set; } = new();

    [Display(Name = "قالب الرسالة")]
    public string? TemplateId { get; set; }

    [Display(Name = "متغيرات الإجراء")]
    public Dictionary<string, object>? Variables { get; set; }

    [Display(Name = "تأخير (بالثواني)")]
    public int? DelaySeconds { get; set; }
}
