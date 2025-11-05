using ApprovalSystem.Models.DTOs;
using ApprovalSystem.Services.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ApprovalSystem.API.Controllers;

/// <summary>
/// Controller لإدارة إعدادات الـ Workflow الديناميكية
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConfigurationsController : ControllerBase
{
    private readonly IConfigurationService _configurationService;
    private readonly IRuleEngine _ruleEngine;

    public ConfigurationsController(IConfigurationService configurationService, IRuleEngine ruleEngine)
    {
        _configurationService = configurationService;
        _ruleEngine = ruleEngine;
    }

    #region CRUD Operations

    /// <summary>
    /// الحصول على جميع إعدادات المستأجر
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<WorkflowConfigurationDto>>> GetConfigurations()
    {
        var tenantId = GetTenantId();
        var configurations = await _configurationService.GetByTenantAsync(tenantId);
        return Ok(configurations);
    }

    /// <summary>
    /// الحصول على إعداد محدد بالمعرف
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<WorkflowConfigurationDto>> GetConfiguration(int id)
    {
        var tenantId = GetTenantId();
        var configuration = await _configurationService.GetByIdAsync(id, tenantId);

        if (configuration == null)
        {
            return NotFound($"Configuration with ID {id} not found");
        }

        return Ok(configuration);
    }

    /// <summary>
    /// الحصول على إعدادات نوع طلب محدد
    /// </summary>
    [HttpGet("request-type/{requestTypeId}")]
    public async Task<ActionResult<List<WorkflowConfigurationDto>>> GetConfigurationsByRequestType(int requestTypeId)
    {
        var tenantId = GetTenantId();
        var configurations = await _configurationService.GetByRequestTypeAsync(requestTypeId, tenantId);
        return Ok(configurations);
    }

    /// <summary>
    /// إنشاء إعداد جديد
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<WorkflowConfigurationDto>> CreateConfiguration([FromBody] CreateWorkflowConfigurationDto dto)
    {
        try
        {
            var tenantId = GetTenantId();
            var userId = GetUserId();

            var configuration = await _configurationService.CreateAsync(dto, tenantId, userId);
            return CreatedAtAction(nameof(GetConfiguration), new { id = configuration.Id }, configuration);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while creating the configuration", details = ex.Message });
        }
    }

    /// <summary>
    /// تحديث إعداد موجود
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<WorkflowConfigurationDto>> UpdateConfiguration(int id, [FromBody] UpdateWorkflowConfigurationDto dto)
    {
        try
        {
            var tenantId = GetTenantId();
            var userId = GetUserId();

            var configuration = await _configurationService.UpdateAsync(id, dto, tenantId, userId);

            if (configuration == null)
            {
                return NotFound($"Configuration with ID {id} not found");
            }

            return Ok(configuration);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while updating the configuration", details = ex.Message });
        }
    }

    /// <summary>
    /// حذف إعداد
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteConfiguration(int id)
    {
        try
        {
            var tenantId = GetTenantId();
            var userId = GetUserId();

            var result = await _configurationService.DeleteAsync(id, tenantId, userId);

            if (!result)
            {
                return NotFound($"Configuration with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while deleting the configuration", details = ex.Message });
        }
    }

    /// <summary>
    /// استنساخ إعداد موجود
    /// </summary>
    [HttpPost("{id}/clone")]
    public async Task<ActionResult<WorkflowConfigurationDto>> CloneConfiguration(int id)
    {
        try
        {
            var tenantId = GetTenantId();
            var userId = GetUserId();

            var clonedConfiguration = await _configurationService.CloneAsync(id, tenantId, userId);

            if (clonedConfiguration == null)
            {
                return NotFound($"Configuration with ID {id} not found");
            }

            return CreatedAtAction(nameof(GetConfiguration), new { id = clonedConfiguration.Id }, clonedConfiguration);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while cloning the configuration", details = ex.Message });
        }
    }

    #endregion

    #region Workflow Management

    /// <summary>
    /// تفعيل إعداد
    /// </summary>
    [HttpPost("{id}/activate")]
    public async Task<ActionResult> ActivateConfiguration(int id)
    {
        try
        {
            var tenantId = GetTenantId();
            var userId = GetUserId();

            var result = await _configurationService.ActivateAsync(id, tenantId, userId);

            if (!result)
            {
                return NotFound($"Configuration with ID {id} not found");
            }

            return Ok(new { message = "Configuration activated successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while activating the configuration", details = ex.Message });
        }
    }

    /// <summary>
    /// إلغاء تفعيل إعداد
    /// </summary>
    [HttpPost("{id}/deactivate")]
    public async Task<ActionResult> DeactivateConfiguration(int id)
    {
        try
        {
            var tenantId = GetTenantId();
            var userId = GetUserId();

            var result = await _configurationService.DeactivateAsync(id, tenantId, userId);

            if (!result)
            {
                return NotFound($"Configuration with ID {id} not found");
            }

            return Ok(new { message = "Configuration deactivated successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while deactivating the configuration", details = ex.Message });
        }
    }

    /// <summary>
    /// نشر إعداد
    /// </summary>
    [HttpPost("{id}/publish")]
    public async Task<ActionResult> PublishConfiguration(int id)
    {
        try
        {
            var tenantId = GetTenantId();
            var userId = GetUserId();

            var result = await _configurationService.PublishConfigurationAsync(id, tenantId, userId);

            if (!result)
            {
                return NotFound($"Configuration with ID {id} not found");
            }

            return Ok(new { message = "Configuration published successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while publishing the configuration", details = ex.Message });
        }
    }

    /// <summary>
    /// أرشفة إعداد
    /// </summary>
    [HttpPost("{id}/archive")]
    public async Task<ActionResult> ArchiveConfiguration(int id)
    {
        try
        {
            var tenantId = GetTenantId();
            var userId = GetUserId();

            var result = await _configurationService.ArchiveConfigurationAsync(id, tenantId, userId);

            if (!result)
            {
                return NotFound($"Configuration with ID {id} not found");
            }

            return Ok(new { message = "Configuration archived successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while archiving the configuration", details = ex.Message });
        }
    }

    #endregion

    #region Validation & Evaluation

    /// <summary>
    /// التحقق من صحة إعداد قبل إنشاؤه
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<ValidationResultDto>> ValidateConfiguration([FromBody] CreateWorkflowConfigurationDto dto)
    {
        try
        {
            var result = await _configurationService.ValidateConfigurationAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while validating the configuration", details = ex.Message });
        }
    }

    /// <summary>
    /// تقييم قوانين إعداد محدد ضد بيانات طلب
    /// </summary>
    [HttpPost("{id}/evaluate")]
    public async Task<ActionResult<RuleEvaluationResultDto>> EvaluateWorkflowRules(int id, [FromBody] Dictionary<string, object> requestData)
    {
        try
        {
            var result = await _configurationService.EvaluateWorkflowRulesAsync(id, requestData);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while evaluating the workflow rules", details = ex.Message });
        }
    }

    /// <summary>
    /// التحقق من شروط بداية الـ Workflow
    /// </summary>
    [HttpPost("{id}/check-start-conditions")]
    public async Task<ActionResult<bool>> CheckStartConditions(int id, [FromBody] Dictionary<string, object> requestData)
    {
        try
        {
            var result = await _configurationService.CheckStartConditionsAsync(id, requestData);
            return Ok(new { canStart = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while checking start conditions", details = ex.Message });
        }
    }

    /// <summary>
    /// التحقق من شروط إكمال الـ Workflow
    /// </summary>
    [HttpPost("{id}/check-completion-conditions")]
    public async Task<ActionResult<bool>> CheckCompletionConditions(int id, [FromBody] Dictionary<string, object> requestData)
    {
        try
        {
            var result = await _configurationService.CheckCompletionConditionsAsync(id, requestData);
            return Ok(new { canComplete = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while checking completion conditions", details = ex.Message });
        }
    }

    /// <summary>
    /// الحصول على أفضل إعداد للطلب
    /// </summary>
    [HttpPost("request-type/{requestTypeId}/best-match")]
    public async Task<ActionResult<WorkflowConfigurationDto>> GetBestConfigurationForRequest(int requestTypeId, [FromBody] Dictionary<string, object> requestData)
    {
        try
        {
            var tenantId = GetTenantId();
            var configuration = await _configurationService.GetActiveConfigurationForRequestAsync(requestTypeId, requestData, tenantId);

            if (configuration == null)
            {
                return NotFound($"No suitable configuration found for request type {requestTypeId}");
            }

            return Ok(configuration);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while finding the best configuration", details = ex.Message });
        }
    }

    /// <summary>
    /// الحصول على جميع الإعدادات المتوافقة مع الطلب
    /// </summary>
    [HttpPost("compatible")]
    public async Task<ActionResult<List<WorkflowConfigurationDto>>> GetCompatibleConfigurations([FromBody] Dictionary<string, object> requestData)
    {
        try
        {
            var tenantId = GetTenantId();
            var configurations = await _configurationService.GetCompatibleConfigurationsAsync(requestData, tenantId);
            return Ok(configurations);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while finding compatible configurations", details = ex.Message });
        }
    }

    #endregion

    #region Search & Statistics

    /// <summary>
    /// البحث في الإعدادات
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<WorkflowConfigurationDto>>> SearchConfigurations([FromQuery] string searchTerm)
    {
        try
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return BadRequest(new { error = "Search term is required" });
            }

            var tenantId = GetTenantId();
            var configurations = await _configurationService.SearchConfigurationsAsync(searchTerm, tenantId);
            return Ok(configurations);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while searching configurations", details = ex.Message });
        }
    }

    /// <summary>
    /// الحصول على إحصائيات الإعدادات
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<WorkflowStatisticsDto>> GetStatistics()
    {
        try
        {
            var tenantId = GetTenantId();
            var statistics = await _configurationService.GetStatisticsAsync(tenantId);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while getting statistics", details = ex.Message });
        }
    }

    /// <summary>
    /// الحصول على تاريخ إعداد محدد
    /// </summary>
    [HttpGet("{id}/history")]
    public async Task<ActionResult<List<WorkflowConfigurationDto>>> GetConfigurationHistory(int id)
    {
        try
        {
            var tenantId = GetTenantId();
            var history = await _configurationService.GetConfigurationHistoryAsync(id, tenantId);
            return Ok(history);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while getting configuration history", details = ex.Message });
        }
    }

    #endregion

    #region Rule Engine Info

    /// <summary>
    /// الحصول على العمليات المدعومة في Rule Engine
    /// </summary>
    [HttpGet("supported-operators")]
    public ActionResult<List<string>> GetSupportedOperators()
    {
        try
        {
            var operators = _ruleEngine.GetSupportedOperators();
            return Ok(operators);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while getting supported operators", details = ex.Message });
        }
    }

    /// <summary>
    /// الحصول على الحقول المدعومة في Rule Engine
    /// </summary>
    [HttpGet("supported-fields")]
    public ActionResult<List<string>> GetSupportedFields()
    {
        try
        {
            var fields = _ruleEngine.GetSupportedFields();
            return Ok(fields);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while getting supported fields", details = ex.Message });
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// الحصول على معرف المستأجر من Claims
    /// </summary>
    private int GetTenantId()
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        if (int.TryParse(tenantIdClaim, out var tenantId))
        {
            return tenantId;
        }
        throw new UnauthorizedAccessException("Tenant ID not found in user claims");
    }

    /// <summary>
    /// الحصول على معرف المستخدم من Claims
    /// </summary>
    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("User ID not found in user claims");
    }

    #endregion
}