using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApprovalSystem.Models.Entities;
using ApprovalSystem.ViewModels;

namespace ApprovalSystem.Core.Interfaces;

/// <summary>
/// واجهة خدمة لوحة التحكم والإحصائيات
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// الحصول على إحصائيات لوحة التحكم الرئيسية
    /// </summary>
    Task<DashboardStats> GetDashboardStatsAsync(Guid tenantId, string? userId = null);

    /// <summary>
    /// الحصول على إحصائيات مفصلة
    /// </summary>
    Task<DashboardAnalytics> GetAnalyticsAsync(Guid tenantId, DateTime startDate, DateTime endDate, string groupBy = "day");

    /// <summary>
    /// الحصول على الطلبات العاجلة
    /// </summary>
    Task<List<Request>> GetUrgentRequestsAsync(Guid tenantId, int count = 10);

    /// <summary>
    /// الحصول على النشاطات الأخيرة
    /// </summary>
    Task<List<RecentActivity>> GetRecentActivityAsync(Guid tenantId, int count = 20);

    /// <summary>
    /// الحصول على ملخص الأداء الشهري
    /// </summary>
    Task<PerformanceSummary> GetPerformanceSummaryAsync(Guid tenantId, int month, int year);

    /// <summary>
    /// الحصول على التحديثات الفورية
    /// </summary>
    Task<LiveUpdates> GetLiveUpdatesAsync(Guid tenantId, string userId);

    /// <summary>
    /// الحصول على إحصائيات المستخدمين
    /// </summary>
    Task<List<UserStats>> GetUserStatsAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// الحصول على إحصائيات الوحدات
    /// </summary>
    Task<List<ModuleStats>> GetModuleStatsAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// الحصول على اتجاهات الطلبات
    /// </summary>
    Task<List<ChartData>> GetRequestTrendsAsync(Guid tenantId, DateTime startDate, DateTime endDate, string groupBy = "day");

    /// <summary>
    /// الحصول على اتجاهات الموافقات
    /// </summary>
    Task<List<ChartData>> GetApprovalTrendsAsync(Guid tenantId, DateTime startDate, DateTime endDate, string groupBy = "day");

    /// <summary>
    /// الحصول على توزيع الحالات
    /// </summary>
    Task<Dictionary<string, int>> GetStatusDistributionAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// الحصول على توزيع الأولويات
    /// </summary>
    Task<Dictionary<string, int>> GetPriorityDistributionAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// الحصول على أداء المستخدمين
    /// </summary>
    Task<List<UserPerformance>> GetUserPerformanceAsync(Guid tenantId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// إنشاء تقرير مخصص
    /// </summary>
    Task<byte[]> GenerateCustomReportAsync(Guid tenantId, ReportConfig config);

    /// <summary>
    /// الحصول على إعدادات لوحة التحكم
    /// </summary>
    Task<DashboardSettings> GetDashboardSettingsAsync(string userId, Guid tenantId);

    /// <summary>
    /// تحديث إعدادات لوحة التحكم
    /// </summary>
    Task<bool> UpdateDashboardSettingsAsync(string userId, Guid tenantId, DashboardSettings settings);
}

/// <summary>
/// نموذج إحصائيات لوحة التحكم
/// </summary>
public class DashboardStats
{
    public OverviewStats Overview { get; set; } = new();
    public Metrics Metrics { get; set; } = new();
    public Dictionary<string, int> ByModule { get; set; } = new();
    public StatusDistribution StatusDistribution { get; set; } = new();
    public Dictionary<string, int> ByPriority { get; set; } = new();
    public List<RecentActivity>? RecentActivity { get; set; }
}

/// <summary>
/// نموذج النظرة العامة
/// </summary>
public class OverviewStats
{
    public int TotalRequests { get; set; }
    public int PendingRequests { get; set; }
    public int InProgressRequests { get; set; }
    public int ApprovedRequests { get; set; }
    public int RejectedRequests { get; set; }
    public int CancelledRequests { get; set; }
    public int MyPendingApprovals { get; set; }
    public int MyCreatedRequests { get; set; }
    public int UrgentRequests { get; set; }
    public int OverdueRequests { get; set; }
    public decimal TotalAmount { get; set; }
}

