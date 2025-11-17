using ActivationCodeApi.Data;
using ActivationCodeApi.Models;

namespace ActivationCodeApi;

public static class SeedData
{
    public static void Initialize(AppDbContext context)
    {
        if (context.ActivationCodes.Any())
        {
            return; // Database already seeded
        }

        var codes = new[]
        {
            new ActivationCode { Code = "TEST-CODE-001", IsUsed = false },
            new ActivationCode { Code = "TEST-CODE-002", IsUsed = false },
            new ActivationCode { Code = "TEST-CODE-003", IsUsed = false },
            new ActivationCode { Code = "DEMO-CODE-123", IsUsed = false },
            new ActivationCode { Code = "DEMO-CODE-456", IsUsed = false }
        };

        context.ActivationCodes.AddRange(codes);
        context.SaveChanges();
    }
}
