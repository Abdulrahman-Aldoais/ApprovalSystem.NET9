using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ApprovalSystem.Models.Entities;
using ApprovalSystem.Models.ViewModels;
using ApprovalSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace ApprovalSystem.API.Controllers;

/// <summary>
/// Controller للمصادقة وإدارة المستخدمين
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// تسجيل دخول المستخدم
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "بيانات الدخول غير صحيحة", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
        }

        try
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !user.IsActive)
            {
                return Unauthorized(new { message = "بيانات الدخول غير صحيحة أو الحساب غير نشط" });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
            
            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var token = GenerateJwtToken(user, roles);
                
                var userViewModel = new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email!,
                    Name = user.Name,
                    Role = user.Role,
                    Department = user.Department,
                    TenantId = user.TenantId,
                    AvatarUrl = user.AvatarUrl,
                    IsActive = user.IsActive,
                    LastLogin = DateTime.UtcNow
                };

                _logger.LogInformation("User {Email} logged in successfully", user.Email);

                return Ok(new
                {
                    message = "تم تسجيل الدخول بنجاح",
                    token,
                    user = userViewModel,
                    expiresAt = DateTime.UtcNow.AddHours(24)
                });
            }

            if (result.RequiresTwoFactor)
            {
                return Ok(new
                {
                    requiresTwoFactor = true,
                    message = "يتطلب التحقق بخطوتين"
                });
            }

            if (result.IsLockedOut)
            {
                return Locked(new { message = "تم قفل الحساب مؤقتاً بسبب محاولات خاطئة متعددة" });
            }

            return Unauthorized(new { message = "بيانات الدخول غير صحيحة" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Email}", model.Email);
            return StatusCode(500, new { message = "حدث خطأ أثناء تسجيل الدخول" });
        }
    }

    /// <summary>
    /// تسجيل مستخدم جديد
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "بيانات التسجيل غير صحيحة", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
        }

        try
        {
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "البريد الإلكتروني مسجل بالفعل" });
            }

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.Name,
                EmailConfirmed = false,
                Role = "User",
                Department = model.Department,
                Phone = model.Phone,
                TenantId = model.TenantId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                
                _logger.LogInformation("New user {Email} registered successfully", user.Email);
                
                return Ok(new { message = "تم إنشاء الحساب بنجاح. يرجى تفعيل البريد الإلكتروني." });
            }

            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { message = "فشل في إنشاء الحساب", errors });

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for user {Email}", model.Email);
            return StatusCode(500, new { message = "حدث خطأ أثناء التسجيل" });
        }
    }

    /// <summary>
    /// تسجيل خروج المستخدم
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            await _signInManager.SignOutAsync();
            return Ok(new { message = "تم تسجيل الخروج بنجاح" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return Ok(new { message = "تم تسجيل الخروج" });
        }
    }

    /// <summary>
    /// الحصول على بيانات المستخدم الحالي
    /// </summary>
    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "المستخدم غير موجود" });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userViewModel = new UserViewModel
            {
                Id = user.Id,
                Email = user.Email!,
                Name = user.Name,
                Role = user.Role,
                Department = user.Department,
                Phone = user.Phone,
                TenantId = user.TenantId,
                AvatarUrl = user.AvatarUrl,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLogin = user.EmailConfirmed ? DateTime.UtcNow : (DateTime?)null
            };

            return Ok(new { user = userViewModel, roles });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile");
            return StatusCode(500, new { message = "حدث خطأ أثناء جلب بيانات المستخدم" });
        }
    }

    /// <summary>
    /// تحديث بيانات المستخدم
    /// </summary>
    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "بيانات التحديث غير صحيحة", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
        }

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "المستخدم غير موجود" });
            }

            // Update user properties
            user.Name = model.Name;
            user.Department = model.Department;
            user.Phone = model.Phone;
            user.AvatarUrl = model.AvatarUrl;
            user.PreferredLanguage = model.PreferredLanguage;
            user.TimeZone = model.TimeZone;
            user.EmailNotificationsEnabled = model.EmailNotificationsEnabled;
            user.PushNotificationsEnabled = model.PushNotificationsEnabled;
            user.SmsNotificationsEnabled = model.SmsNotificationsEnabled;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} profile updated", user.Email);
                return Ok(new { message = "تم تحديث البيانات بنجاح" });
            }

            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { message = "فشل في تحديث البيانات", errors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            return StatusCode(500, new { message = "حدث خطأ أثناء تحديث البيانات" });
        }
    }

    /// <summary>
    /// تغيير كلمة المرور
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "بيانات تغيير كلمة المرور غير صحيحة", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
        }

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "المستخدم غير موجود" });
            }

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                _logger.LogInformation("Password changed for user {Email}", user.Email);
                return Ok(new { message = "تم تغيير كلمة المرور بنجاح" });
            }

            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { message = "فشل في تغيير كلمة المرور", errors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return StatusCode(500, new { message = "حدث خطأ أثناء تغيير كلمة المرور" });
        }
    }

    /// <summary>
    /// التحقق من صحة الرمز المميز
    /// </summary>
    [Authorize]
    [HttpGet("validate-token")]
    public IActionResult ValidateToken()
    {
        return Ok(new { valid = true, expiresAt = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value });
    }

    /// <summary>
    /// تجديد الرمز المميز
    /// </summary>
    [Authorize]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return Unauthorized();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateJwtToken(user, roles);
            
            return Ok(new
            {
                token,
                expiresAt = DateTime.UtcNow.AddHours(24)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return StatusCode(500, new { message = "حدث خطأ أثناء تجديد الرمز" });
        }
    }

    private string GenerateJwtToken(User user, IList<string> roles)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("TenantId", user.TenantId.ToString()),
            new Claim("IsActive", user.IsActive.ToString())
        };

        // Add roles as claims
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
