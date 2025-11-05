using System.ComponentModel.DataAnnotations;

namespace ApprovalSystem.Models.DTOs;

/// <summary>
/// DTO لطلب إنشاء موافقة جديدة
/// </summary>
public class ApprovalRequestDto
{
    [Required]
    public Guid RequestId { get; set; }

    [Required]
    public string ApproverId { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Comments { get; set; }

    [StringLength(50)]
    public string? RequestType { get; set; }

    public object? RequestData { get; set; }

    public int ApprovalLevel { get; set; } = 1;

    public DateTime? DueDate { get; set; }

    public string Priority { get; set; } = "normal";
}

/// <summary>
/// DTO لقرار الموافقة
/// </summary>
public class ApprovalDecisionDto
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public string ApproverId { get; set; } = string.Empty;
    public string ApproverName { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public DateTime DecisionTime { get; set; }
    public int ApprovalLevel { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public bool IsEscalated { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO لإنشاء طلب جديد
/// </summary>
public class CreateRequestDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [StringLength(50)]
    public string RequestType { get; set; } = string.Empty;

    public object? RequestData { get; set; }

    [StringLength(50)]
    public string? Priority { get; set; } = "normal";

    public List<string> Attachments { get; set; } = new();

    public List<string> Tags { get; set; } = new();

    public string? RequesterNotes { get; set; }

    public DateTime? DueDate { get; set; }
}

/// <summary>
/// DTO لعرض الطلب
/// </summary>
public class RequestDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public object? RequestData { get; set; }
    public string RequesterId { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public List<ApprovalDecisionDto> Approvals { get; set; } = new();
    public List<string> Attachments { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public string? CurrentStage { get; set; }
    public int CurrentApprovalLevel { get; set; }
    public bool IsEscalated { get; set; }
    public string? WorkflowInstanceId { get; set; }
}

/// <summary>
/// نوع حالة الطلب
/// </summary>
public enum RequestStatus
{
    Draft = 0,
    Submitted = 1,
    UnderReview = 2,
    Approved = 3,
    Rejected = 4,
    Cancelled = 5,
    Completed = 6,
    OnHold = 7,
    Expired = 8
}

/// <summary>
/// DTO لإشعار المستخدم
/// </summary>
public class NotificationDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? DataJson { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
}

/// <summary>
/// DTO لتخصيص الإشعارات
/// ملاحظة: تم تحريه هذا التعريف إلى NotificationSettingsDto في WorkflowConfigurationDTOs.cs لتجنب التكرار
/// استخدام ApprovalSystem.Models.DTOs.NotificationSettingsDto بدلاً من هذا التعريف
/// </summary>
// public class NotificationSettingsDto
// {
//     public bool EmailEnabled { get; set; } = true;
//     public bool SmsEnabled { get; set; } = false;
//     public bool InAppEnabled { get; set; } = true;
//     public bool PushEnabled { get; set; } = false;
//     public List<string> EventTypes { get; set; } = new();
//     public string? EmailTemplate { get; set; }
//     public string? SmsTemplate { get; set; }
//     public string? InAppTemplate { get; set; }
//     public string? PushTemplate { get; set; }
//     public Dictionary<string, object> CustomSettings { get; set; } = new();
// }
