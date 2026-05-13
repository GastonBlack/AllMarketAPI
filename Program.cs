using AllMarket.Features.Categories.Services;
using AllMarket.Features.Users.Services;
using AllMarket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// //////////////////////////////////////////
// Database
// //////////////////////////////////////////
builder.Services.AddDbContext<AllMarketDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// //////////////////////////////////////////
// Feature Services
// //////////////////////////////////////////
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

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
}

// //////////////////////////////////////////
// Middleware
// //////////////////////////////////////////
app.UseHttpsRedirection();
app.UseAuthorization();

// //////////////////////////////////////////
// Endpoints
// //////////////////////////////////////////
app.MapControllers();

app.Run();
