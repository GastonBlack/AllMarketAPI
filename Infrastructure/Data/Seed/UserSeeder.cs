using AllMarket.Constants.UserRoles;
using AllMarket.Features.Users.Models;
using Microsoft.EntityFrameworkCore;

namespace AllMarket.Infrastructure.Data.Seed;

public static class UserSeeder
{
    public static async Task SeedAsync(AllMarketDbContext db)
    {
        var seedUsers = GetSeedUsers();
        var seedEmails = seedUsers.Select(user => user.Email).ToArray();

        var existingEmails = await db.Users
            .Where(user => seedEmails.Contains(user.Email))
            .Select(user => user.Email)
            .ToListAsync();

        var usersToCreate = seedUsers
            .Where(user => !existingEmails.Contains(user.Email))
            .ToList();

        if (usersToCreate.Count == 0) return;

        await db.Users.AddRangeAsync(usersToCreate);
        await db.SaveChangesAsync();
    }

    private static List<User> GetSeedUsers()
    {
        return new List<User>
        {
            new User
            {
                FullName = "Admin AllMarket",
                Email = "admin@allmarket.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Address = "AllMarket Main Address",
                Phone = "111111111",
                Rol = Roles.Admin
            },
            new User
            {
                FullName = "John Parker",
                Email = "john.parker@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!"),
                Address = "742 Evergreen Avenue",
                Phone = "222222222",
                Rol = Roles.User
            },
            new User
            {
                FullName = "Mary Gomez",
                Email = "mary.gomez@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!"),
                Address = "123 Main Street",
                Phone = "333333333",
                Rol = Roles.User
            }
        };
    }
}
