using AllMarket.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AllMarketAPI.Tests;

public sealed class TestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private TestDatabase(
        SqliteConnection connection,
        string connectionString,
        AllMarketDbContext db)
    {
        _connection = connection;
        ConnectionString = connectionString;
        Db = db;
    }

    public AllMarketDbContext Db { get; }
    public string ConnectionString { get; }

    public static async Task<TestDatabase> CreateAsync()
    {
        var connectionString =
            $"Data Source=AllMarketTests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AllMarketDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new AllMarketDbContext(options);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE "Users" (
                "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                "FullName" TEXT NOT NULL,
                "PasswordHash" TEXT NOT NULL,
                "Email" TEXT NOT NULL,
                "Address" TEXT NOT NULL,
                "Phone" TEXT NULL,
                "Rol" TEXT NOT NULL DEFAULT 'User',
                "IsActive" INTEGER NOT NULL DEFAULT 1,
                "CreatedAt" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                "DisabledAt" TEXT NULL
            );

            CREATE TABLE "Categories" (
                "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NOT NULL
            );

            CREATE TABLE "Products" (
                "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NOT NULL,
                "Description" TEXT NOT NULL,
                "Price" TEXT NOT NULL,
                "Stock" INTEGER NOT NULL DEFAULT 0,
                "ReservedStock" INTEGER NOT NULL DEFAULT 0,
                "TotalSold" INTEGER NOT NULL DEFAULT 0,
                "HasDiscount" INTEGER NOT NULL DEFAULT 0,
                "DiscountPrice" TEXT NULL,
                "IsActive" INTEGER NOT NULL DEFAULT 1,
                "CreatedAt" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                "ImageUrl" TEXT NULL,
                "CategoryId" INTEGER NOT NULL
            );

            CREATE TABLE "Orders" (
                "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                "UserId" INTEGER NOT NULL,
                "Status" TEXT NOT NULL DEFAULT 'Awaiting for payment',
                "TotalPrice" TEXT NOT NULL,
                "StripePaymentIntentId" TEXT NULL,
                "StripeRefundId" TEXT NULL,
                "PreRefundStatus" TEXT NULL,
                "RefundedAt" TEXT NULL,
                "CreatedAt" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                "ReservationExpiresAt" TEXT NULL DEFAULT (datetime('now', '+15 minutes'))
            );

            CREATE TABLE "OrderItems" (
                "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                "OrderId" INTEGER NOT NULL,
                "ProductId" INTEGER NOT NULL,
                "Quantity" INTEGER NOT NULL,
                "PriceAtPurchase" TEXT NOT NULL
            );

            CREATE TABLE "RefreshTokens" (
                "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                "TokenHash" TEXT NOT NULL,
                "FamilyId" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "ExpiresAt" TEXT NOT NULL,
                "RevokedAt" TEXT NULL,
                "ReplacedByTokenHash" TEXT NULL,
                "UserId" INTEGER NOT NULL
            );

            CREATE UNIQUE INDEX "IX_RefreshTokens_TokenHash"
                ON "RefreshTokens" ("TokenHash");
            """);

        return new TestDatabase(connection, connectionString, db);
    }

    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
