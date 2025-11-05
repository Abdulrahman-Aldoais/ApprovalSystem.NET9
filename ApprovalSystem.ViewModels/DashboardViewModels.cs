using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ApprovalSystem.ViewModels;

/// <summary>
/// ViewModels للوحة التحكم والإحصائيات
/// </summary>

/// <summary>
/// نموذج إحصائيات لوحة التحكم الرئيسية
/// </summary>
public class DashboardStatsViewModel
{
    public OverviewStatsViewModel Overview { get; set; } = new();
    public MetricsViewModel Metrics { get; set; } = new();
    public Dictionary<string, int> ByModule { get; set; } = new();
    public StatusDistributionViewModel StatusDistribution { get; set; } = new();
    public Dictionary<string, int> ByPriority { get; set; } = new();
    public List<RecentActivityViewModel>? RecentActivity { get; set; }
}

/// <summary>
/// نموذج نظرة عامة على الإحصائيات
/// </summary>
public class OverviewStatsViewModel
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
    public decimal AverageAmount { get; set; }

    // Computed properties
    public double PendingPercentage => TotalRequests > 0 ? (double)PendingRequests / TotalRequests * 100 : 0;
    public double ApprovedPercentage => TotalRequests > 0 ? (double)ApprovedRequests / TotalRequests * 100 : 0;
    public double RejectedPercentage => TotalRequests > 0 ? (double)RejectedRequests / TotalRequests * 100 : 0;

    public string StatusSummary => $"{PendingRequests} معلق • {ApprovedRequests} معتمد • {RejectedRequests} مرفوض";
}

/// <summary>
/// نموذج المقاييس الرئيسية
/// </summary>
public class MetricsViewModel
{
    public double ApprovalRate { get; set; }
    public double AverageProcessingTimeHours { get; set; }
    public int RecentRequestsCount { get; set; }
    public double CompletionRate { get; set; }
    public double EscalationRate { get; set; }
    public double AverageApprovalTime { get; set; }
    public double EfficiencyScore { get; set; } // Composite score 0-100

    // Computed properties
    public string ApprovalRateFormatted => $"{ApprovalRate:F1}%";
    public string ProcessingTimeFormatted => $"{AverageProcessingTimeHours:F1} ساعة";
    public string ApprovalTimeFormatted => $"{AverageApprovalTime:F1} ساعة";
    public string EfficiencyScoreFormatted => $"{EfficiencyScore:F1}%";

    public string EfficiencyLevel => EfficiencyScore switch
    {
        >= 90 => "ممتاز",
        >= 80 => "جيد جداً",
        >= 70 => "جيد",
        >= 60 => "مقبول",
        _ => "يحتاج تحسين"
    };
}

/// <summary>
/// نموذج توزيع الحالات
/// </summary>
public class StatusDistributionViewModel
{
    public int Pending { get; set; }
    public int InProgress { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int Cancelled { get; set; }

    // Computed properties
    public int Total => Pending + InProgress + Approved + Rejected + Cancelled;

    public Dictionary<string, int> ToDictionary()
    {
        return new Dictionary<string, int>
        {
            { "pending", Pending },
            { "in_progress", InProgress },
            { "approved", Approved },
            { "rejected", Rejected },
            { "cancelled", Cancelled }
        };
    }

    public string[] StatusLabels => new[] { "معلق", "قيد المعالجة", "معتمد", "مرفوض", "ملغي" };
    public int[] StatusValues => new[] { Pending, InProgress, Approved, Rejected, Cancelled };
}

/// <summary>
/// نموذج النشاطات الأخيرة
/// </summary>
public class RecentActivityViewModel
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string RequestTitle { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public Guid? RequestId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Details { get; set; }

    // Computed properties
    public string TimeAgo => GetTimeAgoString(CreatedAt);
    public string ActionDisplay => Action switch
    {
        "created" => "تم إنشاء",
        "updated" => "تم تحديث",
        "approved" => "تمت الموافقة",
        "rejected" => "تم الرفض",
        "escalated" => "تم التصعيد",
        "cancelled" => "تم الإلغاء",
        "commented" => "تمت إضافة تعليق",
        _ => Action
    };

    public string ActionIcon => Action switch
    {
        "created" => "icon-plus",
        "updated" => "icon-edit",
        "approved" => "icon-check",
        "rejected" => "icon-close",
        "escalated" => "icon-arrow-up",
        "cancelled" => "icon-cancel",
        "commented" => "icon-comment",
        _ => "icon-activity"
    };

    private string GetTimeAgoString(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        return timeSpan.TotalDays switch
        {
            >= 365 => $"منذ {Math.Floor(timeSpan.TotalDays / 365):N0} سنة",
            >= 30 => $"منذ {Math.Floor(timeSpan.TotalDays / 30):N0} شهر",
            >= 7 => $"منذ {Math.Floor(timeSpan.TotalDays / 7):N0} أسبوع",
            >= 1 => $"منذ {Math.Floor(timeSpan.TotalDays):N0} يوم",
            _ when timeSpan.TotalHours >= 1 => $"منذ {Math.Floor(timeSpan.TotalHours):N0} ساعة",
            _ when timeSpan.TotalMinutes >= 1 => $"منذ {Math.Floor(timeSpan.TotalMinutes):N0} دقيقة",
            _ => "منذ لحظات"
        };
    }
}

/// <summary>
/// نموذج بيانات التحليلات
/// </summary>
public class AnalyticsViewModel
{
    public TimeRangeViewModel TimeRange { get; set; } = new();
    public List<ChartDataViewModel>? RequestTrends { get; set; }
    public List<ChartDataViewModel>? ApprovalTrends { get; set; }
    public List<ChartDataViewModel>? StatusDistribution { get; set; }
    public List<ChartDataViewModel>? PriorityDistribution { get; set; }
    public List<ChartDataViewModel>? ModuleDistribution { get; set; }
    public List<UserPerformanceViewModel>? UserPerformance { get; set; }

