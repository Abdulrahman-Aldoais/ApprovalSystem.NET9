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
using Microsoft.EntityFrameworkCore;

namespace ApprovalSystem.API.Controllers;

/// <summary>
/// Controller لإدارة الطلبات
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RequestsController : ControllerBase
{
    private readonly IRequestService _requestService;
    private readonly IApprovalService _approvalService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<RequestsController> _logger;

    public RequestsController(
        IRequestService requestService,
        IApprovalService approvalService,
        INotificationService notificationService,
        ILogger<RequestsController> logger)
    {
        _requestService = requestService;
        _approvalService = approvalService;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// الحصول على قائمة الطلبات
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRequests(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] Guid? requestTypeId = null,
        [FromQuery] string? searchTerm = null)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());
            
            if (string.IsNullOrEmpty(userId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            var (requests, totalCount) = await _requestService.GetRequestsAsync(
                tenantId, userId, pageNumber, pageSize, status, priority, requestTypeId, searchTerm);

            var requestViewModels = requests.Select(r => new RequestViewModel
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description,
                Amount = r.Amount,
                Status = r.Status,
                Priority = r.Priority,
                DueDate = r.DueDate,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                Requester = new UserViewModel
                {
                    Id = r.Requester.Id,
                    Name = r.Requester.Name,
                    Email = r.Requester.Email!
                },
                RequestType = new RequestTypeViewModel
                {
                    Id = r.RequestType.Id,
                    Name = r.RequestType.Name
                },
                IsOverdue = r.IsOverdue,
                CurrentStage = r.Approvals.OrderByDescending(a => a.Stage).FirstOrDefault()?.Stage ?? 0
            }).ToList();

            var response = new PagedResponse<RequestViewModel>
            {
                Items = requestViewModels,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting requests list");
            return StatusCode(500, new { message = "حدث خطأ أثناء جلب قائمة الطلبات" });
        }
    }

    /// <summary>
    /// الحصول على تفاصيل طلب محدد
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRequest(Guid id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

            var request = await _requestService.GetRequestByIdAsync(id, tenantId, userId);
            if (request == null)
            {
                return NotFound(new { message = "الطلب غير موجود" });
            }

            var requestViewModel = new RequestDetailViewModel
            {
                Id = request.Id,
                Title = request.Title,
                Description = request.Description,
                Amount = request.Amount,
                Status = request.Status,
                Priority = request.Priority,
                DueDate = request.DueDate,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt,
                Requester = new UserViewModel
                {
                    Id = request.Requester.Id,
                    Name = request.Requester.Name,
                    Email = request.Requester.Email!,
                    Department = request.Requester.Department
                },
                RequestType = new RequestTypeViewModel
                {
                    Id = request.RequestType.Id,
                    Name = request.RequestType.Name,
                    Description = request.RequestType.Description
                },
                IsOverdue = request.IsOverdue,
                Data = request.GetData<Dictionary<string, object>>(),
                Approvals = request.Approvals.OrderBy(a => a.Stage).Select(a => new ApprovalViewModel
                {
                    Id = a.Id,
                    Stage = a.Stage,
                    Status = a.Status,
                    Comments = a.Comments,
                    ApprovedAt = a.ApprovedAt,
                    Approver = new UserViewModel
                    {
                        Id = a.Approver.Id,
                        Name = a.Approver.Name,
                        Email = a.Approver.Email!
                    },
                    IsOverdue = a.IsOverdue,
                    ProcessingTime = a.ProcessingTime
                }).ToList(),
                Attachments = request.Attachments.Where(a => a.IsActive).Select(a => new AttachmentViewModel
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FileType = a.FileType,
                    FileSize = a.FileSize,
                    FileUrl = a.FileUrl,
                    FileSizeFormatted = a.FileSizeFormatted,
                    UploadedBy = new UserViewModel
                    {
                        Id = a.UploadedBy.Id,
                        Name = a.UploadedBy.Name
                    },
                    CreatedAt = a.CreatedAt
                }).ToList(),
                AuditTrail = request.RequestAudits.OrderByDescending(ra => ra.CreatedAt).Select(ra => new RequestAuditViewModel
                {
                    Id = ra.Id,
                    ActionType = ra.ActionType,
                    FromStatus = ra.FromStatus,
                    ToStatus = ra.ToStatus,
                    ActorName = ra.ActorName,
                    Metadata = ra.GetMetadata<Dictionary<string, object>>(),
                    CreatedAt = ra.CreatedAt
                }).ToList()
            };

            return Ok(requestViewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting request details for {RequestId}", id);
            return StatusCode(500, new { message = "حدث خطأ أثناء جلب تفاصيل الطلب" });
        }
    }

    /// <summary>
    /// إنشاء طلب جديد
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateRequest([FromBody] CreateRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "بيانات الطلب غير صحيحة", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
        }

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

            if (string.IsNullOrEmpty(userId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            var request = await _requestService.CreateRequestAsync(model, userId, tenantId);
            
            if (request == null)
            {
                return BadRequest(new { message = "فشل في إنشاء الطلب. تحقق من صحة البيانات." });
            }

            // Send notifications to approvers
            await _notificationService.SendApprovalNotificationsAsync(request);

            var response = new RequestViewModel
            {
                Id = request.Id,
                Title = request.Title,
                Status = request.Status,
                Priority = request.Priority,
                CreatedAt = request.CreatedAt,
                Requester = new UserViewModel
                {
                    Id = request.Requester.Id,
                    Name = request.Requester.Name,
                    Email = request.Requester.Email!
                }
            };

            _logger.LogInformation("Request created successfully: {RequestId}", request.Id);

            return CreatedAtAction(nameof(GetRequest), new { id = request.Id }, new
            {
                message = "تم إنشاء الطلب بنجاح",
                request = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating request");
            return StatusCode(500, new { message = "حدث خطأ أثناء إنشاء الطلب" });
        }
    }

    /// <summary>
    /// تحديث طلب
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRequest(Guid id, [FromBody] UpdateRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "بيانات التحديث غير صحيحة", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
        }

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

            if (string.IsNullOrEmpty(userId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            var result = await _requestService.UpdateRequestAsync(id, model, userId, tenantId);
            
            if (!result)
            {
                return NotFound(new { message = "الطلب غير موجود أو ليس لديك صلاحية لتحديثه" });
            }

            _logger.LogInformation("Request updated successfully: {RequestId}", id);

            return Ok(new { message = "تم تحديث الطلب بنجاح" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating request {RequestId}", id);
            return StatusCode(500, new { message = "حدث خطأ أثناء تحديث الطلب" });
        }
    }

    /// <summary>
    /// حذف طلب
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRequest(Guid id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

            if (string.IsNullOrEmpty(userId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            var result = await _requestService.DeleteRequestAsync(id, userId, tenantId);
            
            if (!result)
            {
                return NotFound(new { message = "الطلب غير موجود أو ليس لديك صلاحية لحذفه" });
            }

            _logger.LogInformation("Request deleted successfully: {RequestId}", id);

            return Ok(new { message = "تم حذف الطلب بنجاح" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting request {RequestId}", id);
            return StatusCode(500, new { message = "حدث خطأ أثناء حذف الطلب" });
        }
    }

    /// <summary>
    /// الموافقة على طلب
    /// </summary>
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveRequest(Guid id, [FromBody] ApproveRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "بيانات الموافقة غير صحيحة", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
        }

        try
        {
            var approverId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

            if (string.IsNullOrEmpty(approverId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            var result = await _approvalService.ApproveRequestAsync(id, model.Comments, approverId, tenantId);
            
            if (!result)
            {
                return BadRequest(new { message = "فشل في الموافقة على الطلب. تحقق من صلاحياتك أو حالة الطلب." });
            }

            // Send notification to requester
            await _notificationService.SendApprovalNotificationAsync(id, approverId, "approved", model.Comments);

            _logger.LogInformation("Request approved successfully: {RequestId} by {ApproverId}", id, approverId);

            return Ok(new { message = "تم الموافقة على الطلب بنجاح" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving request {RequestId}", id);
            return StatusCode(500, new { message = "حدث خطأ أثناء الموافقة على الطلب" });
        }
    }

    /// <summary>
    /// رفض طلب
    /// </summary>
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectRequest(Guid id, [FromBody] RejectRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "بيانات رفض الطلب غير صحيحة", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
        }

        try
        {
            var approverId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

            if (string.IsNullOrEmpty(approverId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            var result = await _approvalService.RejectRequestAsync(id, model.Reason, model.Comments, approverId, tenantId);
            
            if (!result)
            {
                return BadRequest(new { message = "فشل في رفض الطلب. تحقق من صلاحياتك أو حالة الطلب." });
            }

            // Send notification to requester
            await _notificationService.SendApprovalNotificationAsync(id, approverId, "rejected", model.Comments);

            _logger.LogInformation("Request rejected successfully: {RequestId} by {ApproverId}", id, approverId);

            return Ok(new { message = "تم رفض الطلب بنجاح" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting request {RequestId}", id);
            return StatusCode(500, new { message = "حدث خطأ أثناء رفض الطلب" });
        }
    }

    /// <summary>
    /// الحصول على طلبات المستخدم
    /// </summary>
    [HttpGet("my-requests")]
    public async Task<IActionResult> GetMyRequests(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

            if (string.IsNullOrEmpty(userId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            var (requests, totalCount) = await _requestService.GetMyRequestsAsync(userId, tenantId, pageNumber, pageSize, status);

            var requestViewModels = requests.Select(r => new RequestViewModel
            {
                Id = r.Id,
                Title = r.Title,
                Status = r.Status,
                Priority = r.Priority,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                IsOverdue = r.IsOverdue,
                RequestType = new RequestTypeViewModel
                {
                    Name = r.RequestType.Name
                }
            }).ToList();

            var response = new PagedResponse<RequestViewModel>
            {
                Items = requestViewModels,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user's requests");
            return StatusCode(500, new { message = "حدث خطأ أثناء جلب طلباتك" });
        }
    }

    /// <summary>
    /// الحصول على طلبات الموافقة المعلقة
    /// </summary>
    [HttpGet("pending-approvals")]
    public async Task<IActionResult> GetPendingApprovals(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var approverId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

            if (string.IsNullOrEmpty(approverId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            var (approvals, totalCount) = await _approvalService.GetPendingApprovalsAsync(approverId, tenantId, pageNumber, pageSize);

            var approvalViewModels = approvals.Select(a => new PendingApprovalViewModel
            {
                Id = a.Id,
                RequestId = a.Request.Id,
                RequestTitle = a.Request.Title,
                RequesterName = a.Request.Requester.Name,
                Stage = a.Stage,
                Status = a.Status,
                CreatedAt = a.CreatedAt,
                IsOverdue = a.IsOverdue,
                RequestType = new RequestTypeViewModel
                {
                    Name = a.Request.RequestType.Name
                }
            }).ToList();

            var response = new PagedResponse<PendingApprovalViewModel>
            {
                Items = approvalViewModels,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending approvals");
            return StatusCode(500, new { message = "حدث خطأ أثناء جلب الموافقات المعلقة" });
        }
    }

    /// <summary>
    /// تصعيد طلب
    /// </summary>
    [HttpPost("{id}/escalate")]
    public async Task<IActionResult> EscalateRequest(Guid id, [FromBody] EscalateRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "بيانات التصعيد غير صحيحة", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
        }

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

            if (string.IsNullOrEmpty(userId) || tenantId == Guid.Empty)
            {
                return Unauthorized();
            }

            var result = await _approvalService.EscalateRequestAsync(id, model.Reason, model.Comments, userId, tenantId);
            
            if (!result)
            {
                return BadRequest(new { message = "فشل في تصعيد الطلب. تحقق من صحة البيانات أو حالة الطلب." });
            }

            _logger.LogInformation("Request escalated successfully: {RequestId} by {UserId}", id, userId);

            return Ok(new { message = "تم تصعيد الطلب بنجاح" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating request {RequestId}", id);
            return StatusCode(500, new { message = "حدث خطأ أثناء تصعيد الطلب" });
        }
    }
}