/// <summary>
/// نموذج المقاييس
/// </summary>
public class Metrics
{
    public double ApprovalRate { get; set; }
    public double AverageProcessingTimeHours { get; set; }
    public int RecentRequestsCount { get; set; }
    public double CompletionRate { get; set; }
    public double EscalationRate { get; set; }
    public double AverageApprovalTime { get; set; }
    public double EfficiencyScore { get; set; }
}

/// <summary>
/// نموذج توزيع الحالات
/// </summary>
public class StatusDistribution
{
    public int Pending { get; set; }
    public int InProgress { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int Cancelled { get; set; }
}

/// <summary>
/// نموذج النشاطات الأخيرة
/// </summary>
public class RecentActivity
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string RequestTitle { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public Guid? RequestId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Details { get; set; }
}

/// <summary>
/// نموذج بيانات التحليلات
/// </summary>
public class DashboardAnalytics
{
    public List<ChartData>? RequestTrends { get; set; }
    public List<ChartData>? ApprovalTrends { get; set; }
    public List<ChartData>? StatusDistribution { get; set; }
    public List<ChartData>? PriorityDistribution { get; set; }
    public List<ChartData>? ModuleDistribution { get; set; }
    public List<UserPerformance>? UserPerformance { get; set; }
}

/// <summary>
/// نموذج بيانات المخطط
/// </summary>
public class ChartData
{
    public DateTime? Date { get; set; }
    public string? Label { get; set; }
    public int Value { get; set; }
    public decimal? DecimalValue { get; set; }
}

/// <summary>
/// نموذج أداء المستخدم
/// </summary>
public class UserPerformance
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int TotalApprovals { get; set; }
    public double AverageApprovalTime { get; set; }
    public double OnTimeRate { get; set; }
}

/// <summary>
/// نموذج ملخص الأداء
/// </summary>
public class PerformanceSummary
{
    public int TotalRequests { get; set; }
    public int CompletedRequests { get; set; }
    public double AverageProcessingTime { get; set; }
    public double ApprovalRate { get; set; }
    public List<UserStats>? UserStats { get; set; }
    public List<ModuleStats>? ModuleStats { get; set; }
}

/// <summary>
/// نموذج إحصائيات المستخدم
/// </summary>
public class UserStats
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int CompletedRequests { get; set; }
    public double AverageProcessingTime { get; set; }
}

/// <summary>
/// نموذج إحصائيات الوحدة
/// </summary>
public class ModuleStats
{
    public Guid ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public double AverageProcessingTime { get; set; }
}

/// <summary>
/// نموذج التحديثات الفورية
/// </summary>
public class LiveUpdates
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public List<Request>? NewRequests { get; set; }
    public List<Request>? UpdatedRequests { get; set; }
    public List<Notification>? NewNotifications { get; set; }
    public StatsUpdate? UpdatedStats { get; set; }
}

/// <summary>
/// نموذج تحديث الإحصائيات
/// </summary>
public class StatsUpdate
{
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int UrgentCount { get; set; }
    public int OverdueCount { get; set; }
}

/// <summary>
/// نموذج إعدادات لوحة التحكم
/// </summary>
public class DashboardSettings
{
    public bool AutoRefresh { get; set; } = true;
    public int RefreshIntervalSeconds { get; set; } = 30;
    public int RequestsListCount { get; set; } = 20;
    public int ActivityLogCount { get; set; } = 20;
    public bool ShowUrgentRequests { get; set; } = true;
    public bool ShowStats { get; set; } = true;
    public bool ShowCharts { get; set; } = true;
    public string Theme { get; set; } = "auto";
    public string Language { get; set; } = "ar";
    public string TimeZone { get; set; } = "Arabia Standard Time";
    public List<string> EnabledWidgets { get; set; } = new() { "overview", "recent-activity", "urgent-requests", "stats" };
    public Dictionary<string, int> WidgetOrder { get; set; } = new();
}

/// <summary>
/// نموذج تكوين التقرير
/// </summary>
public class ReportConfig
{
    public string ReportType { get; set; } = "summary"; // summary, detailed, performance
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<string> IncludeData { get; set; } = new() { "requests", "approvals", "stats" };
    public List<string> ExcludeData { get; set; } = new();
    public string Format { get; set; } = "pdf"; // pdf, excel, csv
    public Dictionary<string, object> Filters { get; set; } = new();
}
