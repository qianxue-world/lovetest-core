using ActivationCodeApi.Data;
using ActivationCodeApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace ActivationCodeApi.Services;

public class AdminSetupService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminSetupService> _logger;

    public AdminSetupService(AppDbContext context, ILogger<AdminSetupService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task InitializeAdminAccountAsync()
    {
        // Check if admin account already exists
        var adminExists = await _context.AdminUsers.AnyAsync();
        
        if (adminExists)
        {
            _logger.LogInformation("Admin account already exists. Skipping setup.");
            return;
        }

        // Create default admin account with username: admin, password: admin
        var username = "admin";
        var password = "admin";
        var passwordHash = HashPassword(password);

        var adminUser = new AdminUser
        {
            Username = username,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

        _context.AdminUsers.Add(adminUser);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Admin account created with default credentials (username: admin, password: admin)");
    }

    public async Task<bool> ChangePasswordAsync(string username, string oldPassword, string newPassword)
    {
        var admin = await _context.AdminUsers
            .FirstOrDefaultAsync(a => a.Username == username);

        if (admin == null)
        {
            return false;
        }

        var oldPasswordHash = HashPassword(oldPassword);
        if (admin.PasswordHash != oldPasswordHash)
        {
            return false;
        }

        admin.PasswordHash = HashPassword(newPassword);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Password changed for user: {username}");
        return true;
    }

    public async Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        var admin = await _context.AdminUsers
            .FirstOrDefaultAsync(a => a.Username == username);

        if (admin == null)
        {
            return false;
        }

        var passwordHash = HashPassword(password);
        return admin.PasswordHash == passwordHash;
    }

    public string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
