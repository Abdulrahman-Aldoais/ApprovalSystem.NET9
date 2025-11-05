using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ApprovalSystem.Models.Entities;

namespace ApprovalSystem.ViewModels;

/// <summary>
/// ViewModels لإدارة الطلبات والموافقات
/// </summary>

/// <summary>
/// نموذج إنشاء طلب جديد
/// </summary>
public class CreateRequestViewModel
{
    [Required(ErrorMessage = "عنوان الطلب مطلوب")]
    [Display(Name = "عنوان الطلب")]
    [MaxLength(255, ErrorMessage = "عنوان الطلب يجب ألا يزيد عن 255 حرف")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "وصف الطلب")]
    [MaxLength(2000, ErrorMessage = "وصف الطلب يجب ألا يزيد عن 2000 حرف")]
    public string? Description { get; set; }

    [Display(Name = "المبلغ")]
    [Range(0, double.MaxValue, ErrorMessage = "المبلغ يجب أن يكون رقماً موجباً")]
    public decimal? Amount { get; set; }

    [Required(ErrorMessage = "نوع الطلب مطلوب")]
    [Display(Name = "نوع الطلب")]
    public Guid RequestTypeId { get; set; }

    [Display(Name = "معرف مصفوفة الموافقات")]
    public Guid? ApprovalMatrixId { get; set; }

    [Display(Name = "الأولوية")]
    public string Priority { get; set; } = "medium"; // low, medium, high, urgent

    [Display(Name = "تاريخ الاستحقاق")]
    public DateTime? DueDate { get; set; }

    // Dynamic data fields (JSON)
    public Dictionary<string, object>? Data { get; set; }

    [Display(Name = "مستوى السرية")]
    public string? ConfidentialityLevel { get; set; }

    [Display(Name = "ملاحظات إضافية")]
    [MaxLength(1000, ErrorMessage = "الملاحظات يجب ألا تزيد عن 1000 حرف")]
    public string? AdditionalNotes { get; set; }
}

/// <summary>
/// نموذج تحديث طلب
/// </summary>
public class UpdateRequestViewModel
{
    [Required(ErrorMessage = "عنوان الطلب مطلوب")]
    [Display(Name = "عنوان الطلب")]
    [MaxLength(255, ErrorMessage = "عنوان الطلب يجب ألا يزيد عن 255 حرف")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "وصف الطلب")]
    [MaxLength(2000, ErrorMessage = "وصف الطلب يجب ألا يزيد عن 2000 حرف")]
    public string? Description { get; set; }

    [Display(Name = "المبلغ")]
    [Range(0, double.MaxValue, ErrorMessage = "المبلغ يجب أن يكون رقماً موجباً")]
    public decimal? Amount { get; set; }

    [Display(Name = "الأولوية")]
    public string Priority { get; set; } = "medium";

    [Display(Name = "تاريخ الاستحقاق")]
    public DateTime? DueDate { get; set; }

    // Dynamic data fields (JSON)
    public Dictionary<string, object>? Data { get; set; }

    [Display(Name = "ملاحظات إضافية")]
    [MaxLength(1000, ErrorMessage = "الملاحظات يجب ألا تزيد عن 1000 حرف")]
    public string? AdditionalNotes { get; set; }
}

/// <summary>
/// نموذج عرض الطلب في القوائم
/// </summary>
public class RequestViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? Amount { get; set; }
    public string Status { get; set; } = string.Empty; // pending, in_progress, approved, rejected, cancelled
    public string Priority { get; set; } = string.Empty; // low, medium, high, urgent
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsOverdue { get; set; }
    public int CurrentStage { get; set; }
    public UserViewModel? Requester { get; set; }
    public RequestTypeViewModel? RequestType { get; set; }

    // Computed properties
    public string StatusDisplayName => Status switch
    {
        "pending" => "قيد الانتظار",
        "in_progress" => "قيد المعالجة",
        "approved" => "معتمد",
        "rejected" => "مرفوض",
        "cancelled" => "ملغي",
        _ => Status
    };

    public string PriorityDisplayName => Priority switch
    {
        "low" => "منخفضة",
        "medium" => "متوسطة",
        "high" => "عالية",
        "urgent" => "عاجلة",
        _ => Priority
    };
}

/// <summary>
/// نموذج تفاصيل الطلب الكامل
/// </summary>
public class RequestDetailViewModel : RequestViewModel
{
    public Dictionary<string, object>? Data { get; set; }
    public List<ApprovalViewModel>? Approvals { get; set; }
    public List<AttachmentViewModel>? Attachments { get; set; }
    public List<RequestAuditViewModel>? AuditTrail { get; set; }

