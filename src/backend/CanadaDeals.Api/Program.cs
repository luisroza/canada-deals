using System.Threading.RateLimiting;
using System.Security.Claims;
using CanadaDeals.Api.Health;
using CanadaDeals.Api.Services;
using CanadaDeals.Infrastructure.Alerts;
using CanadaDeals.Infrastructure.Email;
using CanadaDeals.Infrastructure.Identity;
using CanadaDeals.Infrastructure.Persistence;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddCanadaDealsPersistence(builder.Configuration);
var databaseConnection = builder.Configuration.GetConnectionString("Database") ?? DatabaseServices.DefaultConnection;
builder.Services.AddHangfire(configuration => configuration.UsePostgreSqlStorage(options => options.UseNpgsqlConnection(databaseConnection)));
builder.Services.AddCanadaDealsTransactionalEmail(builder.Configuration, builder.Environment);
builder.Services.AddScoped<PriceAlertEvaluationJob>();
builder.Services.AddScoped<AccountConfirmationEmailService>();
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true;
        options.Tokens.EmailConfirmationTokenProvider = "email-confirmation";
        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredUniqueChars = 4;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<DealsDbContext>()
    .AddDefaultTokenProviders()
    .AddTokenProvider<EmailConfirmationTokenProvider<ApplicationUser>>("email-confirmation");
builder.Services.Configure<EmailConfirmationTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromHours(builder.Configuration.GetValue("Email:ConfirmationTokenHours", 24)));
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = builder.Environment.IsDevelopment() ? "CanadaDeals.Auth" : "__Host-CanadaDeals.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Cookie.IsEssential = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = builder.Environment.IsDevelopment() ? "CanadaDeals.Antiforgery" : "__Host-CanadaDeals.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("authentication", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue("AuthenticationRateLimit:PermitLimit", 10),
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("price-alert-mutations", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue("PriceAlertRateLimit:PermitLimit", 30),
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<CatalogQueryService>();
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("postgresql");

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    await app.Services.ApplyMigrationsAndSeedAsync(app.Configuration.GetValue<bool>("Database:SeedDemoData"));
}

app.UseExceptionHandler(errorApplication => errorApplication.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await Results.Problem("An unexpected error occurred.").ExecuteAsync(context);
}));
app.UseRouting();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.UseAntiforgery();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
