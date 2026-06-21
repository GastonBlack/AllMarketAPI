using AllMarket.Constants.OrderStatuses;
using AllMarket.Constants.UserRoles;
using AllMarket.Features.Auth.Models;
using AllMarket.Features.Categories.Models;
using AllMarket.Features.OrderItems.Models;
using AllMarket.Features.Orders.Models;
using AllMarket.Features.Products.Models;
using AllMarket.Features.Users.Models;
using Microsoft.EntityFrameworkCore;

namespace AllMarket.Infrastructure.Data;

public class AllMarketDbContext(DbContextOptions<AllMarketDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUsers(modelBuilder);
        ConfigureCategories(modelBuilder);
        ConfigureProducts(modelBuilder);
        ConfigureOrders(modelBuilder);
        ConfigureOrderItems(modelBuilder);
        ConfigureRefreshTokens(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);

            entity.Property(user => user.FullName)
                .IsRequired()
                .HasMaxLength(120);

            entity.Property(user => user.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(user => user.Email)
                .IsRequired()
                .HasMaxLength(150);

            entity.HasIndex(user => user.Email)
                .IsUnique();

            entity.Property(user => user.Address)
                .IsRequired()
                .HasMaxLength(250);

            entity.Property(user => user.Phone)
                .HasMaxLength(30);

            entity.Property(user => user.Rol)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue(Roles.User);

            entity.Property(user => user.IsActive)
                .HasDefaultValue(true);

            entity.Property(user => user.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasMany(user => user.Orders)
                .WithOne(order => order.User)
                .HasForeignKey(order => order.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.Property(user => user.EmailConfirmed)
                .HasDefaultValue(false);

            entity.Property(user => user.EmailVerificationCodeHash)
                .HasMaxLength(500);

            entity.ToTable("Users", table =>
            {
                table.HasCheckConstraint(
                    "CK_Users_Rol",
                    $"\"Rol\" IN ('{Roles.Admin}', '{Roles.User}')");

                table.HasCheckConstraint(
                    "CK_Users_DisabledAt",
                    "\"DisabledAt\" IS NULL OR \"DisabledAt\" >= \"CreatedAt\"");
            });
        });
    }

    private static void ConfigureCategories(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");

            entity.HasKey(category => category.Id);

            entity.Property(category => category.Name)
                .IsRequired()
                .HasMaxLength(60);

            entity.HasIndex(category => category.Name)
                .IsUnique();
        });
    }

    private static void ConfigureProducts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(product => product.Id);

            entity.Property(product => product.Name)
                .IsRequired()
                .HasMaxLength(120);

            entity.Property(product => product.Description)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(product => product.Price)
                .HasColumnType("decimal(18,2)");

            entity.Property(product => product.Stock)
                .HasDefaultValue(0);

            entity.Property(product => product.ReservedStock)
                .HasDefaultValue(0);

            entity.Property(product => product.TotalSold)
                .HasDefaultValue(0);

            entity.Property(product => product.HasDiscount)
                .HasDefaultValue(false);

            entity.Property(product => product.DiscountPrice)
                .HasColumnType("decimal(18,2)");

            entity.Property(product => product.IsActive)
                .HasDefaultValue(true);

            entity.Property(product => product.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(product => product.ImageUrl)
                .HasMaxLength(500);

            entity.HasOne(product => product.Category)
                .WithMany()
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable("Products", table =>
            {
                table.HasCheckConstraint(
                    "CK_Products_Price",
                    "\"Price\" > 0");

                table.HasCheckConstraint(
                    "CK_Products_Stock",
                    "\"Stock\" >= 0");

                table.HasCheckConstraint(
                    "CK_Products_ReservedStock",
                    "\"ReservedStock\" >= 0 AND \"ReservedStock\" <= \"Stock\"");

                table.HasCheckConstraint(
                    "CK_Products_TotalSold",
                    "\"TotalSold\" >= 0");

                table.HasCheckConstraint(
                    "CK_Products_Discount",
                    "(\"HasDiscount\" = FALSE AND \"DiscountPrice\" IS NULL) OR (\"HasDiscount\" = TRUE AND \"DiscountPrice\" IS NOT NULL AND \"DiscountPrice\" > 0 AND \"DiscountPrice\" < \"Price\")");
            });
        });
    }

    private static void ConfigureOrders(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(order => order.Id);

            entity.Property(order => order.Status)
                .IsRequired()
                .HasMaxLength(40)
                .HasDefaultValue(Statuses.AwaitingPayment);

            entity.Property(order => order.TotalPrice)
                .HasColumnType("decimal(18,2)");

            entity.Property(order => order.StripePaymentIntentId)
                .HasMaxLength(255);

            entity.Property(order => order.StripeRefundId)
                .HasMaxLength(255);

            entity.Property(order => order.PreRefundStatus)
                .HasMaxLength(40);

            entity.Property(order => order.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(order => order.ReservationExpiresAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP + INTERVAL '15 minutes'");

            entity.HasMany(order => order.Items)
                .WithOne(item => item.Order)
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable("Orders", table =>
            {
                table.HasCheckConstraint(
                    "CK_Orders_Status",
                    $"\"Status\" IN ('{Statuses.AwaitingPayment}', '{Statuses.Paid}', '{Statuses.Preparing}', '{Statuses.Shipped}', '{Statuses.Delivered}', '{Statuses.Cancelled}', '{Statuses.Expired}', '{Statuses.Refunding}', '{Statuses.Refunded}')");

                table.HasCheckConstraint(
                    "CK_Orders_TotalPrice",
                    "\"TotalPrice\" >= 0");

                table.HasCheckConstraint(
                    "CK_Orders_PreRefundStatus",
                    $"\"PreRefundStatus\" IS NULL OR \"PreRefundStatus\" IN ('{Statuses.Paid}', '{Statuses.Preparing}')");

                table.HasCheckConstraint(
                    "CK_Orders_ReservationExpiresAt",
                    "\"ReservationExpiresAt\" IS NULL OR \"ReservationExpiresAt\" >= \"CreatedAt\"");
            });
        });
    }

    private static void ConfigureOrderItems(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(item => item.Id);

            entity.Property(item => item.Quantity)
                .IsRequired();

            entity.Property(item => item.PriceAtPurchase)
                .HasColumnType("decimal(18,2)");

            entity.HasOne(item => item.Product)
                .WithMany()
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(item => new { item.OrderId, item.ProductId })
                .IsUnique();

            entity.ToTable("OrderItems", table =>
            {
                table.HasCheckConstraint(
                    "CK_OrderItems_Quantity",
                    "\"Quantity\" > 0");

                table.HasCheckConstraint(
                    "CK_OrderItems_PriceAtPurchase",
                    "\"PriceAtPurchase\" > 0");
            });
        });
    }

    private static void ConfigureRefreshTokens(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(token => token.Id);

            entity.Property(token => token.TokenHash)
                .IsRequired()
                .HasMaxLength(64);

            entity.HasIndex(token => token.TokenHash)
                .IsUnique();

            entity.HasIndex(token => token.FamilyId);

            entity.Property(token => token.ReplacedByTokenHash)
                .HasMaxLength(64);

            entity.HasOne(token => token.User)
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
