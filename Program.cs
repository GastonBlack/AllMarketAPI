using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using AllMarket.Constants.RateLimitPolicyNames;
using AllMarket.Features.Admin.Categories.Services;
using AllMarket.Features.Admin.Orders.Services;
using AllMarket.Features.Admin.Products.Services;
using AllMarket.Features.Admin.Users.Services;
using AllMarket.Features.Auth.Security;
using AllMarket.Features.Auth.Services;
using AllMarket.Features.Categories.Services;
using AllMarket.Features.Orders.Services;
using AllMarket.Features.Payments.Services;
using AllMarket.Features.Products.Services;
using AllMarket.Features.Users.Services;
using AllMarket.Infrastructure.BackgroundServices;
using AllMarket.Infrastructure.Caching;
using AllMarket.Infrastructure.Data;
using AllMarket.Infrastructure.Data.Seed;
using AllMarket.Infrastructure.Images;
using AllMarket.Infrastructure.Middleware;
using AllMarket.Infrastructure.Responses;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
const string FrontendCorsPolicy = "FrontendCorsPolicy";

static string GetClientIp(HttpContext httpContext)
{
    return $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
}

static string GetUserOrIp(HttpContext httpContext)
{
    var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    return userId == null ? GetClientIp(httpContext) : $"user:{userId}";
}

// //////////////////////////////////////////
// Database
// //////////////////////////////////////////
builder.Services.AddDbContext<AllMarketDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis")
        ?? "localhost:6379";
    options.InstanceName = "AllMarket:";
});
builder.Services.AddScoped<ICacheService, RedisCacheService>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = null;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// //////////////////////////////////////////
// Feature Services
// //////////////////////////////////////////
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductServices>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IAdminProductService, AdminProductService>();
builder.Services.AddScoped<IAdminCategoryService, AdminCategoryService>();
builder.Services.AddScoped<IAdminOrderService, AdminOrderService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddHttpClient<IImageStorageService, CloudinaryImageStorageService>();
builder.Services.AddHostedService<OrderExpirationBackgroundService>();

// //////////////////////////////////////////
// Authentication
// //////////////////////////////////////////
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("JWT secret key is not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("JWT issuer is not configured.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("JWT audience is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["access_token"];
                return Task.CompletedTask;
            }
        };
    });

// //////////////////////////////////////////
// CORS
// //////////////////////////////////////////
var allowedFrontendOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .GetChildren()
    .Select(origin => origin.Value)
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Cast<string>()
    .ToArray();

if (allowedFrontendOrigins.Length == 0)
{
    allowedFrontendOrigins = ["http://localhost:3000"];
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(allowedFrontendOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// //////////////////////////////////////////
// Rate Limiting
// //////////////////////////////////////////
builder.Services.AddRateLimiter(options =>
{

    // //////////////////////////////////////////
    // Global: 100 per minute.
    // Auth: 5 per minute.
    // Profile update: 10 per minute.
    // Password change: 5 per 15 minutes.
    // Order creation: 10 per minute.
    // Payment checkout: 5 per minute.
    // Refund: 3 per minute.
    // Product creation: 10 per minute.
    // //////////////////////////////////////////
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var response = new ErrorResponse
        {
            Error = "rate_limit_exceeded",
            Message = "Too many requests. Please try again later.",
            StatusCode = StatusCodes.Status429TooManyRequests,
            TraceId = context.HttpContext.TraceIdentifier
        };

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: GetClientIp(httpContext),
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = 0
            }));

    // --------------------------------
    // Individual endpoint policies.
    // --------------------------------
    options.AddPolicy<string>(RateLimitPolicies.Auth, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientIp(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy<string>(RateLimitPolicies.ProfileUpdate, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetUserOrIp(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy<string>(RateLimitPolicies.PasswordChange, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetUserOrIp(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0
            }));

    options.AddPolicy<string>(RateLimitPolicies.OrderCreation, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetUserOrIp(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy<string>(RateLimitPolicies.PaymentCheckout, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetUserOrIp(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy<string>(RateLimitPolicies.Refund, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetUserOrIp(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy<string>(RateLimitPolicies.ProductCreation, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetUserOrIp(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// //////////////////////////////////////////
// API Services
// //////////////////////////////////////////
builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddOpenApi();

var app = builder.Build();

// //////////////////////////////////////////
// Development
// //////////////////////////////////////////
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await DatabaseSeeder.SeedAsync(app.Services);
}
else
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AllMarketDbContext>();
    var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

    await db.Database.MigrateAsync();
    await CategorySeeder.SeedAsync(db);
    await ProductSeeder.SeedAsync(db);
    await cache.RemoveAsync(CacheKeys.Categories);
    await cache.InvalidateProductsAsync();
}

// //////////////////////////////////////////
// Middleware
// //////////////////////////////////////////
app.UseForwardedHeaders();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(FrontendCorsPolicy);
app.UseMiddleware<CsrfProtectionMiddleware>();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

// //////////////////////////////////////////
// Endpoints
// //////////////////////////////////////////
app.MapControllers();

app.Run();
