using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApprovalSystem.Models.Entities;
using ApprovalSystem.ViewModels;

namespace ApprovalSystem.Core.Interfaces;

/// <summary>
/// واجهة خدمة إدارة الموافقات
/// </summary>
public interface IApprovalService
{
    /// <summary>
    /// الحصول على الموافقات المعلقة للمستخدم
    /// </summary>
    Task<(List<Approval> approvals, int totalCount)> GetPendingApprovalsAsync(
        string approverId, Guid tenantId, int pageNumber, int pageSize);

    /// <summary>
    /// الموافقة على طلب
    /// </summary>
    Task<bool> ApproveRequestAsync(Guid requestId, string? comments, string approverId, Guid tenantId);

    /// <summary>
    /// رفض طلب
    /// </summary>
    Task<bool> RejectRequestAsync(Guid requestId, string reason, string? comments, string approverId, Guid tenantId);

    /// <summary>
    /// تصعيد طلب
    /// </summary>
    Task<bool> EscalateRequestAsync(Guid requestId, string reason, string? comments, string escalatedById, Guid tenantId);

    /// <summary>
    /// إنشاء مرحلة موافقة جديدة
    /// </summary>
    Task<Approval?> CreateApprovalAsync(Guid requestId, string approverId, int stage, Guid tenantId);

    /// <summary>
    /// الحصول على الموافقات لطلب محدد
    /// </summary>
    Task<List<Approval>> GetRequestApprovalsAsync(Guid requestId);

    /// <summary>
    /// الحصول على موافقة محددة
    /// </summary>
    Task<Approval?> GetApprovalByIdAsync(Guid approvalId);

    /// <summary>
    /// تحديث حالة الموافقة
    /// </summary>
    Task<bool> UpdateApprovalStatusAsync(Guid approvalId, string status, string? comments, string? rejectionReason = null);

    /// <summary>
    /// الحصول على إحصائيات الموافقات
    /// </summary>
    Task<ApprovalStatsViewModel> GetApprovalStatsAsync(Guid tenantId, string? userId = null);

    /// <summary>
    /// الحصول على مسار الموافقات للطلب
    /// </summary>
    Task<ApprovalMatrix?> GetApprovalMatrixForRequestAsync(Guid requestId);

    /// <summary>
    /// إنشاء تصعيد الموافقة
    /// </summary>
    Task<ApprovalEscalation?> CreateEscalationAsync(
        Guid approvalId, string reason, string escalatedTo, string escalatedById, Guid tenantId);

    /// <summary>
    /// الحصول على التصعيدات المعلقة
    /// </summary>
    Task<List<ApprovalEscalation>> GetPendingEscalationsAsync(Guid tenantId);

    /// <summary>
    /// حل التصعيد
    /// </summary>
    Task<bool> ResolveEscalationAsync(Guid escalationId, string resolvedById);

    /// <summary>
    /// الحصول على تاريخ الموافقات للمستخدم
    /// </summary>
    Task<List<Approval>> GetUserApprovalHistoryAsync(string userId, Guid tenantId, int count = 50);

    /// <summary>
    /// حساب وقت المعالجة المتوقع
    /// </summary>
    Task<TimeSpan> CalculateExpectedProcessingTimeAsync(Guid requestId);

    /// <summary>
    /// إرسال تذكير بالموافقة المعلقة
    /// </summary>
    Task<bool> SendApprovalReminderAsync(Guid approvalId);

    /// <summary>
    /// الحصول على إحصائيات التصعيد
    /// </summary>
    Task<EscalationStatsViewModel> GetEscalationStatsAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null);
}

/// <summary>
/// نموذج إحصائيات الموافقات
/// </summary>
public class ApprovalStatsViewModel
{
    public int TotalApprovals { get; set; }
    public int PendingApprovals { get; set; }
    public int ApprovedApprovals { get; set; }
    public int RejectedApprovals { get; set; }
    public int EscalatedApprovals { get; set; }
    public double AverageProcessingTime { get; set; } // in hours
    public double ApprovalRate { get; set; } // percentage

    // Computed properties
    public double PendingPercentage => TotalApprovals > 0 ? (double)PendingApprovals / TotalApprovals * 100 : 0;
    public double ApprovedPercentage => TotalApprovals > 0 ? (double)ApprovedApprovals / TotalApprovals * 100 : 0;
    public double RejectedPercentage => TotalApprovals > 0 ? (double)RejectedApprovals / TotalApprovals * 100 : 0;
}

/// <summary>
/// نموذج إحصائيات التصعيد
/// </summary>
public class EscalationStatsViewModel
{
    public int TotalEscalations { get; set; }
    public int PendingEscalations { get; set; }
    public int ResolvedEscalations { get; set; }
    public double AverageResolutionTime { get; set; } // in hours
    public int EscalationsByReason { get; set; } // where reason contains common patterns

    // Computed properties
    public double ResolutionRate => TotalEscalations > 0 ? (double)ResolvedEscalations / TotalEscalations * 100 : 0;
}
