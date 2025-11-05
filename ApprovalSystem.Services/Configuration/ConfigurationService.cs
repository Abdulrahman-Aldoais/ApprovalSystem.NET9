using ApprovalSystem.Infrastructure.Data;
using ApprovalSystem.Models.DTOs;
using ApprovalSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ApprovalSystem.Services.Configuration;

/// <summary>
/// واجهة خدمة إدارة الإعدادات
/// </summary>
public interface IConfigurationService
{
    // CRUD Operations
    Task<WorkflowConfigurationDto?> GetByIdAsync(int id, int tenantId);
    Task<List<WorkflowConfigurationDto>> GetByTenantAsync(int tenantId);
    Task<List<WorkflowConfigurationDto>> GetByRequestTypeAsync(Guid requestTypeId, int tenantId);
    Task<WorkflowConfigurationDto> CreateAsync(CreateWorkflowConfigurationDto dto, int tenantId, string userId);
    Task<WorkflowConfigurationDto?> UpdateAsync(int id, UpdateWorkflowConfigurationDto dto, int tenantId, string userId);
    Task<bool> DeleteAsync(int id, int tenantId, string userId);
    Task<bool> ActivateAsync(int id, int tenantId, string userId);
    Task<bool> DeactivateAsync(int id, int tenantId, string userId);
    Task<WorkflowConfigurationDto?> CloneAsync(int id, int tenantId, string userId);

    // Validation & Rules
    Task<ValidationResultDto> ValidateConfigurationAsync(CreateWorkflowConfigurationDto dto);
    Task<RuleEvaluationResultDto> EvaluateWorkflowRulesAsync(int configId, Dictionary<string, object> requestData);
    Task<bool> CheckStartConditionsAsync(int configId, Dictionary<string, object> requestData);
    Task<bool> CheckCompletionConditionsAsync(int configId, Dictionary<string, object> requestData);

    // Workflow Management
    Task<WorkflowConfigurationDto?> GetActiveConfigurationForRequestAsync(Guid requestTypeId, Dictionary<string, object> requestData, int tenantId);
    Task<List<WorkflowConfigurationDto>> GetCompatibleConfigurationsAsync(Dictionary<string, object> requestData, int tenantId);
    Task<bool> PublishConfigurationAsync(int id, int tenantId, string userId);
    Task<bool> ArchiveConfigurationAsync(int id, int tenantId, string userId);

    // Statistics & Reporting
    Task<WorkflowStatisticsDto> GetStatisticsAsync(int tenantId);
    Task<List<WorkflowConfigurationDto>> SearchConfigurationsAsync(string searchTerm, int tenantId);
    Task<List<WorkflowConfigurationDto>> GetConfigurationHistoryAsync(int baseConfigId, int tenantId);
}

