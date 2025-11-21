using ActivationCodeApi.Data;
using ActivationCodeApi.Services;
using ActivationCodeApi.Middleware;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add LiteDB database
var connectionString = "Filename=activationcodes.db;Connection=shared";
builder.Services.AddSingleton(new LiteDbContext(connectionString));

// Add custom services
builder.Services.AddScoped<AdminSetupService>();
builder.Services.AddSingleton<TokenService>();

// Add background service for cleanup
builder.Services.AddHostedService<CodeCleanupService>();

// Add rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

var app = builder.Build();

// Initialize database and admin account on startup
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var dbContext = app.Services.GetRequiredService<LiteDbContext>();
        
        // Check if database is new
        var isNewDatabase = !File.Exists("activationcodes.db");
        
        if (isNewDatabase)
        {
            logger.LogInformation("=== NEW DATABASE CREATED ===");
        }
        else
        {
            logger.LogInformation("Database initialized successfully");
        }
        
        // Initialize admin account (one-time setup with default credentials)
        using (var scope = app.Services.CreateScope())
        {
            var adminSetupService = scope.ServiceProvider.GetRequiredService<AdminSetupService>();
            await adminSetupService.InitializeAdminAccountAsync();
        }
        
        // Seed initial activation codes if needed
        ActivationCodeApi.SeedData.Initialize(dbContext);
        logger.LogInformation("Database seeding completed");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable CORS
app.UseCors("AllowAll");

app.UseIpRateLimiting();

// Add API key authentication middleware
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