    // Computed properties
    public bool HasRequestTrends => RequestTrends?.Any() == true;
    public bool HasApprovalTrends => ApprovalTrends?.Any() == true;
    public bool HasUserPerformance => UserPerformance?.Any() == true;
}

/// <summary>
/// نموذج النطاق الزمني
/// </summary>
public class TimeRangeViewModel
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string GroupBy { get; set; } = "day"; // day, week, month

    // Computed properties
    public int TotalDays => (EndDate - StartDate).Days + 1;
    public string DateRangeDisplay => $"{StartDate:yyyy/MM/dd} - {EndDate:yyyy/MM/dd}";
    public string GroupByDisplay => GroupBy switch
    {
        "day" => "يومياً",
        "week" => "أسبوعياً",
        "month" => "شهرياً",
        _ => "يومياً"
    };
}

/// <summary>
/// نموذج بيانات المخطط البياني
/// </summary>
public class ChartDataViewModel
{
    public DateTime? Date { get; set; }
    public string? Label { get; set; }
    public int Value { get; set; }
    public decimal? DecimalValue { get; set; }

    // Computed properties
    public string DateLabel => Date?.ToString("yyyy/MM/dd") ?? Label ?? "";
    public double NumericValue => DecimalValue ?? Value;
}

/// <summary>
/// نموذج أداء المستخدم
/// </summary>
public class UserPerformanceViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int TotalApprovals { get; set; }
    public double AverageApprovalTime { get; set; } // in hours
    public double OnTimeRate { get; set; } // percentage

    // Computed properties
    public string AverageApprovalTimeFormatted => $"{AverageApprovalTime:F1} ساعة";
    public string OnTimeRateFormatted => $"{OnTimeRate:F1}%";
    public string PerformanceLevel => OnTimeRate switch
    {
        >= 95 => "ممتاز",
        >= 85 => "جيد جداً",
        >= 75 => "جيد",
        >= 65 => "مقبول",
        _ => "يحتاج تحسين"
    };
}

/// <summary>
/// نموذج ملخص الأداء الشهري
/// </summary>
public class PerformanceSummaryViewModel
{
    public int Month { get; set; }
    public int Year { get; set; }
    public int TotalRequests { get; set; }
    public int CompletedRequests { get; set; }
    public double AverageProcessingTime { get; set; } // in hours
    public double ApprovalRate { get; set; } // percentage
    public List<UserStatsViewModel>? UserStats { get; set; }
    public List<ModuleStatsViewModel>? ModuleStats { get; set; }

    // Computed properties
    public string MonthYearDisplay => $"{new DateTime(Year, Month, 1):MMMM yyyy}";
    public double CompletionRate => TotalRequests > 0 ? (double)CompletedRequests / TotalRequests * 100 : 0;
    public string ApprovalRateFormatted => $"{ApprovalRate:F1}%";
    public string ProcessingTimeFormatted => $"{AverageProcessingTime:F1} ساعة";
}

/// <summary>
/// نموذج إحصائيات المستخدم
/// </summary>
public class UserStatsViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int CompletedRequests { get; set; }
    public double AverageProcessingTime { get; set; } // in hours

    // Computed properties
    public string ProcessingTimeFormatted => $"{AverageProcessingTime:F1} ساعة";
    public double CompletionRate => TotalRequests > 0 ? (double)CompletedRequests / TotalRequests * 100 : 0;
}

/// <summary>
/// نموذج إحصائيات الوحدة
/// </summary>
public class ModuleStatsViewModel
{
    public Guid ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public double AverageProcessingTime { get; set; } // in hours

    // Computed properties
    public string ProcessingTimeFormatted => $"{AverageProcessingTime:F1} ساعة";
}

