using LineNotify.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ===========================================
// 服務配置
// ===========================================

// 加入控制器服務
builder.Services.AddControllers();

// 加入 OpenAPI 支援（.NET 10 內建）
builder.Services.AddOpenApi();

// 配置 MySQL 資料庫（使用 MySql.EntityFrameworkCore）
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySQL(connectionString);
});

// 配置 JWT 認證
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("JWT Secret not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "LineNotify";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "LineNotifyUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero // 不允許時間偏移
    };
});

builder.Services.AddAuthorization();

// 加入 CORS 支援（開發環境）
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 加入健康檢查
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

var app = builder.Build();

// ===========================================
// 中介軟體配置
// ===========================================

// 開發環境配置
if (app.Environment.IsDevelopment())
{
    // 啟用 OpenAPI（Swagger UI 在 /openapi/v1.json）
    app.MapOpenApi();

    // 開發環境自動執行資料庫遷移
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// 健康檢查端點
app.MapHealthChecks("/health");

// 控制器路由
app.MapControllers();

// API 資訊端點
app.MapGet("/api/info", () => new
{
    Name = "Line 通知服務",
    Version = "1.0.0",
    Status = "Running",
    Timestamp = DateTime.UtcNow
})
.WithName("GetApiInfo")
.WithTags("System");

app.Run();
