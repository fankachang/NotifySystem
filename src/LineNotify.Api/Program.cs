using LineNotify.Api.Configuration;
using LineNotify.Api.Data;
using LineNotify.Api.Middleware;
using LineNotify.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ===========================================
// 配置設定
// ===========================================

// 綁定設定類別
builder.Services.Configure<LineSettings>(builder.Configuration.GetSection(LineSettings.SectionName));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(AppSettings.SectionName));

// ===========================================
// 服務配置
// ===========================================

// 加入控制器服務
builder.Services.AddControllers();

// 加入 Razor Pages 服務
builder.Services.AddRazorPages();

// 加入 OpenAPI 支援（.NET 10 內建）
builder.Services.AddOpenApi();

// 配置 MySQL 資料庫（使用 MySql.EntityFrameworkCore）
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySQL(connectionString);
});

// ===========================================
// 認證與授權
// ===========================================

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

builder.Services.AddAuthorization(options =>
{
    // 定義管理員政策
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("user_type", "admin"));

    // 定義超級管理員政策
    options.AddPolicy("SuperAdminOnly", policy =>
        policy.RequireClaim("user_type", "admin")
              .RequireClaim("is_super_admin", "true"));

    // 定義使用者政策
    options.AddPolicy("UserOnly", policy =>
        policy.RequireClaim("user_type", "user"));

    // 管理員或使用者都可存取
    options.AddPolicy("Authenticated", policy =>
        policy.RequireAuthenticatedUser());
});

// ===========================================
// 註冊服務
// ===========================================

// JWT 服務
builder.Services.AddScoped<IJwtService, JwtService>();

// Line 認證服務
builder.Services.AddScoped<ILineAuthService, LineAuthService>();

// 登入記錄服務
builder.Services.AddScoped<ILoginLogService, LoginLogService>();

// 管理員服務
builder.Services.AddScoped<IAdminService, AdminService>();

// 群組服務
builder.Services.AddScoped<IGroupService, GroupService>();

// 訂閱服務
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

// 訊息類型服務
builder.Services.AddScoped<IMessageTypeService, MessageTypeService>();

// 使用者管理服務
builder.Services.AddScoped<IUserService, UserService>();

// 報表服務
builder.Services.AddScoped<IReportService, ReportService>();

// Line Messaging 服務
builder.Services.AddHttpClient<ILineMessagingService, LineMessagingService>(client =>
{
    client.BaseAddress = new Uri("https://api.line.me/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// 訊息派送服務
builder.Services.AddScoped<IMessageDispatchService, MessageDispatchService>();

// HTTP Client Factory（用於呼叫外部 API）
builder.Services.AddHttpClient("LineApi", client =>
{
    client.BaseAddress = new Uri("https://api.line.me/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// 加入 CORS 支援
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // 開發環境允許所有來源
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            // 生產環境需配置允許的來源
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// 加入健康檢查
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

// 註冊背景服務
builder.Services.AddHostedService<LineNotify.Api.BackgroundServices.MessageSenderService>();
builder.Services.AddHostedService<LineNotify.Api.BackgroundServices.RetryService>();

// ===========================================
// 建置應用程式
// ===========================================

var app = builder.Build();

// ===========================================
// 中介軟體配置
// ===========================================

// 全域例外處理（最優先）
app.UseGlobalExceptionHandler();

// 開發環境配置
if (app.Environment.IsDevelopment())
{
    // 啟用 OpenAPI（Swagger UI 在 /openapi/v1.json）
    app.MapOpenApi();

    // 開發環境自動確保資料庫建立
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.UseCors();

// API Key 認證（在 JWT 認證之前）
app.UseApiKeyAuth();

// 認證與授權
app.UseAuthentication();
app.UseAuthorization();

// 審計日誌（在認證之後，確保能取得使用者資訊）
app.UseAuditLog();

// 健康檢查端點
app.MapHealthChecks("/health");

// 靜態檔案
app.UseStaticFiles();

// Razor Pages 路由
app.MapRazorPages();

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
