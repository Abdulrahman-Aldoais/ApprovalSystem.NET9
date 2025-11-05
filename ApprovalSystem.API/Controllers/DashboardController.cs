using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ApprovalSystem.Models.Entities;
using ApprovalSystem.Models.ViewModels;
using ApprovalSystem.ViewModels;
using ApprovalSystem.Core.Interfaces;
using ApprovalSystem.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ApprovalSystem.API.Controllers;

/// <summary>
/// Controller للوحة التحكم والإحصائيات
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IRequestService _requestService;
    private readonly IApprovalService _approvalService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService dashboardService,
        IRequestService requestService,
        IApprovalService approvalService,
        INotificationService notificationService,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _requestService = requestService;
        _approvalService = approvalService;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// الحصول على إحصائيات لوحة التحكم الرئيسية
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

            if (string.IsNullOrEmpty(userId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            var stats = await _dashboardService.GetDashboardStatsAsync(tenantId, userId);

            var response = new DashboardStatsViewModel
            {
                Overview = new OverviewStatsViewModel
                {
                    TotalRequests = stats.Overview.TotalRequests,
                    PendingRequests = stats.Overview.PendingRequests,
                    InProgressRequests = stats.Overview.InProgressRequests,
                    ApprovedRequests = stats.Overview.ApprovedRequests,
                    RejectedRequests = stats.Overview.RejectedRequests,
                    CancelledRequests = stats.Overview.CancelledRequests,
                    TotalAmount = stats.Overview.TotalAmount,
                    MyPendingApprovals = stats.Overview.MyPendingApprovals,
                    MyCreatedRequests = stats.Overview.MyCreatedRequests,
                    UrgentRequests = stats.Overview.UrgentRequests,
                    OverdueRequests = stats.Overview.OverdueRequests
                },
                Metrics = new MetricsViewModel
                {
                    ApprovalRate = stats.Metrics.ApprovalRate,
                    AverageProcessingTimeHours = stats.Metrics.AverageProcessingTimeHours,
                    RecentRequestsCount = stats.Metrics.RecentRequestsCount,
                    CompletionRate = stats.Metrics.CompletionRate,
                    EscalationRate = stats.Metrics.EscalationRate,
                    AverageApprovalTime = stats.Metrics.AverageApprovalTime
                },
                ByModule = stats.ByModule,
                StatusDistribution = new StatusDistributionViewModel
                {
                    Pending = stats.StatusDistribution.Pending,
                    InProgress = stats.StatusDistribution.InProgress,
                    Approved = stats.StatusDistribution.Approved,
                    Rejected = stats.StatusDistribution.Rejected,
                    Cancelled = stats.StatusDistribution.Cancelled
                },
                ByPriority = stats.ByPriority,
                RecentActivity = stats.RecentActivity?.Select(a => new RecentActivityViewModel
                {
                    Id = a.Id,
                    Action = a.Action,
                    RequestTitle = a.RequestTitle,
                    ActorName = a.ActorName,
                    CreatedAt = a.CreatedAt,
                    RequestId = a.RequestId
                }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard stats");
            return StatusCode(500, new { message = "حدث خطأ أثناء جلب إحصائيات لوحة التحكم" });
        }
    }

    /// <summary>
    /// الحصول على إحصائيات مفصلة للمؤسسة
    /// </summary>
    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? groupBy = "day")
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            // Check if user has admin/manager role for detailed analytics
            if (userRole != "Admin" && userRole != "Manager")
            {
                return Forbid("Access denied. Admin or Manager role required.");
            }

            var analytics = await _dashboardService.GetAnalyticsAsync(
                tenantId, startDate ?? DateTime.UtcNow.AddDays(-30), 
                endDate ?? DateTime.UtcNow, groupBy);

            var response = new AnalyticsViewModel
            {
                TimeRange = new TimeRangeViewModel
                {
                    StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                    EndDate = endDate ?? DateTime.UtcNow,
                    GroupBy = groupBy
                },
                RequestTrends = analytics.RequestTrends?.Select(t => new ChartDataViewModel
                {
                    Date = t.Date,
                    Value = t.Value,
                    Label = t.Label
                }).ToList(),
                ApprovalTrends = analytics.ApprovalTrends?.Select(t => new ChartDataViewModel
                {
                    Date = t.Date,
                    Value = t.Value,
                    Label = t.Label
                }).ToList(),
                StatusDistribution = analytics.StatusDistribution?.Select(s => new ChartDataViewModel
                {
                    Label = s.Label,
                    Value = s.Value
                }).ToList(),
                PriorityDistribution = analytics.PriorityDistribution?.Select(p => new ChartDataViewModel
                {
                    Label = p.Label,
                    Value = p.Value
                }).ToList(),
                ModuleDistribution = analytics.ModuleDistribution?.Select(m => new ChartDataViewModel
                {
                    Label = m.Label,
                    Value = m.Value
                }).ToList(),
                UserPerformance = analytics.UserPerformance?.Select(u => new UserPerformanceViewModel
                {
                    UserId = u.UserId,
                    UserName = u.UserName,
                    TotalApprovals = u.TotalApprovals,
                    AverageApprovalTime = u.AverageApprovalTime,
                    OnTimeRate = u.OnTimeRate
                }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics data");
            return StatusCode(500, new { message = "حدث خطأ أثناء جلب بيانات التحليلات" });
        }
    }

    /// <summary>
    /// الحصول على الطلبات العاجلة
    /// </summary>
    [HttpGet("urgent-requests")]
    public async Task<IActionResult> GetUrgentRequests()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

            if (string.IsNullOrEmpty(userId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            var urgentRequests = await _dashboardService.GetUrgentRequestsAsync(tenantId);

            var response = urgentRequests.Select(r => new RequestViewModel
            {
                Id = r.Id,
                Title = r.Title,
                Priority = r.Priority,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                DueDate = r.DueDate,
                IsOverdue = r.IsOverdue,
                Requester = new UserViewModel
                {
                    Name = r.Requester.Name,
                    Email = r.Requester.Email!
                }
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting urgent requests");
            return StatusCode(500, new { message = "حدث خطأ أثناء جلب الطلبات العاجلة" });
        }
    }

    /// <summary>
    /// الحصول على النشاطات الأخيرة
    /// </summary>
    [HttpGet("recent-activity")]
    public async Task<IActionResult> GetRecentActivity(
        [FromQuery] int count = 20)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

            if (string.IsNullOrEmpty(userId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            var recentActivities = await _dashboardService.GetRecentActivityAsync(tenantId, count);

            var response = recentActivities.Select(a => new RecentActivityViewModel
            {
                Id = a.Id,
                Action = a.Action,
                RequestTitle = a.RequestTitle,
                RequestId = a.RequestId,
                ActorName = a.ActorName,
                ActorId = a.ActorId,
                CreatedAt = a.CreatedAt,
                Details = a.Details
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent activity");
            return StatusCode(500, new { message = "حدث خطأ أثناء جلب النشاطات الأخيرة" });
        }
    }

    /// <summary>
    /// الحصول على ملخص الأداء الشهري
    /// </summary>
    [HttpGet("performance-summary")]
    public async Task<IActionResult> GetPerformanceSummary(
        [FromQuery] int month = -1,
        [FromQuery] int year = -1)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            if (userRole != "Admin" && userRole != "Manager")
            {
                return Forbid("Access denied. Admin or Manager role required.");
            }

            // Default to current month if not specified
            if (month == -1) month = DateTime.UtcNow.Month;
            if (year == -1) year = DateTime.UtcNow.Year;

            var summary = await _dashboardService.GetPerformanceSummaryAsync(tenantId, month, year);

            var response = new PerformanceSummaryViewModel
            {
                Month = month,
                Year = year,
                TotalRequests = summary.TotalRequests,
                CompletedRequests = summary.CompletedRequests,
                AverageProcessingTime = summary.AverageProcessingTime,
                ApprovalRate = summary.ApprovalRate,
                UserStats = summary.UserStats?.Select(u => new UserStatsViewModel
                {
                    UserId = u.UserId,
                    UserName = u.UserName,
                    TotalRequests = u.TotalRequests,
                    CompletedRequests = u.CompletedRequests,
                    AverageProcessingTime = u.AverageProcessingTime
                }).ToList(),
                ModuleStats = summary.ModuleStats?.Select(m => new ModuleStatsViewModel
                {
                    ModuleId = m.ModuleId,
                    ModuleName = m.ModuleName,
                    TotalRequests = m.TotalRequests,
                    AverageProcessingTime = m.AverageProcessingTime
                }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance summary");
            return StatusCode(500, new { message = "حدث خطأ أثناء جلب ملخص الأداء" });
        }
    }

    /// <summary>
    /// تحديث البيانات الفورية
    /// </summary>
    [HttpGet("live-update")]
    public async Task<IActionResult> GetLiveUpdates()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

            if (string.IsNullOrEmpty(userId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            var updates = await _dashboardService.GetLiveUpdatesAsync(tenantId, userId);

            var response = new LiveUpdatesViewModel
            {
                Timestamp = DateTime.UtcNow,
                NewRequests = updates.NewRequests?.Select(r => new RequestViewModel
                {
                    Id = r.Id,
                    Title = r.Title,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                }).ToList(),
                UpdatedRequests = updates.UpdatedRequests?.Select(r => new RequestViewModel
                {
                    Id = r.Id,
                    Title = r.Title,
                    Status = r.Status,
                    UpdatedAt = r.UpdatedAt
                }).ToList(),
                NewNotifications = updates.NewNotifications?.Select(n => new NotificationViewModel
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Priority = n.Priority,
                    CreatedAt = n.CreatedAt
                }).ToList(),
                UpdatedStats = new StatsUpdateViewModel
                {
                    PendingCount = updates.UpdatedStats?.PendingCount ?? 0,
                    ApprovedCount = updates.UpdatedStats?.ApprovedCount ?? 0,
                    RejectedCount = updates.UpdatedStats?.RejectedCount ?? 0
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting live updates");
            return StatusCode(500, new { message = "حدث خطأ أثناء جلب التحديثات الفورية" });
        }
    }
}
