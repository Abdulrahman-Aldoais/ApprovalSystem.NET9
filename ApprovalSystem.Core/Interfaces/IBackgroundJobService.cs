using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApprovalSystem.Models.Entities;

namespace ApprovalSystem.Core.Interfaces;

/// <summary>
/// واجهة خدمة المهام الخلفية (Background Jobs)
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// تشغيل مهمة تصعيد تلقائي
    /// </summary>
    Task<bool> RunAutoEscalationAsync(Guid tenantId);

    /// <summary>
    /// تشغيل تنظيف البيانات
    /// </summary>
    Task<bool> RunDataCleanupAsync(Guid tenantId);

    /// <summary>
    /// إرسال تقارير دورية
    /// </summary>
    Task<bool> SendPeriodicReportsAsync(Guid tenantId);

    /// <summary>
    /// تشغيل مراقبة النظام
    /// </summary>
    Task<bool> RunSystemMonitoringAsync(Guid tenantId);

    /// <summary>
    /// إرسال تذكيرات
    /// </summary>
    Task<bool> SendRemindersAsync(Guid tenantId);

    /// <summary>
    /// حساب إحصائيات يومية
    /// </summary>
    Task<bool> CalculateDailyStatsAsync(DateTime? date = null);

    /// <summary>
    /// إنشاء تقرير شهري
    /// </summary>
    Task<byte?> GenerateMonthlyReportAsync(int month, int year, Guid tenantId);

    /// <summary>
    /// نسخ احتياطي للبيانات
    /// </summary>
    Task<bool> CreateDataBackupAsync(Guid tenantId);

    /// <summary>
    /// إشعار انتهاء صلاحية كلمات المرور
    /// </summary>
    Task<bool> NotifyPasswordExpiryAsync(int daysBeforeExpiry = 7);

    /// <summary>
    /// تنظيف الجلسات المنتهية الصلاحية
    /// </summary>
    Task<bool> CleanExpiredSessionsAsync();

    /// <summary>
    /// تحديث حالة الطلبات المتأخرة
    /// </summary>
    Task<bool> UpdateOverdueRequestsStatusAsync();

    /// <summary>
    /// حساب متوسط وقت المعالجة
    /// </summary>
    Task<bool> CalculateAverageProcessingTimesAsync();

    /// <summary>
    /// إرسال إحصائيات فورية
    /// </summary>
    Task<bool> SendInstantStatsAsync(Guid tenantId);

    /// <summary>
    /// تنظيف المرفقات القديمة
    /// </summary>
    Task<int> CleanOldAttachmentsAsync(int daysToKeep = 90);

    /// <summary>
    /// إشعار انتهاء المهلة
    /// </summary>
    Task<int> NotifyUpcomingDeadlinesAsync(int hoursBefore = 24);

    /// <summary>
    /// تشغيل مهمة مجدولة
    /// </summary>
    Task<bool> RunScheduledTaskAsync(string taskName, Dictionary<string, object>? parameters = null);

    /// <summary>
    /// الحصول على حالة المهام
    /// </summary>
    Task<List<JobStatus>> GetJobStatusesAsync();

    /// <summary>
    /// الحصول على سجل المهام
    /// </summary>
    Task<List<JobLog>> GetJobLogsAsync(DateTime startDate, DateTime endDate, int page = 1, int pageSize = 50);

    /// <summary>
    /// إعادة محاولة مهمة فاشلة
    /// </summary>
    Task<bool> RetryFailedJobAsync(Guid jobId);

    /// <summary>
    /// إلغاء مهمة قيد التشغيل
    /// </summary>
    Task<bool> CancelJobAsync(Guid jobId);
}

/// <summary>
/// نموذج حالة المهمة
/// </summary>
public class JobStatus
{
    public Guid JobId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Pending, Running, Completed, Failed, Cancelled
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public double? ExecutionTimeMinutes { get; set; }
    public string? Result { get; set; }
    public string Schedule { get; set; } = string.Empty; // cron expression or frequency
}

/// <summary>
/// نموذج سجل المهمة
/// </summary>
public class JobLog
{
    public Guid LogId { get; set; }
    public Guid JobId { get; set; }
    public string Level { get; set; } = "Info"; // Info, Warning, Error
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Exception { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public string? ExecutionId { get; set; }
}
