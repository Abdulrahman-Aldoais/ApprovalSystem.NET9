using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApprovalSystem.Models.Entities;
using ApprovalSystem.ViewModels;

namespace ApprovalSystem.Core.Interfaces;

/// <summary>
/// واجهة خدمة إدارة الطلبات
/// </summary>
public interface IRequestService
{
    /// <summary>
    /// الحصول على قائمة الطلبات مع التصفية والترقيم
    /// </summary>
    Task<(List<Request> requests, int totalCount)> GetRequestsAsync(
        Guid tenantId, string userId, int pageNumber, int pageSize,
        string? status = null, string? priority = null, 
        Guid? requestTypeId = null, string? searchTerm = null);

    /// <summary>
    /// الحصول على تفاصيل طلب محدد
    /// </summary>
    Task<Request?> GetRequestByIdAsync(Guid requestId, Guid tenantId, string userId);

    /// <summary>
    /// إنشاء طلب جديد
    /// </summary>
    Task<Request?> CreateRequestAsync(CreateRequestViewModel model, string userId, Guid tenantId);

    /// <summary>
    /// تحديث طلب
    /// </summary>
    Task<bool> UpdateRequestAsync(Guid requestId, UpdateRequestViewModel model, string userId, Guid tenantId);

    /// <summary>
    /// حذف طلب
    /// </summary>
    Task<bool> DeleteRequestAsync(Guid requestId, string userId, Guid tenantId);

    /// <summary>
    /// الحصول على طلبات المستخدم
    /// </summary>
    Task<(List<Request> requests, int totalCount)> GetMyRequestsAsync(
        string userId, Guid tenantId, int pageNumber, int pageSize, string? status = null);

    /// <summary>
    /// الحصول على إحصائيات الطلبات
    /// </summary>
    Task<RequestStatsViewModel> GetRequestStatsAsync(Guid tenantId, string? userId = null);

    /// <summary>
    /// البحث في الطلبات
    /// </summary>
    Task<List<Request>> SearchRequestsAsync(
        Guid tenantId, string searchTerm, int pageSize = 50);

    /// <summary>
    /// الحصول على الطلبات العاجلة
    /// </summary>
    Task<List<Request>> GetUrgentRequestsAsync(Guid tenantId, int count = 10);

    /// <summary>
    /// الحصول على الطلبات المتأخرة
    /// </summary>
    Task<List<Request>> GetOverdueRequestsAsync(Guid tenantId, int count = 20);

    /// <summary>
    /// إضافة مرفق للطلب
    /// </summary>
    Task<bool> AddAttachmentAsync(Guid requestId, string fileName, string filePath, 
        string contentType, long fileSize, string uploadedById);

    /// <summary>
    /// الحصول على مرفقات الطلب
    /// </summary>
    Task<List<Attachment>> GetRequestAttachmentsAsync(Guid requestId);

    /// <summary>
    /// حذف مرفق
    /// </summary>
    Task<bool> DeleteAttachmentAsync(Guid attachmentId, string userId);

    /// <summary>
    /// تصدير الطلبات
    /// </summary>
    Task<byte[]> ExportRequestsAsync(Guid tenantId, string? status = null, 
        DateTime? startDate = null, DateTime? endDate = null, string format = "excel");
}