/// <summary>
/// نموذج التحديثات الفورية
/// </summary>
public class LiveUpdatesViewModel
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public List<RequestViewModel>? NewRequests { get; set; }
    public List<RequestViewModel>? UpdatedRequests { get; set; }
    public List<NotificationViewModel>? NewNotifications { get; set; }
    public StatsUpdateViewModel? UpdatedStats { get; set; }

    // Computed properties
    public int NewItemsCount => (NewRequests?.Count ?? 0) + (NewNotifications?.Count ?? 0);
    public string UpdateTimeDisplay => Timestamp.ToString("HH:mm:ss");

    public bool HasUpdates => NewItemsCount > 0 || UpdatedRequests?.Any() == true;
}

/// <summary>
/// نموذج تحديث الإحصائيات
/// </summary>
public class StatsUpdateViewModel
{
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int UrgentCount { get; set; }
    public int OverdueCount { get; set; }

    // Computed properties
    public int TotalCount => PendingCount + ApprovedCount + RejectedCount;
    public Dictionary<string, int> StatusCounts => new()
    {
        { "pending", PendingCount },
        { "approved", ApprovedCount },
        { "rejected", RejectedCount }
    };
}

/// <summary>
/// نموذج إعدادات لوحة التحكم
/// </summary>
public class DashboardSettingsViewModel
{
    [Display(Name = "تحديث تلقائي")]
    public bool AutoRefresh { get; set; } = true;

    [Display(Name = "فترة التحديث (بالثواني)")]
    [Range(5, 300, ErrorMessage = "فترة التحديث يجب أن تكون بين 5 و 300 ثانية")]
    public int RefreshIntervalSeconds { get; set; } = 30;

    [Display(Name = "عدد الطلبات في القائمة")]
    [Range(5, 100, ErrorMessage = "عدد الطلبات يجب أن يكون بين 5 و 100")]
    public int RequestsListCount { get; set; } = 20;

    [Display(Name = "عدد النشاطات في السجل")]
    [Range(5, 50, ErrorMessage = "عدد النشاطات يجب أن يكون بين 5 و 50")]
    public int ActivityLogCount { get; set; } = 20;

    [Display(Name = "إظهار الطلبات العاجلة")]
    public bool ShowUrgentRequests { get; set; } = true;

    [Display(Name = "إظهار الإحصائيات")]
    public bool ShowStats { get; set; } = true;

    [Display(Name = "إظهار المخططات البيانية")]
    public bool ShowCharts { get; set; } = true;

    [Display(Name = "النمط المفضل")]
    public string Theme { get; set; } = "auto"; // auto, light, dark

    [Display(Name = "اللغة")]
    public string Language { get; set; } = "ar";

    [Display(Name = "المنطقة الزمنية")]
    public string TimeZone { get; set; } = "Arabia Standard Time";

    // Widgets settings
    [Display(Name = "اختيارات الواجهة")]
    public List<string> EnabledWidgets { get; set; } = new() { "overview", "recent-activity", "urgent-requests", "stats" };

    [Display(Name = "ترتيب الواجهة")]
    public Dictionary<string, int> WidgetOrder { get; set; } = new()
    {
        { "overview", 1 },
        { "recent-activity", 2 },
        { "urgent-requests", 3 },
        { "stats", 4 }
    };
}

/// <summary>
/// نموذج تقرير لوحة التحكم
/// </summary>
public class DashboardReportViewModel
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string Period { get; set; } = string.Empty; // daily, weekly, monthly
    public string ReportType { get; set; } = "summary"; // summary, detailed, performance
    public OverviewStatsViewModel Overview { get; set; } = new();
    public List<RecentActivityViewModel> Activities { get; set; } = new();
    public List<string> Insights { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();

    // Computed properties
    public string ReportTitle => $"{Period} - {ReportType}";
    public string GeneratedAtDisplay => GeneratedAt.ToString("yyyy/MM/dd HH:mm");
    public bool HasInsights => Insights.Any();
    public bool HasRecommendations => Recommendations.Any();
}

/// <summary>
/// نموذج إحصائيات الوحدة النمطية
/// </summary>
public class WidgetStatsViewModel
{
    public string WidgetName { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int NewToday { get; set; }
    public int Pending { get; set; }
    public int Completed { get; set; }
    public double Percentage { get; set; }
    public string Trend { get; set; } = "stable"; // up, down, stable
    public string? AlertMessage { get; set; }

    // Computed properties
    public string DisplayValue => $"{TotalCount}";
    public string TrendIcon => Trend switch
    {
        "up" => "icon-trending-up",
        "down" => "icon-trending-down",
        _ => "icon-trending-flat"
    };

    public string TrendColor => Trend switch
    {
        "up" => "text-success",
        "down" => "text-danger",
        _ => "text-muted"
    };

    public string AlertLevel => AlertMessage switch
    {
        "high" => "alert-danger",
        "medium" => "alert-warning",
        "low" => "alert-info",
        _ => ""
    };
}