    // Workflow tracking
    public WorkflowTrackingViewModel? WorkflowTracking { get; set; }

    // Additional computed properties
    public bool CanBeApproved => Status == "pending" || Status == "in_progress";
    public bool CanBeRejected => Status == "pending" || Status == "in_progress";
    public bool CanBeEscalated => Status == "pending" && Approvals?.Any(a => a.Status == "pending" && a.IsOverdue) == true;
    public bool CanBeCancelled => (Status == "pending" || Status == "in_progress") && 
                                  Requester != null; // Additional user ID check should be done in controller/service

    public int TotalApprovalStages => Approvals?.Count ?? 0;
    public int CompletedStages => Approvals?.Count(a => a.Status == "approved") ?? 0;
    public int RejectedStages => Approvals?.Count(a => a.Status == "rejected") ?? 0;
}

/// <summary>
/// نموذج الموافقة على الطلب
/// </summary>
public class ApproveRequestViewModel
{
    [Display(Name = "تعليقات")]
    [MaxLength(1000, ErrorMessage = "التعليقات يجب ألا تزيد عن 1000 حرف")]
    public string? Comments { get; set; }

    [Display(Name = "شروط الموافقة")]
    [MaxLength(500, ErrorMessage = "الشروط يجب ألا تزيد عن 500 حرف")]
    public string? Conditions { get; set; }

    [Display(Name = "تاريخ استحقاق المتابعة")]
    public DateTime? FollowUpDate { get; set; }
}

/// <summary>
/// نموذج رفض الطلب
/// </summary>
public class RejectRequestViewModel
{
    [Required(ErrorMessage = "سبب الرفض مطلوب")]
    [Display(Name = "سبب الرفض")]
    [MaxLength(255, ErrorMessage = "سبب الرفض يجب ألا يزيد عن 255 حرف")]
    public string Reason { get; set; } = string.Empty;

    [Display(Name = "تعليقات إضافية")]
    [MaxLength(1000, ErrorMessage = "التعليقات يجب ألا تزيد عن 1000 حرف")]
    public string? Comments { get; set; }

    [Display(Name = "اقتراحات للتحسين")]
    [MaxLength(500, ErrorMessage = "الاقتراحات يجب ألا تزيد عن 500 حرف")]
    public string? Suggestions { get; set; }

    [Display(Name = "هل يمكن إعادة التقديم؟")]
    public bool CanResubmit { get; set; } = true;
}

/// <summary>
/// نموذج تصعيد الطلب
/// </summary>
public class EscalateRequestViewModel
{
    [Required(ErrorMessage = "سبب التصعيد مطلوب")]
    [Display(Name = "سبب التصعيد")]
    [MaxLength(255, ErrorMessage = "سبب التصعيد يجب ألا يزيد عن 255 حرف")]
    public string Reason { get; set; } = string.Empty;

    [Display(Name = "تعليقات")]
    [MaxLength(1000, ErrorMessage = "التعليقات يجب ألا تزيد عن 1000 حرف")]
    public string? Comments { get; set; }

    [Required(ErrorMessage = "مستوى التصعيد مطلوب")]
    [Display(Name = "مستوى التصعيد")]
    public string EscalationLevel { get; set; } = "manager"; // manager, director, executive

    [Display(Name = "معرّف الموظف المراد التصعيد له")]
    public string? EscalateToUserId { get; set; }

    [Display(Name = "مهلة التنفيذ")]
    public int DeadlineHours { get; set; } = 24;
}

