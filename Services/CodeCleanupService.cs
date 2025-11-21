using ActivationCodeApi.Data;

namespace ActivationCodeApi.Services;

public class CodeCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CodeCleanupService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public CodeCleanupService(IServiceProvider serviceProvider, ILogger<CodeCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Code Cleanup Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredCodes();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up expired codes");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task CleanupExpiredCodes()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LiteDbContext>();

        var now = DateTime.UtcNow;
        var expiredCodes = dbContext.ActivationCodes
            .Find(c => c.IsUsed && c.ExpiresAt != null && c.ExpiresAt < now)
            .ToList();

        if (expiredCodes.Any())
        {
            foreach (var code in expiredCodes)
            {
                dbContext.ActivationCodes.Delete(code.Id);
            }
            
            _logger.LogInformation($"Deleted {expiredCodes.Count} expired activation codes");
        }
        
        await Task.CompletedTask;
    }
}
