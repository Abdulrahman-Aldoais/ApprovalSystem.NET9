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
/// خدمة إدارة الموافقات
/// </summary>
public class ApprovalService : IApprovalService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApprovalService> _logger;

    public ApprovalService(ApplicationDbContext context, ILogger<ApprovalService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(List<Approval> approvals, int totalCount)> GetPendingApprovalsAsync(
        string approverId, Guid tenantId, int pageNumber, int pageSize)
    {
        try
        {
            var query = _context.Approvals
                .Include(a => a.Request)
                .Include(a => a.Request.Requester)
                .Include(a => a.Approver)
                .Include(a => a.ApprovalMatrix)
                .Where(a => a.TenantId == tenantId && 
                           a.ApproverId == approverId && 
                           a.Status == "Pending");

            var totalCount = await query.CountAsync();

            var approvals = await query
                .OrderBy(a => a.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} pending approvals for user {UserId}", approvals.Count, approverId);

            return (approvals, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending approvals for user {UserId}", approverId);
            throw;
        }
    }

    public async Task<bool> ApproveRequestAsync(Guid requestId, string? comments, string approverId, Guid tenantId)
    {
        try
        {
            var approval = await _context.Approvals
                .Include(a => a.Request)
                .Include(a => a.ApprovalMatrix)
                .FirstOrDefaultAsync(a => a.RequestId == requestId && 
                                         a.ApproverId == approverId && 
                                         a.TenantId == tenantId &&
                                         a.Status == "Pending");

            if (approval == null)
            {
                _logger.LogWarning("Approval not found for request {RequestId} and approver {ApproverId}", requestId, approverId);
                return false;
            }

            // Update approval status
            approval.Status = "Approved";
            approval.UpdatedAt = DateTime.UtcNow;
            approval.UpdatedBy = approverId;
            approval.Comments = comments;
            approval.ApprovedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Request {RequestId} approved by user {ApproverId}", requestId, approverId);

            // Check if this was the final approval needed
            await CheckAndCompleteRequestWorkflowAsync(requestId, tenantId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving request {RequestId} by user {ApproverId}", requestId, approverId);
            return false;
        }
    }

    public async Task<bool> RejectRequestAsync(Guid requestId, string reason, string? comments, string approverId, Guid tenantId)
    {
        try
        {
            var approval = await _context.Approvals
                .Include(a => a.Request)
                .FirstOrDefaultAsync(a => a.RequestId == requestId && 
                                         a.ApproverId == approverId && 
                                         a.TenantId == tenantId &&
                                         a.Status == "Pending");

            if (approval == null)
            {
                _logger.LogWarning("Approval not found for request {RequestId} and approver {ApproverId}", requestId, approverId);
                return false;
            }

            // Update approval status
            approval.Status = "Rejected";
            approval.UpdatedAt = DateTime.UtcNow;
            approval.UpdatedBy = approverId;
            approval.Comments = comments;
            approval.ApprovedAt = DateTime.UtcNow;

            // Update request status
            approval.Request.Status = "Rejected";
            approval.Request.RejectionReason = reason;
            approval.Request.UpdatedAt = DateTime.UtcNow;
            approval.Request.UpdatedBy = approverId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Request {RequestId} rejected by user {ApproverId} with reason: {Reason}", 
                requestId, approverId, reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting request {RequestId} by user {ApproverId}", requestId, approverId);
            return false;
        }
    }

    public async Task<bool> EscalateRequestAsync(Guid requestId, string reason, string? comments, string escalatedById, Guid tenantId)
    {
        try
        {
            var currentApproval = await _context.Approvals
                .FirstOrDefaultAsync(a => a.RequestId == requestId && 
                                         a.ApproverId == escalatedById && 
                                         a.TenantId == tenantId &&
                                         a.Status == "Pending");

            if (currentApproval == null)
            {
                _logger.LogWarning("Current approval not found for escalation of request {RequestId}", requestId);
                return false;
            }

            // Create escalation record
            var escalation = new ApprovalEscalation
            {
                Id = Guid.NewGuid(),
                ApprovalId = currentApproval.Id,
                Reason = reason,
                TriggeredAt = DateTime.UtcNow,
                EscalatedByUserId = escalatedById,
                TenantId = tenantId,
                Comments = comments,
                Status = "Pending"
            };

            _context.ApprovalEscalations.Add(escalation);

            // Update current approval
            currentApproval.Status = "Escalated";
            currentApproval.UpdatedAt = DateTime.UtcNow;
            currentApproval.UpdatedBy = escalatedById;
            currentApproval.Comments = comments;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Request {RequestId} escalated by user {EscalatedById}", requestId, escalatedById);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating request {RequestId} by user {EscalatedById}", requestId, escalatedById);
            return false;
        }
    }

    public async Task<Approval?> CreateApprovalAsync(Guid requestId, string approverId, int stage, Guid tenantId)
    {
        try
        {
            var approval = new Approval
            {
                Id = Guid.NewGuid(),
                RequestId = requestId,
                ApproverId = approverId,
                Stage = stage,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                TenantId = tenantId
            };

            _context.Approvals.Add(approval);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created approval {ApprovalId} for request {RequestId} at stage {Stage}", 
                approval.Id, requestId, stage);

            return approval;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating approval for request {RequestId} at stage {Stage}", requestId, stage);
            throw;
        }
    }

    public async Task<List<Approval>> GetRequestApprovalsAsync(Guid requestId)
    {
        try
        {
            var approvals = await _context.Approvals
                .Include(a => a.Approver)
                .Include(a => a.ApprovalEscalations)
                .Where(a => a.RequestId == requestId)
                .OrderBy(a => a.Stage)
                .ThenBy(a => a.CreatedAt)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} approvals for request {RequestId}", approvals.Count, requestId);

            return approvals;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving approvals for request {RequestId}", requestId);
            throw;
        }
    }

    public async Task<Approval?> GetApprovalByIdAsync(Guid approvalId)
    {
        try
        {
            var approval = await _context.Approvals
                .Include(a => a.Request)
                .Include(a => a.Approver)
                .Include(a => a.ApprovalMatrix)
                .Include(a => a.ApprovalEscalations)
                .FirstOrDefaultAsync(a => a.Id == approvalId);

            if (approval != null)
            {
                _logger.LogInformation("Retrieved approval {ApprovalId}", approvalId);
            }

            return approval;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving approval {ApprovalId}", approvalId);
            throw;
        }
    }

    public async Task<bool> UpdateApprovalStatusAsync(Guid approvalId, string status, string? comments, string? rejectionReason = null)
    {
        try
        {
            var approval = await _context.Approvals
                .Include(a => a.Request)
                .FirstOrDefaultAsync(a => a.Id == approvalId);

            if (approval == null)
            {
                _logger.LogWarning("Approval {ApprovalId} not found", approvalId);
                return false;
            }

            approval.Status = status;
            approval.UpdatedAt = DateTime.UtcNow;
            approval.Comments = comments;

            if (status == "Approved" || status == "Rejected")
            {
                approval.ApprovedAt = DateTime.UtcNow;
            }

            if (!string.IsNullOrEmpty(rejectionReason))
            {
                approval.Request.RejectionReason = rejectionReason;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated approval {ApprovalId} status to {Status}", approvalId, status);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating approval {ApprovalId} status", approvalId);
            return false;
        }
    }

    public async Task<ApprovalStatsViewModel> GetApprovalStatsAsync(Guid tenantId, string? userId = null)
    {
        try
        {
            var query = _context.Approvals.Where(a => a.TenantId == tenantId);

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(a => a.ApproverId == userId);
            }

            var totalApprovals = await query.CountAsync();
            var pendingApprovals = await query.CountAsync(a => a.Status == "Pending");
            var approvedApprovals = await query.CountAsync(a => a.Status == "Approved");
            var rejectedApprovals = await query.CountAsync(a => a.Status == "Rejected");
            var escalatedApprovals = await query.CountAsync(a => a.Status == "Escalated");

            // Calculate average processing time
            var completedApprovals = await query
                .Where(a => a.ApprovedAt.HasValue)
                .Select(a => new { a.CreatedAt, a.ApprovedAt })
                .ToListAsync();

            var averageProcessingTime = completedApprovals.Any() 
                ? completedApprovals.Average(a => (a.ApprovedAt!.Value - a.CreatedAt).TotalHours)
                : 0;

            // Calculate approval rate
            var approvalRate = totalApprovals > 0 
                ? (double)approvedApprovals / totalApprovals * 100 
                : 0;

            var stats = new ApprovalStatsViewModel
            {
                TotalApprovals = totalApprovals,
                PendingApprovals = pendingApprovals,
                ApprovedApprovals = approvedApprovals,
                RejectedApprovals = rejectedApprovals,
                EscalatedApprovals = escalatedApprovals,
                AverageProcessingTime = Math.Round(averageProcessingTime, 2),
                ApprovalRate = Math.Round(approvalRate, 2)
            };

            _logger.LogInformation("Retrieved approval stats for tenant {TenantId}, user {UserId}", tenantId, userId ?? "All");

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving approval stats for tenant {TenantId}, user {UserId}", tenantId, userId ?? "All");
            throw;
        }
    }

    public async Task<ApprovalMatrix?> GetApprovalMatrixForRequestAsync(Guid requestId)
    {
        try
        {
            var request = await _context.Requests
                .Include(r => r.RequestType)
                .Include(r => r.RequestType.ApprovalMatrix)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request?.RequestType?.ApprovalMatrix != null)
            {
                _logger.LogInformation("Retrieved approval matrix {MatrixId} for request {RequestId}", 
                    request.RequestType.ApprovalMatrix.Id, requestId);
                return request.RequestType.ApprovalMatrix;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving approval matrix for request {RequestId}", requestId);
            throw;
        }
    }

    public async Task<ApprovalEscalation?> CreateEscalationAsync(
        Guid approvalId, string reason, string escalatedTo, string escalatedById, Guid tenantId)
    {
        try
        {
            var escalation = new ApprovalEscalation
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                Reason = reason,
                TriggeredAt = DateTime.UtcNow,
                EscalatedToUserId = escalatedTo,
                EscalatedByUserId = escalatedById,
                TenantId = tenantId,
                Comments = reason,
                Status = "Pending"
            };

            _context.ApprovalEscalations.Add(escalation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created escalation {EscalationId} for approval {ApprovalId}", escalation.Id, approvalId);

            return escalation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating escalation for approval {ApprovalId}", approvalId);
            throw;
        }
    }

    public async Task<List<ApprovalEscalation>> GetPendingEscalationsAsync(Guid tenantId)
    {
        try
        {
            var escalations = await _context.ApprovalEscalations
                .Include(e => e.Approval)
                .Include(e => e.Approval.Request)
                .Include(e => e.Approval.Approver)
                .Where(e => e.TenantId == tenantId && e.Status == "Pending")
                .OrderBy(e => e.TriggeredAt)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} pending escalations for tenant {TenantId}", escalations.Count, tenantId);

            return escalations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending escalations for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<bool> ResolveEscalationAsync(Guid escalationId, string resolvedById)
    {
        try
        {
            var escalation = await _context.ApprovalEscalations
                .Include(e => e.Approval)
                .FirstOrDefaultAsync(e => e.Id == escalationId);

            if (escalation == null)
            {
                _logger.LogWarning("Escalation {EscalationId} not found", escalationId);
                return false;
            }

            escalation.Status = "Resolved";
            escalation.ResolvedAt = DateTime.UtcNow;
            escalation.ResolvedByUserId = resolvedById;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Resolved escalation {EscalationId} by user {ResolvedById}", escalationId, resolvedById);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving escalation {EscalationId} by user {ResolvedById}", escalationId, resolvedById);
            return false;
        }
    }

    public async Task<List<Approval>> GetUserApprovalHistoryAsync(string userId, Guid tenantId, int count = 50)
    {
        try
        {
            var approvals = await _context.Approvals
                .Include(a => a.Request)
                .Include(a => a.Request.Requester)
                .Where(a => a.ApproverId == userId && a.TenantId == tenantId && a.Status != "Pending")
                .OrderByDescending(a => a.UpdatedAt)
                .Take(count)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} approval history entries for user {UserId}", approvals.Count, userId);

            return approvals;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving approval history for user {UserId}", userId);
            throw;
        }
    }

    public async Task<TimeSpan> CalculateExpectedProcessingTimeAsync(Guid requestId)
    {
        try
        {
            var request = await _context.Requests
                .Include(r => r.RequestType)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request?.RequestType?.ProcessingTimeInHours != null)
            {
                return TimeSpan.FromHours(request.RequestType.ProcessingTimeInHours.Value);
            }

            // Default to 24 hours if no specific time is set
            return TimeSpan.FromHours(24);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating processing time for request {RequestId}", requestId);
            return TimeSpan.FromHours(24); // Return default time on error
        }
    }

    public async Task<bool> SendApprovalReminderAsync(Guid approvalId)
    {
        try
        {
            var approval = await _context.Approvals
                .Include(a => a.Approver)
                .Include(a => a.Request)
                .FirstOrDefaultAsync(a => a.Id == approvalId);

            if (approval == null)
            {
                _logger.LogWarning("Approval {ApprovalId} not found for reminder", approvalId);
                return false;
            }

            // Create reminder notification
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = approval.ApproverId,
                Title = "تذكير بموافقة معلقة",
                Message = $"لديك موافقة معلقة للطلب: {approval.Request.Title}",
                Type = "Reminder",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                TenantId = approval.TenantId
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Sent approval reminder for approval {ApprovalId} to user {UserId}", approvalId, approval.ApproverId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending approval reminder for approval {ApprovalId}", approvalId);
            return false;
        }
    }

    public async Task<EscalationStatsViewModel> GetEscalationStatsAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _context.ApprovalEscalations.Where(e => e.TenantId == tenantId);

            if (startDate.HasValue)
                query = query.Where(e => e.TriggeredAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(e => e.TriggeredAt <= endDate.Value);

            var totalEscalations = await query.CountAsync();
            var pendingEscalations = await query.CountAsync(e => e.Status == "Pending");
            var resolvedEscalations = await query.CountAsync(e => e.Status == "Resolved");

            // Calculate average resolution time
            var resolvedWithTime = await query
                .Where(e => e.Status == "Resolved" && e.ResolvedAt.HasValue)
                .Select(e => new { e.TriggeredAt, e.ResolvedAt })
                .ToListAsync();

            var averageResolutionTime = resolvedWithTime.Any() 
                ? resolvedWithTime.Average(e => (e.ResolvedAt!.Value - e.TriggeredAt).TotalHours)
                : 0;

            // Count escalations by reason patterns
            var escalationsByReason = await query
                .CountAsync(e => e.Reason.ToLower().Contains("urgent") || 
                               e.Reason.ToLower().Contains("delay") ||
                               e.Reason.ToLower().Contains("timeout"));

            var stats = new EscalationStatsViewModel
            {
                TotalEscalations = totalEscalations,
                PendingEscalations = pendingEscalations,
                ResolvedEscalations = resolvedEscalations,
                AverageResolutionTime = Math.Round(averageResolutionTime, 2),
                EscalationsByReason = escalationsByReason
            };

            _logger.LogInformation("Retrieved escalation stats for tenant {TenantId}", tenantId);

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving escalation stats for tenant {TenantId}", tenantId);
            throw;
        }
    }

    private async Task CheckAndCompleteRequestWorkflowAsync(Guid requestId, Guid tenantId)
    {
        try
        {
            var request = await _context.Requests
                .Include(r => r.Approvals)
                .FirstOrDefaultAsync(r => r.Id == requestId && r.TenantId == tenantId);

            if (request == null)
                return;

            // Check if all required approvals are completed
            var pendingApprovals = request.Approvals.Count(a => a.Status == "Pending");
            
            if (pendingApprovals == 0)
            {
                // All approvals completed
                var approvedApprovals = request.Approvals.Count(a => a.Status == "Approved");
                var totalApprovals = request.Approvals.Count;

                if (approvedApprovals == totalApprovals)
                {
                    request.Status = "Completed";
                    _logger.LogInformation("Request {RequestId} workflow completed successfully", requestId);
                }
                else if (request.Approvals.Any(a => a.Status == "Rejected"))
                {
                    request.Status = "Rejected";
                    _logger.LogInformation("Request {RequestId} workflow completed with rejection", requestId);
                }
                else
                {
                    // Handle escalated approvals
                    request.Status = "Completed";
                    _logger.LogInformation("Request {RequestId} workflow completed with escalations", requestId);
                }

                request.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking workflow completion for request {RequestId}", requestId);
        }
    }
}