/// <summary>
/// نموذج عرض الموافقة
/// </summary>
public class ApprovalViewModel
{
    public Guid Id { get; set; }
    public int Stage { get; set; }
    public string Status { get; set; } = string.Empty; // pending, approved, rejected, escalated
    public string? Comments { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public UserViewModel? Approver { get; set; }
    public bool IsOverdue { get; set; }
    public TimeSpan? ProcessingTime { get; set; }

    // Additional fields
    public string? RejectionReason { get; set; }
    public bool CanEscalate => Status == "pending" && IsOverdue;

    // Computed properties
    public string StatusDisplayName => Status switch
    {
        "pending" => "قيد الانتظار",
        "approved" => "معتمد",
        "rejected" => "مرفوض",
        "escalated" => "محال",
        _ => Status
    };
}

/// <summary>
/// نموذج الموافقات المعلقة
/// </summary>
public class PendingApprovalViewModel
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public string RequestTitle { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public int Stage { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
    public bool IsOverdue { get; set; }
    public RequestTypeViewModel? RequestType { get; set; }

    // Computed properties
    public int DaysSinceCreated => (int)(DateTime.UtcNow - CreatedAt).TotalDays;
    public string OverdueDays => IsOverdue ? $"{DaysSinceCreated - 3} يوم" : "لا يوجد";
    public string UrgentLevel => DaysSinceCreated switch
    {
        <= 1 => "عادية",
        <= 3 => "متوسطة",
        <= 7 => "عالية",
        _ => "عاجلة جداً"
    };
}

/// <summary>
/// نموذج صفحتين الدعم (Paged Response)
/// </summary>
public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
    public string? NextPageUrl => HasNextPage ? $"?pageNumber={PageNumber + 1}&pageSize={PageSize}" : null;
    public string? PreviousPageUrl => HasPreviousPage ? $"?pageNumber={PageNumber - 1}&pageSize={PageSize}" : null;
}

/// <summary>
/// نموذج إحصائيات الطلبات
/// </summary>
public class RequestStatsViewModel
{
    public int TotalRequests { get; set; }
    public int PendingRequests { get; set; }
    public int InProgressRequests { get; set; }
    public int ApprovedRequests { get; set; }
    public int RejectedRequests { get; set; }
    public int CancelledRequests { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AverageProcessingTime { get; set; } // in hours

    // Percentage calculations
    public double ApprovalRate => TotalRequests > 0 ? (double)ApprovedRequests / TotalRequests * 100 : 0;
    public double RejectionRate => TotalRequests > 0 ? (double)RejectedRequests / TotalRequests * 100 : 0;
    public double PendingRate => TotalRequests > 0 ? (double)PendingRequests / TotalRequests * 100 : 0;
}

/// <summary>
/// نموذج تتبع مسارات العمل
/// </summary>
public class WorkflowTrackingViewModel
{
    public Guid Id { get; set; }
    public string? CurrentStage { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
    public bool SlaBreachAlert { get; set; }
    public int EscalationCount { get; set; }
    public DateTime? Deadline { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public UserViewModel? AssignedToUser { get; set; }

    // Computed properties
    public bool IsOverdue => Deadline.HasValue && DateTime.UtcNow > Deadline.Value && Status != "completed";
    public int OverdueDays => IsOverdue ? (int)(DateTime.UtcNow - Deadline.Value).TotalDays : 0;
    public TimeSpan? ProcessingTime => CompletedAt.HasValue ? CompletedAt.Value.Subtract(DateTime.UtcNow.AddDays(-1)) : null;
    public string SlaStatus => SlaBreachAlert ? "مخالف" : IsOverdue ? "متأخر" : "في الوقت المناسب";
}

/// <summary>
/// نموذج سجل مراجعة الطلبات
/// </summary>
public class RequestAuditViewModel
{
    public Guid Id { get; set; }
    public string ActionType { get; set; } = string.Empty; // create, update, approve, reject, escalate, etc.
    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }
    public string? ActorName { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }

    // Computed properties
    public string ActionDisplayName => ActionType switch
    {
        "created" => "تم الإنشاء",
        "updated" => "تم التحديث",
        "approved" => "تمت الموافقة",
        "rejected" => "تم الرفض",
        "escalated" => "تم التصعيد",
        "cancelled" => "تم الإلغاء",
        _ => ActionType
    };

    public string StatusChange => (FromStatus, ToStatus) switch
    {
        (null, null) => "",
        (var from, var to) when !string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to) => $"{from} -> {to}",
        (var from, _) when !string.IsNullOrEmpty(from) => $"{from} -> ",
        (_, var to) when !string.IsNullOrEmpty(to) => "-> {to}",
        _ => ""
    };
}

/// <summary>
/// نموذج نوع الطلب
/// </summary>
public class RequestTypeViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public ModuleViewModel? Module { get; set; }

    // Configuration (JSON)
    public Dictionary<string, object>? AmountThresholds { get; set; }
    public Dictionary<string, object>? RequiredFields { get; set; }
}

/// <summary>
/// نموذج الوحدة/القسم
/// </summary>
public class ModuleViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public string? Manager { get; set; }
}

/// <summary>
/// نموذج مصفوفة الموافقات
/// </summary>
public class ApprovalMatrixViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // Configuration (JSON)
    public Dictionary<string, object>? Rules { get; set; }
    public Dictionary<string, object>? Conditions { get; set; }
}

/// <summary>
/// نموذج المرفق
/// </summary>
public class AttachmentViewModel
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string FileSizeFormatted { get; set; } = string.Empty;
    public UserViewModel? UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    // Computed properties
    public bool IsImage => FileType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    public bool IsPdf => FileType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
    public bool IsDocument => FileType.Contains("document") || 
                             FileType.Contains("word") ||
                             FileType.Contains("excel") ||
                             FileType.Contains("powerpoint");
    public bool IsAllowedType => IsImage || IsPdf || IsDocument || FileType.Contains("text/");
}
