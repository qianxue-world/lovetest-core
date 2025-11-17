using Microsoft.EntityFrameworkCore;
using ActivationCodeApi.Models;

namespace ActivationCodeApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ActivationCode> ActivationCodes { get; set; }
    public DbSet<AdminUser> AdminUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivationCode>()
            .HasIndex(a => a.Code)
            .IsUnique();

        modelBuilder.Entity<AdminUser>()
            .HasIndex(a => a.Username)
            .IsUnique();
    }
}
