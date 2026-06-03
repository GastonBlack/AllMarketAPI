using Microsoft.EntityFrameworkCore;

namespace AllMarket.Infrastructure.Data.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AllMarketDbContext>();

        await db.Database.MigrateAsync();
        await UserSeeder.SeedAsync(db);
        await CategorySeeder.SeedAsync(db);
        await ProductSeeder.SeedAsync(db);
    }
}
