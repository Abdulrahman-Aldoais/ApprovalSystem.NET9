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
/// خدمة إدارة الطلبات
/// </summary>
public class RequestService : IRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RequestService> _logger;

    public RequestService(ApplicationDbContext context, ILogger<RequestService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(List<Request> requests, int totalCount)> GetRequestsAsync(
        Guid tenantId, string userId, int pageNumber, int pageSize,
        string? status = null, string? priority = null, 
        Guid? requestTypeId = null, string? searchTerm = null)
    {
        try
        {
            var query = _context.Requests
                .Include(r => r.Requester)
                .Include(r => r.RequestType)
                .Include(r => r.ApprovalMatrix)
                .Include(r => r.Approvals)
                .Where(r => r.TenantId == tenantId);

            // Add filters
            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            if (!string.IsNullOrEmpty(priority))
                query = query.Where(r => r.Priority == priority);

            if (requestTypeId.HasValue)
                query = query.Where(r => r.RequestTypeId == requestTypeId.Value);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(r => r.Title.Contains(searchTerm) || 
                                        (r.Description != null && r.Description.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();

            var requests = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} requests for tenant {TenantId}", requests.Count, tenantId);

            return (requests, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving requests for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<Request?> GetRequestByIdAsync(Guid requestId, Guid tenantId, string userId)
    {
        try
        {
            var request = await _context.Requests
                .Include(r => r.Requester)
                .Include(r => r.RequestType)
                .Include(r => r.ApprovalMatrix)
                .Include(r => r.Approvals).ThenInclude(a => a.Approver)
                .Include(r => r.Attachments)
                .Include(r => r.RequestAudits)
                .Include(r => r.WorkflowTrackings)
                .FirstOrDefaultAsync(r => r.Id == requestId && r.TenantId == tenantId);

            if (request == null)
                return null;

            // Check user permissions (simplified for now)
            // In a real implementation, you would check if the user is:
            // 1. The requester
            // 2. An approver for this request
            // 3. An admin/manager

            return request;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving request {RequestId}", requestId);
            throw;
        }
    }

    public async Task<Request?> CreateRequestAsync(CreateRequestViewModel model, string userId, Guid tenantId)
    {
        try
        {
            var request = new Request
            {
                Title = model.Title,
                Description = model.Description,
                Amount = model.Amount,
                Data = model.Data != null ? System.Text.Json.JsonSerializer.Serialize(model.Data) : null,
                Priority = model.Priority ?? "medium",
                DueDate = model.DueDate,
                RequestTypeId = model.RequestTypeId,
                ApprovalMatrixId = model.ApprovalMatrixId,
                RequesterId = userId,
                TenantId = tenantId,
                Status = "pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            // Create audit trail
            var audit = new RequestAudit
            {
                RequestId = request.Id,
                TenantId = tenantId,
                ActionType = "created",
                ActorId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.RequestAudits.Add(audit);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Request created successfully: {RequestId} by user {UserId}", request.Id, userId);

            return await GetRequestByIdAsync(request.Id, tenantId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating request by user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> UpdateRequestAsync(Guid requestId, UpdateRequestViewModel model, string userId, Guid tenantId)
    {
        try
        {
            var request = await _context.Requests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.TenantId == tenantId);

            if (request == null)
                return false;

            // Check if user can update this request
            if (request.RequesterId != userId && !await CanUserUpdateRequest(requestId, userId))
                return false;

            var oldStatus = request.Status;

            request.Title = model.Title;
            request.Description = model.Description;
            request.Amount = model.Amount;
            request.Priority = model.Priority ?? "medium";
            request.DueDate = model.DueDate;
            request.Data = model.Data != null ? System.Text.Json.JsonSerializer.Serialize(model.Data) : null;
            request.UpdatedAt = DateTime.UtcNow;

            _context.Requests.Update(request);

            // Create audit trail
            var audit = new RequestAudit
            {
                RequestId = request.Id,
                TenantId = tenantId,
                ActionType = "updated",
                ActorId = userId,
                FromStatus = oldStatus,
                ToStatus = request.Status,
                Metadata = System.Text.Json.JsonSerializer.Serialize(new { model }),
                CreatedAt = DateTime.UtcNow
            };

            _context.RequestAudits.Add(audit);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Request {RequestId} updated successfully by user {UserId}", requestId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating request {RequestId} by user {UserId}", requestId, userId);
            return false;
        }
    }

    public async Task<bool> DeleteRequestAsync(Guid requestId, string userId, Guid tenantId)
    {
        try
        {
            var request = await _context.Requests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.TenantId == tenantId);

            if (request == null)
                return false;

            // Check if user can delete this request
            if (request.RequesterId != userId && !await CanUserDeleteRequest(requestId, userId))
                return false;

            // Only allow deletion if request is pending or cancelled
            if (request.Status != "pending" && request.Status != "cancelled")
                return false;

            var audit = new RequestAudit
            {
                RequestId = request.Id,
                TenantId = tenantId,
                ActionType = "deleted",
                ActorId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.RequestAudits.Add(audit);
            _context.Requests.Remove(request);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Request {RequestId} deleted by user {UserId}", requestId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting request {RequestId} by user {UserId}", requestId, userId);
            return false;
        }
    }

    public async Task<(List<Request> requests, int totalCount)> GetMyRequestsAsync(
        string userId, Guid tenantId, int pageNumber, int pageSize, string? status = null)
    {
        try
        {
            var query = _context.Requests
                .Include(r => r.RequestType)
                .Include(r => r.Approvals)
                .Where(r => r.RequesterId == userId && r.TenantId == tenantId);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            var totalCount = await query.CountAsync();

            var requests = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (requests, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user requests for user {UserId}", userId);
            throw;
        }
    }

    public async Task<RequestStatsViewModel> GetRequestStatsAsync(Guid tenantId, string? userId = null)
    {
        try
        {
            IQueryable<Request> query = _context.Requests.Where(r => r.TenantId == tenantId);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(r => r.RequesterId == userId);

            var requests = await query.ToListAsync();

            return new RequestStatsViewModel
            {
                TotalRequests = requests.Count,
                PendingRequests = requests.Count(r => r.Status == "pending"),
                InProgressRequests = requests.Count(r => r.Status == "in_progress"),
                ApprovedRequests = requests.Count(r => r.Status == "approved"),
                RejectedRequests = requests.Count(r => r.Status == "rejected"),
                CancelledRequests = requests.Count(r => r.Status == "cancelled"),
                TotalAmount = requests.Sum(r => r.Amount ?? 0),
                AverageProcessingTime = CalculateAverageProcessingTime(requests)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating request stats for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<List<Request>> SearchRequestsAsync(Guid tenantId, string searchTerm, int pageSize = 50)
    {
        try
        {
            var requests = await _context.Requests
                .Include(r => r.Requester)
                .Include(r => r.RequestType)
                .Where(r => r.TenantId == tenantId &&
                            (r.Title.Contains(searchTerm) ||
                             (r.Description != null && r.Description.Contains(searchTerm)) ||
                             r.Requester.Name.Contains(searchTerm)))
                .OrderByDescending(r => r.CreatedAt)
                .Take(pageSize)
                .ToListAsync();

            return requests;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching requests for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<List<Request>> GetUrgentRequestsAsync(Guid tenantId, int count = 10)
    {
        try
        {
            var requests = await _context.Requests
                .Include(r => r.Requester)
                .Include(r => r.RequestType)
                .Where(r => r.TenantId == tenantId &&
                            (r.Priority == "urgent" || r.Priority == "high" ||
                             (r.DueDate.HasValue && r.DueDate < DateTime.UtcNow.AddDays(1))))
                .OrderBy(r => r.DueDate ?? DateTime.MaxValue)
                .ThenByDescending(r => r.CreatedAt)
                .Take(count)
                .ToListAsync();

            return requests;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving urgent requests for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<List<Request>> GetOverdueRequestsAsync(Guid tenantId, int count = 20)
    {
        try
        {
            var requests = await _context.Requests
                .Include(r => r.Requester)
                .Include(r => r.RequestType)
                .Where(r => r.TenantId == tenantId &&
                            r.DueDate.HasValue &&
                            r.DueDate < DateTime.UtcNow &&
                            r.Status != "approved" &&
                            r.Status != "rejected" &&
                            r.Status != "cancelled")
                .OrderBy(r => r.DueDate)
                .Take(count)
                .ToListAsync();

            return requests;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving overdue requests for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<bool> AddAttachmentAsync(Guid requestId, string fileName, string filePath,
        string contentType, long fileSize, string uploadedById)
    {
        try
        {
            var request = await _context.Requests
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
                return false;

            var attachment = new Attachment
            {
                FileName = fileName,
                FilePath = filePath,
                FileType = fileName.Split('.').Last(),
                ContentType = contentType,
                FileSize = fileSize,
                RequestId = requestId,
                UploadedById = uploadedById,
                CreatedAt = DateTime.UtcNow
            };

            _context.Attachments.Add(attachment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Attachment {FileName} added to request {RequestId} by user {UserId}",
                fileName, requestId, uploadedById);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding attachment to request {RequestId}", requestId);
            return false;
        }
    }

    public async Task<List<Attachment>> GetRequestAttachmentsAsync(Guid requestId)
    {
        try
        {
            var attachments = await _context.Attachments
                .Include(a => a.UploadedBy)
                .Where(a => a.RequestId == requestId && a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return attachments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attachments for request {RequestId}", requestId);
            throw;
        }
    }

    public async Task<bool> DeleteAttachmentAsync(Guid attachmentId, string userId)
    {
        try
        {
            var attachment = await _context.Attachments
                .FirstOrDefaultAsync(a => a.Id == attachmentId);

            if (attachment == null)
                return false;

            // Check if user can delete this attachment
            if (attachment.UploadedById != userId && !await CanUserDeleteAttachment(attachmentId, userId))
                return false;

            attachment.IsActive = false;
            _context.Attachments.Update(attachment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Attachment {AttachmentId} deleted by user {UserId}", attachmentId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attachment {AttachmentId} by user {UserId}", attachmentId, userId);
            return false;
        }
    }

    public async Task<byte[]> ExportRequestsAsync(Guid tenantId, string? status = null,
        DateTime? startDate = null, DateTime? endDate = null, string format = "excel")
    {
        try
        {
            var query = _context.Requests
                .Include(r => r.Requester)
                .Include(r => r.RequestType)
                .Include(r => r.Approvals)
                .Where(r => r.TenantId == tenantId);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            if (startDate.HasValue)
                query = query.Where(r => r.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(r => r.CreatedAt <= endDate.Value);

            var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

            // Simple CSV export for now
            var csv = "عنوان الطلب,الحالة,الأولوية,المقدم,تاريخ الإنشاء,المبلغ\n";
            foreach (var request in requests)
            {
                csv += $"\"{request.Title}\",\"{request.Status}\",\"{request.Priority}\",\"{request.Requester.Name}\",\"{request.CreatedAt:yyyy-MM-dd}\",\"{request.Amount}\"\n";
            }

            return System.Text.Encoding.UTF8.GetBytes(csv);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting requests for tenant {TenantId}", tenantId);
            throw;
        }
    }

    // Helper methods
    private double CalculateAverageProcessingTime(List<Request> requests)
    {
        var completedRequests = requests
            .Where(r => r.Status == "approved" || r.Status == "rejected")
            .ToList();

        if (!completedRequests.Any())
            return 0;

        var totalTime = completedRequests
            .Sum(r => (r.UpdatedAt - r.CreatedAt).TotalHours);

        return totalTime / completedRequests.Count;
    }

    private async Task<bool> CanUserUpdateRequest(Guid requestId, string userId)
    {
        // Simplified permission check - implement proper authorization logic
        var approval = await _context.Approvals
            .FirstOrDefaultAsync(a => a.RequestId == requestId && a.ApproverId == userId);

        return approval != null;
    }

    private async Task<bool> CanUserDeleteRequest(Guid requestId, string userId)
    {
        // Simplified permission check - implement proper authorization logic
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user?.Role == "Admin" || user?.Role == "Manager";
    }

    private async Task<bool> CanUserDeleteAttachment(Guid attachmentId, string userId)
    {
        // Simplified permission check - implement proper authorization logic
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user?.Role == "Admin" || user?.Role == "Manager";
    }
}
