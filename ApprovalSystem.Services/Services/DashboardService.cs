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
/// خدمة لوحة التحكم والإحصائيات
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(ApplicationDbContext context, ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardStats> GetDashboardStatsAsync(Guid tenantId, string? userId = null)
    {
        try
        {
            var overview = new OverviewStats();
            var metrics = new Metrics();
            var byModule = new Dictionary<string, int>();
            var statusDistribution = new StatusDistribution();

            var baseQuery = _context.Requests.Where(r => r.TenantId == tenantId);
            var userQuery = userId != null ? baseQuery.Where(r => r.RequesterId == userId) : baseQuery;

            // Get base statistics
            var allRequests = await baseQuery.ToListAsync();
            var userRequests = userId != null ? await userQuery.ToListAsync() : new List<Request>();
            
            // Overview stats
            overview.TotalRequests = allRequests.Count;
            overview.PendingRequests = allRequests.Count(r => r.Status == "Pending");
            overview.InProgressRequests = allRequests.Count(r => r.Status == "InProgress");
            overview.ApprovedRequests = allRequests.Count(r => r.Status == "Approved");
            overview.RejectedRequests = allRequests.Count(r => r.Status == "Rejected");
            overview.CancelledRequests = allRequests.Count(r => r.Status == "Cancelled");
            overview.TotalAmount = allRequests.Sum(r => r.Amount ?? 0);

            if (userId != null)
            {
                overview.MyPendingApprovals = await _context.Approvals
                    .CountAsync(a => a.ApproverId == userId && a.TenantId == tenantId && a.Status == "Pending");
                
                overview.MyCreatedRequests = userRequests.Count;
            }

            overview.UrgentRequests = allRequests.Count(r => r.Priority == "High" && r.Status == "Pending");
            overview.OverdueRequests = allRequests.Count(r => r.DueDate.HasValue && 
                r.DueDate < DateTime.UtcNow && r.Status != "Completed" && r.Status != "Cancelled");

            // Status distribution
            statusDistribution.Pending = overview.PendingRequests;
            statusDistribution.InProgress = overview.InProgressRequests;
            statusDistribution.Approved = overview.ApprovedRequests;
            statusDistribution.Rejected = overview.RejectedRequests;
            statusDistribution.Cancelled = overview.CancelledRequests;

            // Module distribution
            var moduleData = await _context.Requests
                .Include(r => r.RequestType)
                .Include(r => r.RequestType.Module)
                .Where(r => r.TenantId == tenantId)
                .GroupBy(r => r.RequestType.Module.Name)
                .Select(g => new { ModuleName = g.Key, Count = g.Count() })
                .ToListAsync();

            byModule = moduleData.ToDictionary(x => x.ModuleName ?? "غير محدد", x => x.Count);

            // Priority distribution
            var priorityData = allRequests
                .GroupBy(r => r.Priority)
                .ToDictionary(g => g.Key, g => g.Count());

            // Metrics calculation
            var completedRequests = allRequests.Where(r => r.Status == "Approved" || r.Status == "Rejected").ToList();
            metrics.ApprovalRate = overview.TotalRequests > 0 
                ? (double)overview.ApprovedRequests / overview.TotalRequests * 100 
                : 0;

            // Calculate average processing time
            var requestsWithProcessingTime = allRequests
                .Where(r => r.Status == "Completed" && r.UpdatedAt.HasValue)
                .Select(r => (r.UpdatedAt!.Value - r.CreatedAt).TotalHours)
                .ToList();

            metrics.AverageProcessingTimeHours = requestsWithProcessingTime.Any() 
                ? requestsWithProcessingTime.Average() 
                : 0;

            // Recent requests (last 7 days)
            metrics.RecentRequestsCount = allRequests
                .Count(r => r.CreatedAt >= DateTime.UtcNow.AddDays(-7));

            metrics.CompletionRate = overview.TotalRequests > 0 
                ? (double)(overview.ApprovedRequests + overview.RejectedRequests) / overview.TotalRequests * 100 
                : 0;

            // Escalation rate
            var escalatedApprovals = await _context.Approvals
                .CountAsync(a => a.TenantId == tenantId && a.Status == "Escalated");

            metrics.EscalationRate = overview.TotalRequests > 0 
                ? (double)escalatedApprovals / overview.TotalRequests * 100 
                : 0;

            // Average approval time
            var completedApprovals = await _context.Approvals
                .Where(a => a.TenantId == tenantId && a.ApprovedAt.HasValue)
                .Select(a => (a.ApprovedAt!.Value - a.CreatedAt).TotalHours)
                .ToListAsync();

            metrics.AverageApprovalTime = completedApprovals.Any() 
                ? completedApprovals.Average() 
                : 0;

            // Efficiency score (combination of metrics)
            metrics.EfficiencyScore = Math.Round(
                (metrics.ApprovalRate + metrics.CompletionRate + (100 - metrics.EscalationRate)) / 3, 2);

            return new DashboardStats
            {
                Overview = overview,
                Metrics = metrics,
                ByModule = byModule,
                StatusDistribution = statusDistribution,
                ByPriority = priorityData,
                RecentActivity = await GetRecentActivityAsync(tenantId, 10)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard stats for tenant {TenantId}, user {UserId}", tenantId, userId ?? "All");
            throw;
        }
    }

    public async Task<DashboardAnalytics> GetAnalyticsAsync(Guid tenantId, DateTime startDate, DateTime endDate, string groupBy = "day")
    {
        try
        {
            var analytics = new DashboardAnalytics();

            // Get all data for the period
            var requests = await _context.Requests
                .Include(r => r.RequestType)
                .Include(r => r.RequestType.Module)
                .Where(r => r.TenantId == tenantId && r.CreatedAt >= startDate && r.CreatedAt <= endDate)
                .ToListAsync();

            var approvals = await _context.Approvals
                .Include(a => a.Request)
                .Where(a => a.TenantId == tenantId && a.CreatedAt >= startDate && a.CreatedAt <= endDate)
                .ToListAsync();

            // Request trends
            analytics.RequestTrends = await GenerateTrendData(requests, "request", groupBy);

            // Approval trends
            analytics.ApprovalTrends = await GenerateTrendData(approvals, "approval", groupBy);

            // Status distribution
            analytics.StatusDistribution = GenerateDistributionData(requests.GroupBy(r => r.Status));

            // Priority distribution
            analytics.PriorityDistribution = GenerateDistributionData(requests.GroupBy(r => r.Priority));

            // Module distribution
            var moduleGroups = requests.GroupBy(r => r.RequestType?.Module?.Name ?? "غير محدد");
            analytics.ModuleDistribution = GenerateDistributionData(moduleGroups);

            // User performance
            analytics.UserPerformance = await CalculateUserPerformanceAsync(tenantId, startDate, endDate);

            _logger.LogInformation("Retrieved analytics for tenant {TenantId} from {StartDate} to {EndDate}", tenantId, startDate, endDate);

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<List<Request>> GetUrgentRequestsAsync(Guid tenantId, int count = 10)
    {
        try
        {
            var urgentRequests = await _context.Requests
                .Include(r => r.Requester)
                .Include(r => r.RequestType)
                .Where(r => r.TenantId == tenantId && 
                           r.Priority == "High" && 
                           r.Status == "Pending")
                .OrderBy(r => r.CreatedAt)
                .Take(count)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} urgent requests for tenant {TenantId}", urgentRequests.Count, tenantId);

            return urgentRequests;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting urgent requests for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<List<RecentActivity>> GetRecentActivityAsync(Guid tenantId, int count = 20)
    {
        try
        {
            // Get recent request updates
            var requestUpdates = await _context.RequestAudits
                .Include(a => a.Request)
                .Include(a => a.User)
                .Where(a => a.TenantId == tenantId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(count / 2)
                .Select(a => new RecentActivity
                {
                    Id = a.Id,
                    Action = a.Action,
                    RequestTitle = a.Request.Title,
                    ActorName = a.User?.DisplayName ?? "مستخدم مجهول",
                    ActorId = a.UserId,
                    RequestId = a.RequestId,
                    CreatedAt = a.CreatedAt,
                    Details = $"تم {a.Action} الطلب"
                })
                .ToListAsync();

            // Get recent approval activities
            var approvalActivities = await _context.Approvals
                .Include(a => a.Request)
                .Include(a => a.Approver)
                .Where(a => a.TenantId == tenantId && a.ApprovedAt.HasValue)
                .OrderByDescending(a => a.ApprovedAt)
                .Take(count / 2)
                .Select(a => new RecentActivity
                {
                    Id = a.Id,
                    Action = a.Status,
                    RequestTitle = a.Request.Title,
                    ActorName = a.Approver?.DisplayName ?? "مستخدم مجهول",
                    ActorId = a.ApproverId,
                    RequestId = a.RequestId,
                    CreatedAt = a.ApprovedAt!.Value,
                    Details = $"{a.Status} الطلب"
                })
                .ToListAsync();

            // Combine and sort by date
            var activities = requestUpdates.Concat(approvalActivities)
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .ToList();

            _logger.LogInformation("Retrieved {Count} recent activities for tenant {TenantId}", activities.Count, tenantId);

            return activities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent activities for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<PerformanceSummary> GetPerformanceSummaryAsync(Guid tenantId, int month, int year)
    {
        try
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var requests = await _context.Requests
                .Include(r => r.Requester)
                .Include(r => r.RequestType)
                .Include(r => r.RequestType.Module)
                .Where(r => r.TenantId == tenantId && 
                           r.CreatedAt >= startDate && 
                           r.CreatedAt <= endDate)
                .ToListAsync();

            var summary = new PerformanceSummary
            {
                TotalRequests = requests.Count,
                CompletedRequests = requests.Count(r => r.Status == "Approved" || r.Status == "Rejected"),
                AverageProcessingTime = requests.Any() 
                    ? requests.Where(r => r.Status == "Completed").Average(r => 
                        (r.UpdatedAt?.Subtract(r.CreatedAt).TotalHours) ?? 0)
                    : 0,
                ApprovalRate = requests.Count > 0 
                    ? (double)requests.Count(r => r.Status == "Approved") / requests.Count * 100 
                    : 0
            };

            // User statistics
            var userStats = requests
                .GroupBy(r => r.Requester)
                .Select(g => new UserStats
                {
                    UserId = g.Key.Id,
                    UserName = g.Key.DisplayName,
                    TotalRequests = g.Count(),
                    CompletedRequests = g.Count(r => r.Status == "Approved" || r.Status == "Rejected"),
                    AverageProcessingTime = g.Where(r => r.Status == "Completed").Any()
                        ? g.Where(r => r.Status == "Completed").Average(r => 
                            (r.UpdatedAt?.Subtract(r.CreatedAt).TotalHours) ?? 0)
                        : 0
                })
                .ToList();

            summary.UserStats = userStats;

            // Module statistics
            var moduleStats = requests
                .GroupBy(r => r.RequestType?.Module)
                .Where(g => g.Key != null)
                .Select(g => new ModuleStats
                {
                    ModuleId = g.Key!.Id,
                    ModuleName = g.Key.Name,
                    TotalRequests = g.Count(),
                    AverageProcessingTime = g.Where(r => r.Status == "Completed").Any()
                        ? g.Where(r => r.Status == "Completed").Average(r => 
                            (r.UpdatedAt?.Subtract(r.CreatedAt).TotalHours) ?? 0)
                        : 0
                })
                .ToList();

            summary.ModuleStats = moduleStats;

            _logger.LogInformation("Retrieved performance summary for tenant {TenantId} for {Month}/{Year}", tenantId, month, year);

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance summary for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<LiveUpdates> GetLiveUpdatesAsync(Guid tenantId, string userId)
    {
        try
        {
            var timestamp = DateTime.UtcNow;
            var fiveMinutesAgo = timestamp.AddMinutes(-5);

            // Get new requests in last 5 minutes
            var newRequests = await _context.Requests
                .Include(r => r.Requester)
                .Include(r => r.RequestType)
                .Where(r => r.TenantId == tenantId && r.CreatedAt >= fiveMinutesAgo)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToListAsync();

            // Get updated requests in last 5 minutes
            var updatedRequests = await _context.Requests
                .Include(r => r.Requester)
                .Include(r => r.RequestType)
                .Where(r => r.TenantId == tenantId && r.UpdatedAt >= fiveMinutesAgo && r.UpdatedAt != r.CreatedAt)
                .OrderByDescending(r => r.UpdatedAt)
                .Take(10)
                .ToListAsync();

            // Get new notifications for user
            var newNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.TenantId == tenantId && n.CreatedAt >= fiveMinutesAgo)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToListAsync();

            // Get updated stats
            var overview = await GetOverviewStatsAsync(tenantId);

            return new LiveUpdates
            {
                Timestamp = timestamp,
                NewRequests = newRequests,
                UpdatedRequests = updatedRequests,
                NewNotifications = newNotifications,
                UpdatedStats = new StatsUpdate
                {
                    PendingCount = overview.PendingRequests,
                    ApprovedCount = overview.ApprovedRequests,
                    RejectedCount = overview.RejectedRequests,
                    UrgentCount = overview.UrgentRequests,
                    OverdueCount = overview.OverdueRequests
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting live updates for tenant {TenantId}, user {UserId}", tenantId, userId);
            throw;
        }
    }

    public async Task<List<UserStats>> GetUserStatsAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _context.Requests
                .Include(r => r.Requester)
                .Where(r => r.TenantId == tenantId);

            if (startDate.HasValue)
                query = query.Where(r => r.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(r => r.CreatedAt <= endDate.Value);

            var userStats = await query
                .GroupBy(r => r.Requester)
                .Select(g => new UserStats
                {
                    UserId = g.Key.Id,
                    UserName = g.Key.DisplayName,
                    TotalRequests = g.Count(),
                    CompletedRequests = g.Count(r => r.Status == "Approved" || r.Status == "Rejected"),
                    AverageProcessingTime = g.Where(r => r.Status == "Completed").Any()
                        ? g.Where(r => r.Status == "Completed").Average(r => 
                            (r.UpdatedAt?.Subtract(r.CreatedAt).TotalHours) ?? 0)
                        : 0
                })
                .OrderByDescending(us => us.TotalRequests)
                .ToListAsync();

            _logger.LogInformation("Retrieved user stats for tenant {TenantId}", tenantId);

            return userStats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user stats for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<List<ModuleStats>> GetModuleStatsAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _context.Requests
                .Include(r => r.RequestType)
                .Include(r => r.RequestType.Module)
                .Where(r => r.TenantId == tenantId);

            if (startDate.HasValue)
                query = query.Where(r => r.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(r => r.CreatedAt <= endDate.Value);

            var moduleStats = await query
                .GroupBy(r => r.RequestType?.Module)
                .Where(g => g.Key != null)
                .Select(g => new ModuleStats
                {
                    ModuleId = g.Key!.Id,
                    ModuleName = g.Key.Name,
                    TotalRequests = g.Count(),
                    AverageProcessingTime = g.Where(r => r.Status == "Completed").Any()
                        ? g.Where(r => r.Status == "Completed").Average(r => 
                            (r.UpdatedAt?.Subtract(r.CreatedAt).TotalHours) ?? 0)
                        : 0
                })
                .OrderByDescending(ms => ms.TotalRequests)
                .ToListAsync();

            _logger.LogInformation("Retrieved module stats for tenant {TenantId}", tenantId);

            return moduleStats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting module stats for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<List<ChartData>> GetRequestTrendsAsync(Guid tenantId, DateTime startDate, DateTime endDate, string groupBy = "day")
    {
        try
        {
            var requests = await _context.Requests
                .Where(r => r.TenantId == tenantId && r.CreatedAt >= startDate && r.CreatedAt <= endDate)
                .ToListAsync();

            return await GenerateTrendData(requests, "request", groupBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting request trends for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<List<ChartData>> GetApprovalTrendsAsync(Guid tenantId, DateTime startDate, DateTime endDate, string groupBy = "day")
    {
        try
        {
            var approvals = await _context.Approvals
                .Where(a => a.TenantId == tenantId && a.CreatedAt >= startDate && a.CreatedAt <= endDate)
                .ToListAsync();

            return await GenerateTrendData(approvals, "approval", groupBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting approval trends for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<Dictionary<string, int>> GetStatusDistributionAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _context.Requests.Where(r => r.TenantId == tenantId);

            if (startDate.HasValue)
                query = query.Where(r => r.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(r => r.CreatedAt <= endDate.Value);

            var distribution = await query
                .GroupBy(r => r.Status)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            _logger.LogInformation("Retrieved status distribution for tenant {TenantId}", tenantId);

            return distribution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status distribution for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<Dictionary<string, int>> GetPriorityDistributionAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _context.Requests.Where(r => r.TenantId == tenantId);

            if (startDate.HasValue)
                query = query.Where(r => r.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(r => r.CreatedAt <= endDate.Value);

            var distribution = await query
                .GroupBy(r => r.Priority)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            _logger.LogInformation("Retrieved priority distribution for tenant {TenantId}", tenantId);

            return distribution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting priority distribution for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<List<UserPerformance>> GetUserPerformanceAsync(Guid tenantId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var userPerformances = new List<UserPerformance>();

            var approvals = await _context.Approvals
                .Include(a => a.Approver)
                .Include(a => a.Request)
                .Where(a => a.TenantId == tenantId && 
                           a.ApprovedAt.HasValue && 
                           a.ApprovedAt >= startDate && 
                           a.ApprovedAt <= endDate)
                .ToListAsync();

            var userGroups = approvals.GroupBy(a => a.Approver);

            foreach (var group in userGroups)
            {
                var user = group.Key;
                if (user == null) continue;

                var totalApprovals = group.Count();
                var onTimeApprovals = group.Count(a => 
                    a.Request.DueDate.HasValue && 
                    a.ApprovedAt.HasValue && 
                    a.ApprovedAt.Value <= a.Request.DueDate.Value);

                var processingTimes = group.Select(a => 
                    (a.ApprovedAt!.Value - a.CreatedAt).TotalHours).ToList();

                var performance = new UserPerformance
                {
                    UserId = user.Id,
                    UserName = user.DisplayName,
                    TotalApprovals = totalApprovals,
                    AverageApprovalTime = processingTimes.Any() ? processingTimes.Average() : 0,
                    OnTimeRate = totalApprovals > 0 ? (double)onTimeApprovals / totalApprovals * 100 : 0
                };

                userPerformances.Add(performance);
            }

            return userPerformances.OrderByDescending(up => up.TotalApprovals).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user performance for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<byte[]> GenerateCustomReportAsync(Guid tenantId, ReportConfig config)
    {
        try
        {
            // TODO: Implement custom report generation
            // This would generate PDF, Excel, or CSV reports based on the configuration
            _logger.LogInformation("Generating custom report for tenant {TenantId} with config {Config}", tenantId, config);
            
            // For now, return empty byte array
            return new byte[0];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating custom report for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<DashboardSettings> GetDashboardSettingsAsync(string userId, Guid tenantId)
    {
        try
        {
            // TODO: Get from database
            // For now, return default settings
            return new DashboardSettings
            {
                AutoRefresh = true,
                RefreshIntervalSeconds = 30,
                RequestsListCount = 20,
                ActivityLogCount = 20,
                ShowUrgentRequests = true,
                ShowStats = true,
                ShowCharts = true,
                Theme = "auto",
                Language = "ar",
                TimeZone = "Arabia Standard Time",
                EnabledWidgets = new List<string> { "overview", "recent-activity", "urgent-requests", "stats" },
                WidgetOrder = new Dictionary<string, int>
                {
                    { "overview", 1 },
                    { "recent-activity", 2 },
                    { "urgent-requests", 3 },
                    { "stats", 4 }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard settings for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> UpdateDashboardSettingsAsync(string userId, Guid tenantId, DashboardSettings settings)
    {
        try
        {
            // TODO: Save to database
            _logger.LogInformation("Updating dashboard settings for user {UserId}: AutoRefresh={AutoRefresh}, Theme={Theme}", 
                userId, settings.AutoRefresh, settings.Theme);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dashboard settings for user {UserId}", userId);
            return false;
        }
    }

    private async Task<OverviewStats> GetOverviewStatsAsync(Guid tenantId)
    {
        var allRequests = await _context.Requests.Where(r => r.TenantId == tenantId).ToListAsync();

        return new OverviewStats
        {
            TotalRequests = allRequests.Count,
            PendingRequests = allRequests.Count(r => r.Status == "Pending"),
            ApprovedRequests = allRequests.Count(r => r.Status == "Approved"),
            RejectedRequests = allRequests.Count(r => r.Status == "Rejected"),
            UrgentRequests = allRequests.Count(r => r.Priority == "High" && r.Status == "Pending"),
            OverdueRequests = allRequests.Count(r => r.DueDate.HasValue && 
                r.DueDate < DateTime.UtcNow && r.Status != "Completed" && r.Status != "Cancelled")
        };
    }

    private async Task<List<ChartData>> GenerateTrendData<T>(List<T> data, string dataType, string groupBy) where T : class
    {
        if (!data.Any()) return new List<ChartData>();

        DateTime GetDate(T item) => dataType switch
        {
            "request" => (item as Request)?.CreatedAt ?? DateTime.MinValue,
            "approval" => (item as Approval)?.CreatedAt ?? DateTime.MinValue,
            _ => DateTime.MinValue
        };

        var groupedData = dataType switch
        {
            "request" => groupBy switch
            {
                "hour" => data.Cast<Request>().GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month, r.CreatedAt.Day, r.CreatedAt.Hour }),
                "day" => data.Cast<Request>().GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month, r.CreatedAt.Day }),
                "week" => data.Cast<Request>().GroupBy(r => new { r.CreatedAt.Year, ISOWeek.GetWeekOfYear(r.CreatedAt) }),
                "month" => data.Cast<Request>().GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month }),
                _ => data.Cast<Request>().GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month, r.CreatedAt.Day })
            },
            "approval" => groupBy switch
            {
                "hour" => data.Cast<Approval>().GroupBy(a => new { a.CreatedAt.Year, a.CreatedAt.Month, a.CreatedAt.Day, a.CreatedAt.Hour }),
                "day" => data.Cast<Approval>().GroupBy(a => new { a.CreatedAt.Year, a.CreatedAt.Month, a.CreatedAt.Day }),
                "week" => data.Cast<Approval>().GroupBy(a => new { a.CreatedAt.Year, ISOWeek.GetWeekOfYear(a.CreatedAt) }),
                "month" => data.Cast<Approval>().GroupBy(a => new { a.CreatedAt.Year, a.CreatedAt.Month }),
                _ => data.Cast<Approval>().GroupBy(a => new { a.CreatedAt.Year, a.CreatedAt.Month, a.CreatedAt.Day })
            },
            _ => throw new ArgumentException("Unsupported data type")
        };

        var result = new List<ChartData>();
        foreach (var group in groupedData.OrderBy(g => g.Key.GetHashCode()))
        {
            var key = group.Key;
            var date = groupBy switch
            {
                "hour" => new DateTime(key.Year, key.Month, key.Day, key.Hour, 0, 0),
                "day" => new DateTime(key.Year, key.Month, key.Day),
                "week" => ISOWeek.ToDateTime(key.Year, (int)key.Week, DayOfWeek.Monday),
                "month" => new DateTime(key.Year, key.Month, 1),
                _ => new DateTime(key.Year, key.Month, key.Day)
            };

            result.Add(new ChartData
            {
                Date = date,
                Label = date.ToString(groupBy switch
                {
                    "hour" => "yyyy-MM-dd HH:00",
                    "day" => "yyyy-MM-dd",
                    "week" => "yyyy-'W'WW",
                    "month" => "yyyy-MM",
                    _ => "yyyy-MM-dd"
                }),
                Value = group.Count()
            });
        }

        return result;
    }

    private List<ChartData> GenerateDistributionData<T>(IEnumerable<IGrouping<T, Request>> groups)
    {
        return groups.Select(g => new ChartData
        {
            Label = g.Key?.ToString() ?? "غير محدد",
            Value = g.Count()
        }).ToList();
    }

    private async Task<List<UserPerformance>> CalculateUserPerformanceAsync(Guid tenantId, DateTime startDate, DateTime endDate)
    {
        var userPerformance = new List<UserPerformance>();

        var approvals = await _context.Approvals
            .Include(a => a.Approver)
            .Where(a => a.TenantId == tenantId && 
                       a.ApprovedAt.HasValue && 
                       a.ApprovedAt >= startDate && 
                       a.ApprovedAt <= endDate)
            .ToListAsync();

        var userGroups = approvals.GroupBy(a => a.Approver);

        foreach (var group in userGroups)
        {
            var user = group.Key;
            if (user == null) continue;

            var totalApprovals = group.Count();
            var processingTimes = group.Select(a => 
                (a.ApprovedAt!.Value - a.CreatedAt).TotalHours).ToList();

            userPerformance.Add(new UserPerformance
            {
                UserId = user.Id,
                UserName = user.DisplayName,
                TotalApprovals = totalApprovals,
                AverageApprovalTime = processingTimes.Any() ? processingTimes.Average() : 0,
                OnTimeRate = 85.0 // Placeholder value
            });
        }

        return userPerformance.OrderByDescending(up => up.TotalApprovals).ToList();
    }
}