/// <summary>
/// تنفيذ خدمة إدارة الإعدادات
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly ApplicationDbContext _context;
    private readonly IRuleEngine _ruleEngine;

    public ConfigurationService(ApplicationDbContext context, IRuleEngine ruleEngine)
    {
        _context = context;
        _ruleEngine = ruleEngine;
    }

    #region CRUD Operations

    /// <summary>
    /// الحصول على إعداد بالمعرف
    /// </summary>
    public async Task<WorkflowConfigurationDto?> GetByIdAsync(int id, int tenantId)
    {
        var config = await _context.WorkflowConfigurations
            .Include(w => w.RequestType)
            .Include(w => w.Tenant)
            .Where(w => w.Id == id && w.TenantId == tenantId && !w.IsDeleted)
            .FirstOrDefaultAsync();

        return config != null ? MapToDto(config) : null;
    }

    /// <summary>
    /// الحصول على إعدادات المستأجر
    /// </summary>
    public async Task<List<WorkflowConfigurationDto>> GetByTenantAsync(int tenantId)
    {
        var configs = await _context.WorkflowConfigurations
            .Include(w => w.RequestType)
            .Where(w => w.TenantId == tenantId && !w.IsDeleted)
            .OrderBy(w => w.WorkflowName)
            .ToListAsync();

        return configs.Select(MapToDto).ToList();
    }

    /// <summary>
    /// الحصول على إعدادات نوع الطلب
    /// </summary>
    public async Task<List<WorkflowConfigurationDto>> GetByRequestTypeAsync(Guid requestTypeId, int tenantId)
    {
        var configs = await _context.WorkflowConfigurations
            .Include(w => w.RequestType)
            .Where(w => w.RequestTypeId == requestTypeId && w.TenantId == tenantId && !w.IsDeleted)
            .OrderBy(w => w.Priority)
            .ThenBy(w => w.WorkflowName)
            .ToListAsync();

        return configs.Select(MapToDto).ToList();
    }

    /// <summary>
    /// إنشاء إعداد جديد
    /// </summary>
    public async Task<WorkflowConfigurationDto> CreateAsync(CreateWorkflowConfigurationDto dto, int tenantId, string userId)
    {
        // التحقق من صحة البيانات
        var validation = await ValidateConfigurationAsync(dto);
        if (!validation.IsValid)
        {
            throw new ArgumentException($"Invalid configuration: {string.Join(", ", validation.Errors)}");
        }

        var config = new WorkflowConfiguration
        {
            TenantId = tenantId,
            WorkflowName = dto.WorkflowName,
            Description = dto.Description,
            RequestTypeId = dto.RequestTypeId,
            WorkflowDefinition = JsonSerializer.Serialize(dto.WorkflowDefinition ?? new object()),
            EvaluationRules = JsonSerializer.Serialize(dto.EvaluationRules),
            EscalationSettings = JsonSerializer.Serialize(dto.EscalationSettings ?? new object()),
            NotificationSettings = JsonSerializer.Serialize(dto.NotificationSettings ?? new object()),
            StartConditions = JsonSerializer.Serialize(dto.StartConditions),
            CompletionConditions = JsonSerializer.Serialize(dto.CompletionConditions),
            DefaultData = JsonSerializer.Serialize(dto.DefaultData ?? new object()),
            Priority = dto.Priority,
            IsActive = dto.IsActive,
            RequiresManualApproval = dto.RequiresManualApproval,
            SupportsParallelApproval = dto.SupportsParallelApproval,
            MaxExecutionTimeHours = dto.MaxExecutionTimeHours,
            MaxRetryCount = dto.MaxRetryCount,
            Status = "Draft",
            Version = "1.0",
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.WorkflowConfigurations.Add(config);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(config.Id, tenantId)
            ?? throw new InvalidOperationException("Failed to retrieve created configuration");
    }

    /// <summary>
    /// تحديث إعداد موجود
    /// </summary>
    public async Task<WorkflowConfigurationDto?> UpdateAsync(int id, UpdateWorkflowConfigurationDto dto, int tenantId, string userId)
    {
        var config = await _context.WorkflowConfigurations
            .Where(w => w.Id == id && w.TenantId == tenantId && !w.IsDeleted)
            .FirstOrDefaultAsync();

        if (config == null)
        {
            return null;
        }

        // تحديث الحقول المُرسلة فقط
        if (!string.IsNullOrEmpty(dto.WorkflowName))
            config.WorkflowName = dto.WorkflowName;

        if (dto.Description != null)
            config.Description = dto.Description;

        if (dto.WorkflowDefinition != null)
            config.WorkflowDefinition = JsonSerializer.Serialize(dto.WorkflowDefinition);

        if (dto.EvaluationRules != null)
            config.EvaluationRules = JsonSerializer.Serialize(dto.EvaluationRules);

        if (dto.EscalationSettings != null)
            config.EscalationSettings = JsonSerializer.Serialize(dto.EscalationSettings);

        if (dto.NotificationSettings != null)
            config.NotificationSettings = JsonSerializer.Serialize(dto.NotificationSettings);

        if (dto.StartConditions != null)
            config.StartConditions = JsonSerializer.Serialize(dto.StartConditions);

        if (dto.CompletionConditions != null)
            config.CompletionConditions = JsonSerializer.Serialize(dto.CompletionConditions);

        if (dto.DefaultData != null)
            config.DefaultData = JsonSerializer.Serialize(dto.DefaultData);

        if (dto.Priority.HasValue)
            config.Priority = dto.Priority.Value;

        if (dto.IsActive.HasValue)
            config.IsActive = dto.IsActive.Value;

        if (dto.RequiresManualApproval.HasValue)
            config.RequiresManualApproval = dto.RequiresManualApproval.Value;

        if (dto.SupportsParallelApproval.HasValue)
            config.SupportsParallelApproval = dto.SupportsParallelApproval.Value;

        if (dto.MaxExecutionTimeHours.HasValue)
            config.MaxExecutionTimeHours = dto.MaxExecutionTimeHours.Value;

        if (dto.MaxRetryCount.HasValue)
            config.MaxRetryCount = dto.MaxRetryCount.Value;

        if (!string.IsNullOrEmpty(dto.Status))
            config.Status = dto.Status;

        config.UpdatedAt = DateTime.UtcNow;
        config.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id, tenantId);
    }

    /// <summary>
    /// حذف إعداد (Soft Delete)
    /// </summary>
    public async Task<bool> DeleteAsync(int id, int tenantId, string userId)
    {
        var config = await _context.WorkflowConfigurations
            .Where(w => w.Id == id && w.TenantId == tenantId && !w.IsDeleted)
            .FirstOrDefaultAsync();

        if (config == null)
        {
            return false;
        }

        config.IsDeleted = true;
        config.DeletedAt = DateTime.UtcNow;
        config.DeletedBy = userId;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// تفعيل إعداد
    /// </summary>
    public async Task<bool> ActivateAsync(int id, int tenantId, string userId)
    {
        var config = await _context.WorkflowConfigurations
            .Where(w => w.Id == id && w.TenantId == tenantId && !w.IsDeleted)
            .FirstOrDefaultAsync();

        if (config == null)
        {
            return false;
        }

        config.IsActive = true;
        config.UpdatedAt = DateTime.UtcNow;
        config.UpdatedBy = userId;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// إلغاء تفعيل إعداد
    /// </summary>
    public async Task<bool> DeactivateAsync(int id, int tenantId, string userId)
    {
        var config = await _context.WorkflowConfigurations
            .Where(w => w.Id == id && w.TenantId == tenantId && !w.IsDeleted)
            .FirstOrDefaultAsync();

        if (config == null)
        {
            return false;
        }

        config.IsActive = false;
        config.UpdatedAt = DateTime.UtcNow;
        config.UpdatedBy = userId;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// استنساخ إعداد
    /// </summary>
    public async Task<WorkflowConfigurationDto?> CloneAsync(int id, int tenantId, string userId)
    {
        var originalConfig = await _context.WorkflowConfigurations
            .Where(w => w.Id == id && w.TenantId == tenantId && !w.IsDeleted)
            .FirstOrDefaultAsync();

        if (originalConfig == null)
        {
            return null;
        }

        var clonedConfig = originalConfig.Clone();
        clonedConfig.CreatedBy = userId;

        _context.WorkflowConfigurations.Add(clonedConfig);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(clonedConfig.Id, tenantId);
    }

    #endregion

    #region Validation & Rules

    /// <summary>
    /// التحقق من صحة الإعداد
    /// </summary>
    public async Task<ValidationResultDto> ValidateConfigurationAsync(CreateWorkflowConfigurationDto dto)
    {
        var result = new ValidationResultDto { IsValid = true };

        // التحقق من الحقول المطلوبة
        if (string.IsNullOrEmpty(dto.WorkflowName))
            result.Errors.Add("WorkflowName is required");

        if (dto.RequestTypeId != Guid.Empty)
            result.Errors.Add("Valid RequestTypeId is required");

        // التحقق من صحة القوانين
        foreach (var rule in dto.EvaluationRules)
        {
            if (!_ruleEngine.ValidateRule(rule))
            {
                result.Errors.Add($"Invalid evaluation rule: {rule.Field} {rule.Operator} {rule.Value}");
            }
        }

        // التحقق من صحة الشروط
        foreach (var condition in dto.StartConditions)
        {
            if (!_ruleEngine.ValidateCondition(condition))
            {
                result.Errors.Add($"Invalid start condition: {condition.Field} {condition.Operator} {condition.Value}");
            }
        }

        foreach (var condition in dto.CompletionConditions)
        {
            if (!_ruleEngine.ValidateCondition(condition))
            {
                result.Errors.Add($"Invalid completion condition: {condition.Field} {condition.Operator} {condition.Value}");
            }
        }

        // التحقق من نوع الطلب
        var requestTypeExists = await _context.RequestTypes
            .AnyAsync(rt => rt.Id == dto.RequestTypeId);

        if (!requestTypeExists)
        {
            result.Errors.Add("RequestType does not exist");
        }

        result.IsValid = !result.Errors.Any();
        return result;
    }

    /// <summary>
    /// تقييم قوانين الـ Workflow
    /// </summary>
    public async Task<RuleEvaluationResultDto> EvaluateWorkflowRulesAsync(int configId, Dictionary<string, object> requestData)
    {
        var config = await _context.WorkflowConfigurations
            .Where(w => w.Id == configId && !w.IsDeleted)
            .FirstOrDefaultAsync();

        if (config == null)
        {
            return new RuleEvaluationResultDto
            {
                IsValid = false,
                Errors = new List<string> { "Configuration not found" }
            };
        }

        var rules = config.GetEvaluationRules<EvaluationRuleDto>() ?? new List<EvaluationRuleDto>();
        return await _ruleEngine.EvaluateRulesAsync(rules, requestData);
    }

    /// <summary>
    /// التحقق من شروط البداية
    /// </summary>
    public async Task<bool> CheckStartConditionsAsync(int configId, Dictionary<string, object> requestData)
    {
        var config = await _context.WorkflowConfigurations
            .Where(w => w.Id == configId && !w.IsDeleted)
            .FirstOrDefaultAsync();

        if (config == null)
        {
            return false;
        }

        var startConditions = JsonSerializer.Deserialize<List<ConditionDto>>(config.StartConditions) ?? new List<ConditionDto>();
        return await _ruleEngine.EvaluateConditionsAsync(startConditions, requestData);
    }

    /// <summary>
    /// التحقق من شروط الإكمال
    /// </summary>
    public async Task<bool> CheckCompletionConditionsAsync(int configId, Dictionary<string, object> requestData)
    {
        var config = await _context.WorkflowConfigurations
            .Where(w => w.Id == configId && !w.IsDeleted)
            .FirstOrDefaultAsync();

        if (config == null)
        {
            return false;
        }

        var completionConditions = JsonSerializer.Deserialize<List<ConditionDto>>(config.CompletionConditions) ?? new List<ConditionDto>();
        return await _ruleEngine.EvaluateConditionsAsync(completionConditions, requestData);
    }

    #endregion

    #region Workflow Management

    /// <summary>
    /// الحصول على الإعداد النشط للطلب
    /// </summary>
    public async Task<WorkflowConfigurationDto?> GetActiveConfigurationForRequestAsync(Guid requestTypeId, Dictionary<string, object> requestData, int tenantId)
    {
        var activeConfigs = await _context.WorkflowConfigurations
            .Include(w => w.RequestType)
            .Where(w => w.RequestTypeId == requestTypeId &&
                       w.TenantId == tenantId &&
                       w.IsActive &&
                       !w.IsDeleted &&
                       w.Status == "Published")
            .OrderBy(w => w.Priority)
            .ToListAsync();

        foreach (var config in activeConfigs)
        {
            var startConditions = JsonSerializer.Deserialize<List<ConditionDto>>(config.StartConditions) ?? new List<ConditionDto>();

            if (!startConditions.Any() || await _ruleEngine.EvaluateConditionsAsync(startConditions, requestData))
            {
                return MapToDto(config);
            }
        }

        return null;
    }

    /// <summary>
    /// الحصول على الإعدادات المتوافقة
    /// </summary>
    public async Task<List<WorkflowConfigurationDto>> GetCompatibleConfigurationsAsync(Dictionary<string, object> requestData, int tenantId)
    {
        var allConfigs = await _context.WorkflowConfigurations
            .Include(w => w.RequestType)
            .Where(w => w.TenantId == tenantId && w.IsActive && !w.IsDeleted)
            .ToListAsync();

        var compatibleConfigs = new List<WorkflowConfiguration>();

        foreach (var config in allConfigs)
        {
            var startConditions = JsonSerializer.Deserialize<List<ConditionDto>>(config.StartConditions) ?? new List<ConditionDto>();

            if (!startConditions.Any() || await _ruleEngine.EvaluateConditionsAsync(startConditions, requestData))
            {
                compatibleConfigs.Add(config);
            }
        }

        return compatibleConfigs.Select(MapToDto).ToList();
    }

    /// <summary>
    /// نشر الإعداد
    /// </summary>
    public async Task<bool> PublishConfigurationAsync(int id, int tenantId, string userId)
    {
        var config = await _context.WorkflowConfigurations
            .Where(w => w.Id == id && w.TenantId == tenantId && !w.IsDeleted)
            .FirstOrDefaultAsync();

        if (config == null)
        {
            return false;
        }

        config.Status = "Published";
        config.UpdatedAt = DateTime.UtcNow;
        config.UpdatedBy = userId;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// أرشفة الإعداد
    /// </summary>
    public async Task<bool> ArchiveConfigurationAsync(int id, int tenantId, string userId)
    {
        var config = await _context.WorkflowConfigurations
            .Where(w => w.Id == id && w.TenantId == tenantId && !w.IsDeleted)
            .FirstOrDefaultAsync();

        if (config == null)
        {
            return false;
        }

        config.Status = "Archived";
        config.IsActive = false;
        config.UpdatedAt = DateTime.UtcNow;
        config.UpdatedBy = userId;

        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Statistics & Reporting

    /// <summary>
    /// الحصول على إحصائيات الإعدادات
    /// </summary>
    public async Task<WorkflowStatisticsDto> GetStatisticsAsync(int tenantId)
    {
        var configs = await _context.WorkflowConfigurations
            .Include(w => w.RequestType)
            .Where(w => w.TenantId == tenantId && !w.IsDeleted)
            .ToListAsync();

        return new WorkflowStatisticsDto
        {
            TotalConfigurations = configs.Count,
            ActiveConfigurations = configs.Count(c => c.IsActive),
            DraftConfigurations = configs.Count(c => c.Status == "Draft"),
            ArchivedConfigurations = configs.Count(c => c.Status == "Archived"),
            ConfigurationsByRequestType = configs
                .GroupBy(c => c.RequestType.Name)
                .ToDictionary(g => g.Key, g => g.Count()),
            ConfigurationsByPriority = configs
                .GroupBy(c => c.Priority)
                .ToDictionary(g => $"Priority {g.Key}", g => g.Count()),
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <summary>
    /// البحث في الإعدادات
    /// </summary>
    public async Task<List<WorkflowConfigurationDto>> SearchConfigurationsAsync(string searchTerm, int tenantId)
    {
        var configs = await _context.WorkflowConfigurations
            .Include(w => w.RequestType)
            .Where(w => w.TenantId == tenantId &&
                       !w.IsDeleted &&
                       (w.WorkflowName.Contains(searchTerm) ||
                        w.Description.Contains(searchTerm) ||
                        w.RequestType.Name.Contains(searchTerm)))
            .OrderBy(w => w.WorkflowName)
            .ToListAsync();

        return configs.Select(MapToDto).ToList();
    }

    /// <summary>
    /// الحصول على تاريخ الإعداد
    /// </summary>
    public async Task<List<WorkflowConfigurationDto>> GetConfigurationHistoryAsync(int baseConfigId, int tenantId)
    {
        // في المستقبل يمكن تطبيق نظام versioning أكثر تقدماً
        var config = await _context.WorkflowConfigurations
            .Include(w => w.RequestType)
            .Where(w => w.Id == baseConfigId && w.TenantId == tenantId && !w.IsDeleted)
            .FirstOrDefaultAsync();

        return config != null ? new List<WorkflowConfigurationDto> { MapToDto(config) } : new List<WorkflowConfigurationDto>();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// تحويل Entity إلى DTO
    /// </summary>
    private WorkflowConfigurationDto MapToDto(WorkflowConfiguration config)
    {
        return new WorkflowConfigurationDto
        {
            Id = config.Id,
            TenantId = config.TenantId,
            WorkflowName = config.WorkflowName,
            Description = config.Description,
            RequestTypeId = config.RequestTypeId,
            RequestTypeName = config.RequestType?.Name ?? "",
            WorkflowDefinition = config.GetWorkflowDefinition<object>(),
            EvaluationRules = config.GetEvaluationRules<EvaluationRuleDto>() ?? new List<EvaluationRuleDto>(),
            EscalationSettings = config.GetEscalationSettings<EscalationSettingsDto>(),
            NotificationSettings = JsonSerializer.Deserialize<NotificationSettingsDto>(config.NotificationSettings),
            StartConditions = JsonSerializer.Deserialize<List<ConditionDto>>(config.StartConditions) ?? new List<ConditionDto>(),
            CompletionConditions = JsonSerializer.Deserialize<List<ConditionDto>>(config.CompletionConditions) ?? new List<ConditionDto>(),
            DefaultData = JsonSerializer.Deserialize<object>(config.DefaultData),
            Priority = config.Priority,
            IsActive = config.IsActive,
            RequiresManualApproval = config.RequiresManualApproval,
            SupportsParallelApproval = config.SupportsParallelApproval,
            MaxExecutionTimeHours = config.MaxExecutionTimeHours,
            MaxRetryCount = config.MaxRetryCount,
            Status = config.Status,
            Version = config.Version,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt,
            CreatedBy = config.CreatedBy,
            UpdatedBy = config.UpdatedBy
        };
    }

    #endregion
}