using Microsoft.AspNetCore.Mvc;
using ActivationCodeApi.Data;

namespace ActivationCodeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly LiteDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(LiteDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Liveness probe - 检查应用是否存活
    /// 如果此端点失败，Kubernetes会重启Pod
    /// </summary>
    [HttpGet("live")]
    public IActionResult GetLiveness()
    {
        // 简单检查：应用是否能响应请求
        return Ok(new
        {
            status = "alive",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Readiness probe - 检查应用是否准备好接收流量
    /// 如果此端点失败，Kubernetes会将Pod从Service中移除
    /// </summary>
    [HttpGet("ready")]
    public async Task<IActionResult> GetReadiness()
    {
        try
        {
            // 检查数据库连接
            var canConnect = _context.CanConnect();
            
            if (!canConnect)
            {
                _logger.LogWarning("Readiness check failed: Cannot connect to database");
                return StatusCode(503, new
                {
                    status = "not_ready",
                    reason = "database_unavailable",
                    timestamp = DateTime.UtcNow
                });
            }

            // 可选：检查数据库是否有数据（确保初始化完成）
            var adminExists = _context.AdminUsers.Count() > 0;
            
            if (!adminExists)
            {
                _logger.LogWarning("Readiness check failed: Admin user not initialized");
                return StatusCode(503, new
                {
                    status = "not_ready",
                    reason = "database_not_initialized",
                    timestamp = DateTime.UtcNow
                });
            }

            return Ok(new
            {
                status = "ready",
                database = "connected",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed with exception");
            return StatusCode(503, new
            {
                status = "not_ready",
                reason = "exception",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Startup probe - 检查应用是否已启动完成
    /// 用于慢启动的应用，避免在启动期间被liveness probe杀死
    /// </summary>
    [HttpGet("startup")]
    public async Task<IActionResult> GetStartup()
    {
        try
        {
            // 检查数据库是否已初始化
            var canConnect = await _context.Database.CanConnectAsync();
            
            if (!canConnect)
            {
                return StatusCode(503, new
                {
                    status = "starting",
                    reason = "database_unavailable",
                    timestamp = DateTime.UtcNow
                });
            }

            // 检查关键表是否存在
            var adminExists = _context.AdminUsers.Count() > 0;
            
            return Ok(new
            {
                status = "started",
                database = "initialized",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Startup check failed with exception");
            return StatusCode(503, new
            {
                status = "starting",
                reason = "exception",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// 综合健康检查 - 返回详细的健康状态
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        var health = new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            checks = new Dictionary<string, object>()
        };

        try
        {
            // 检查数据库连接
            var dbConnected = _context.CanConnect();
            health.checks["database"] = new
            {
                status = dbConnected ? "healthy" : "unhealthy",
                message = dbConnected ? "Connected" : "Cannot connect"
            };

            // 检查数据库初始化
            if (dbConnected)
            {
                var adminCount = _context.AdminUsers.Count();
                var codeCount = _context.ActivationCodes.Count();
                
                health.checks["database_data"] = new
                {
                    status = "healthy",
                    adminUsers = adminCount,
                    activationCodes = codeCount
                };
            }

            // 检查应用版本
            var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";
            health.checks["version"] = new
            {
                status = "healthy",
                version = version
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(503, new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }
}
