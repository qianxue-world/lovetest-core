using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ActivationCodeApi.Data;
using ActivationCodeApi.DTOs;

namespace ActivationCodeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActivationController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ActivationController> _logger;

    public ActivationController(AppDbContext context, ILogger<ActivationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("validate")]
    public async Task<ActionResult<ValidateCodeResponse>> ValidateCode([FromBody] ValidateCodeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new ValidateCodeResponse
            {
                IsValid = false,
                Message = "Activation code is required"
            });
        }

        var activationCode = await _context.ActivationCodes
            .FirstOrDefaultAsync(c => c.Code == request.Code);

        if (activationCode == null)
        {
            _logger.LogWarning($"Activation code not found: {request.Code}");
            return NotFound(new ValidateCodeResponse
            {
                IsValid = false,
                Message = "Activation code not found"
            });
        }

        // 增加验证次数
        activationCode.ValidationCount++;
        activationCode.LastValidatedAt = DateTime.UtcNow;

        // 检查验证次数是否超过限制
        if (activationCode.ValidationCount > 3)
        {
            await _context.SaveChangesAsync();
            
            _logger.LogWarning($"Activation code validation limit exceeded: {request.Code}, count: {activationCode.ValidationCount}");
            return BadRequest(new ValidateCodeResponse
            {
                IsValid = false,
                Message = "Activation code has been invalidated due to excessive validation attempts",
                ValidationCount = activationCode.ValidationCount,
                RemainingValidations = 0
            });
        }

        if (activationCode.IsUsed)
        {
            if (activationCode.ExpiresAt.HasValue && activationCode.ExpiresAt.Value > DateTime.UtcNow)
            {
                await _context.SaveChangesAsync();
                
                return Ok(new ValidateCodeResponse
                {
                    IsValid = true,
                    Message = "Activation code is valid",
                    ExpiresAt = activationCode.ExpiresAt,
                    ValidationCount = activationCode.ValidationCount,
                    RemainingValidations = Math.Max(0, 3 - activationCode.ValidationCount)
                });
            }
            else
            {
                await _context.SaveChangesAsync();
                
                return BadRequest(new ValidateCodeResponse
                {
                    IsValid = false,
                    Message = "Activation code has expired",
                    ValidationCount = activationCode.ValidationCount,
                    RemainingValidations = Math.Max(0, 3 - activationCode.ValidationCount)
                });
            }
        }

        // 首次激活
        activationCode.IsUsed = true;
        activationCode.ActivatedAt = DateTime.UtcNow;
        activationCode.ExpiresAt = DateTime.UtcNow.AddDays(7);

        await _context.SaveChangesAsync();

        _logger.LogInformation($"Activation code activated: {request.Code}, expires at: {activationCode.ExpiresAt}, validation count: {activationCode.ValidationCount}");

        return Ok(new ValidateCodeResponse
        {
            IsValid = true,
            Message = "Activation code successfully activated",
            ExpiresAt = activationCode.ExpiresAt,
            ValidationCount = activationCode.ValidationCount,
            RemainingValidations = Math.Max(0, 3 - activationCode.ValidationCount)
        });
    }
}
