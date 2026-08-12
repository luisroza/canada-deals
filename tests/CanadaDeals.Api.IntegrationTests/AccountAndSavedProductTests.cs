using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CanadaDeals.Domain.Accounts;
using CanadaDeals.Infrastructure.Identity;
using CanadaDeals.Infrastructure.Email;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Hosting;

namespace CanadaDeals.Api.IntegrationTests;

public sealed class AccountAndSavedProductTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private const string Password = "SecurePass42";

    private HttpClient CreateClient() => fixture.CreateClient(new() { AllowAutoRedirect = false });

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/v1/account/antiforgery");
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("requestToken").GetString()!;
    }

    private static async Task<HttpResponseMessage> MutateAsync(HttpClient client, HttpMethod method, string path, object? body = null)
    {
        var token = await GetAntiforgeryTokenAsync(client);
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-CSRF-TOKEN", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    private static async Task RegisterAsync(HttpClient client, string email)
    {
        using var response = await MutateAsync(client, HttpMethod.Post, "/api/v1/account/register", new { email, password = Password });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<Guid> GetProductIdAsync(HttpClient client, string slug = "northstar-55-qled-tv")
    {
        using var response = await client.GetAsync($"/api/v1/products/{slug}");
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("productId").GetGuid();
    }

    [RequiresPostgresFact]
    public async Task Register_creates_a_confirmed_development_account_and_session()
    {
        var client = CreateClient();
        var email = $"register-{Guid.NewGuid():N}@example.test";

        using var response = await MutateAsync(client, HttpMethod.Post, "/api/v1/account/register", new { email, password = Password });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), value => value.Contains("CanadaDeals.Auth=", StringComparison.Ordinal) && value.Contains("httponly", StringComparison.OrdinalIgnoreCase) && value.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase));
        using var me = await client.GetAsync("/api/v1/account/me");
        using var json = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("isAuthenticated").GetBoolean());
        Assert.Equal(email, json.RootElement.GetProperty("email").GetString());

        await using var scope = fixture.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.True((await users.FindByEmailAsync(email))?.EmailConfirmed);
    }

    [RequiresPostgresFact]
    public async Task Production_registration_stays_unconfirmed_and_uses_a_secure_cookie_after_confirmation()
    {
        using var productionFactory = fixture.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            ConfigureProductionEmail(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ITransactionalEmailSender>();
                services.AddScoped<ITransactionalEmailSender, ControlledTransactionalEmailSender>();
            });
        });
        var client = productionFactory.CreateClient(new() { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        var email = $"production-gate-{Guid.NewGuid():N}@example.test";

        using var registration = await MutateAsync(client, HttpMethod.Post, "/api/v1/account/register", new { email, password = Password });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        Assert.Contains("\"isAuthenticated\":false", await registration.Content.ReadAsStringAsync());
        using (var me = await client.GetAsync("/api/v1/account/me"))
            Assert.Contains("\"isAuthenticated\":false", await me.Content.ReadAsStringAsync());

        await using (var scope = productionFactory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByEmailAsync(email))!;
            Assert.False(user.EmailConfirmed);
            var token = await users.GenerateEmailConfirmationTokenAsync(user);
            Assert.True((await users.ConfirmEmailAsync(user, token)).Succeeded);
        }

        using var login = await MutateAsync(client, HttpMethod.Post, "/api/v1/account/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains(login.Headers.GetValues("Set-Cookie"), value =>
            value.Contains("__Host-CanadaDeals.Auth=", StringComparison.Ordinal) &&
            value.Contains("secure", StringComparison.OrdinalIgnoreCase) &&
            value.Contains("httponly", StringComparison.OrdinalIgnoreCase) &&
            value.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase));
    }

    private static void ConfigureProductionEmail(IWebHostBuilder builder)
    {
        builder.UseSetting("Email:Enabled", "true");
        builder.UseSetting("Email:Provider", "Resend");
        builder.UseSetting("Email:ApiKey", "re_test_only");
        builder.UseSetting("Email:FromAddress", "alerts@example.test");
        builder.UseSetting("Email:PublicOrigin", "https://example.test");
        builder.UseSetting("Email:WebhookSigningSecret", "whsec_MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTIzNDU2Nzg5MDE=");
    }

    [RequiresPostgresFact]
    public async Task Normalized_duplicate_email_fails_without_disclosing_account_state()
    {
        var email = $"duplicate-{Guid.NewGuid():N}@example.test";
        await RegisterAsync(CreateClient(), email);
        var secondClient = CreateClient();

        using var response = await MutateAsync(secondClient, HttpMethod.Post, "/api/v1/account/register", new { email = email.ToUpperInvariant(), password = Password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Unable to create an account with these details", body);
        Assert.DoesNotContain("already taken", body, StringComparison.OrdinalIgnoreCase);
    }

    [RequiresPostgresFact]
    public async Task Login_uses_a_generic_error_and_establishes_a_cookie_session_for_valid_credentials()
    {
        var client = CreateClient();
        var email = $"login-{Guid.NewGuid():N}@example.test";
        await RegisterAsync(client, email);
        using (var logout = await MutateAsync(client, HttpMethod.Post, "/api/v1/account/logout"))
            Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        using var wrong = await MutateAsync(client, HttpMethod.Post, "/api/v1/account/login", new { email, password = "IncorrectPass42" });
        using var unknown = await MutateAsync(client, HttpMethod.Post, "/api/v1/account/login", new { email = $"unknown-{Guid.NewGuid():N}@example.test", password = "IncorrectPass42" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Contains("Invalid email or password", await wrong.Content.ReadAsStringAsync());
        Assert.Contains("Invalid email or password", await unknown.Content.ReadAsStringAsync());

        using var valid = await MutateAsync(client, HttpMethod.Post, "/api/v1/account/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
        using var me = await client.GetAsync("/api/v1/account/me");
        Assert.Contains("\"isAuthenticated\":true", await me.Content.ReadAsStringAsync());
    }

    [RequiresPostgresFact]
    public async Task Logout_ends_private_access_but_keeps_public_discovery_available()
    {
        var client = CreateClient();
        await RegisterAsync(client, $"logout-{Guid.NewGuid():N}@example.test");

        using var logout = await MutateAsync(client, HttpMethod.Post, "/api/v1/account/logout");
        using var saved = await client.GetAsync("/api/v1/saved-products");
        using var discovery = await client.GetAsync("/api/v1/deals");

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, saved.StatusCode);
        Assert.Equal(HttpStatusCode.OK, discovery.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Authentication_rate_limit_returns_429_after_the_configured_budget()
    {
        using var limitedFactory = fixture.WithWebHostBuilder(builder => builder.UseSetting("AuthenticationRateLimit:PermitLimit", "3"));
        var client = limitedFactory.CreateClient(new() { AllowAutoRedirect = false });
        var statuses = new List<HttpStatusCode>();
        for (var index = 0; index < 4; index++)
        {
            using var response = await MutateAsync(client, HttpMethod.Post, "/api/v1/account/login", new { email = $"missing-{index}@example.test", password = "IncorrectPass42" });
            statuses.Add(response.StatusCode);
        }

        Assert.Equal([HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests], statuses);
    }

    [RequiresPostgresFact]
    public async Task State_changing_account_and_save_operations_require_antiforgery_tokens()
    {
        var anonymous = CreateClient();
        using var register = await anonymous.PostAsJsonAsync("/api/v1/account/register", new { email = $"csrf-{Guid.NewGuid():N}@example.test", password = Password });
        Assert.Equal(HttpStatusCode.BadRequest, register.StatusCode);

        var client = CreateClient();
        await RegisterAsync(client, $"csrf-save-{Guid.NewGuid():N}@example.test");
        var productId = await GetProductIdAsync(client);
        using var save = await client.PutAsync($"/api/v1/saved-products/{productId}", null);
        using var unsave = await client.DeleteAsync($"/api/v1/saved-products/{productId}");

        Assert.Equal(HttpStatusCode.BadRequest, save.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unsave.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Save_is_persisted_idempotent_listed_and_reversible()
    {
        var client = CreateClient();
        var email = $"save-{Guid.NewGuid():N}@example.test";
        await RegisterAsync(client, email);
        var productId = await GetProductIdAsync(client);

        using var first = await MutateAsync(client, HttpMethod.Put, $"/api/v1/saved-products/{productId}");
        using var second = await MutateAsync(client, HttpMethod.Put, $"/api/v1/saved-products/{productId}");
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        using var list = await client.GetAsync("/api/v1/saved-products");
        list.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Assert.Single(json.RootElement.EnumerateArray());
        Assert.Equal(productId, json.RootElement[0].GetProperty("productId").GetGuid());
        Assert.Equal("STRONG", json.RootElement[0].GetProperty("evidenceState").GetString());

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
            Assert.Equal(1, await db.SavedProducts.CountAsync(x => x.ProductId == productId && x.UserId == db.Users.Where(u => u.Email == email).Select(u => u.Id).Single()));
        }

        using var removed = await MutateAsync(client, HttpMethod.Delete, $"/api/v1/saved-products/{productId}");
        using var removedAgain = await MutateAsync(client, HttpMethod.Delete, $"/api/v1/saved-products/{productId}");
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, removedAgain.StatusCode);
        using var empty = await client.GetAsync("/api/v1/saved-products");
        Assert.Equal("[]", await empty.Content.ReadAsStringAsync());
    }

    [RequiresPostgresFact]
    public async Task Saving_does_not_change_public_price_truth_or_organic_order()
    {
        var client = CreateClient();
        await RegisterAsync(client, $"truth-neutral-{Guid.NewGuid():N}@example.test");
        var productId = await GetProductIdAsync(client);
        using var before = await client.GetAsync("/api/v1/deals");
        var beforeBody = await before.Content.ReadAsStringAsync();

        using var save = await MutateAsync(client, HttpMethod.Put, $"/api/v1/saved-products/{productId}");
        save.EnsureSuccessStatusCode();
        using var after = await client.GetAsync("/api/v1/deals");

        Assert.Equal(beforeBody, await after.Content.ReadAsStringAsync());
    }

    [RequiresPostgresFact]
    public async Task Save_rejects_an_unknown_product_without_creating_an_orphan()
    {
        var client = CreateClient();
        await RegisterAsync(client, $"invalid-product-{Guid.NewGuid():N}@example.test");
        var unknown = Guid.NewGuid();

        using var response = await MutateAsync(client, HttpMethod.Put, $"/api/v1/saved-products/{unknown}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        Assert.False(await db.SavedProducts.AnyAsync(x => x.ProductId == unknown));
    }

    [RequiresPostgresFact]
    public async Task Saved_product_survives_logout_and_a_new_login_session()
    {
        var email = $"persistent-{Guid.NewGuid():N}@example.test";
        var firstSession = CreateClient();
        await RegisterAsync(firstSession, email);
        var productId = await GetProductIdAsync(firstSession);
        using (var save = await MutateAsync(firstSession, HttpMethod.Put, $"/api/v1/saved-products/{productId}"))
            save.EnsureSuccessStatusCode();
        using (var logout = await MutateAsync(firstSession, HttpMethod.Post, "/api/v1/account/logout"))
            Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var secondSession = CreateClient();
        using (var login = await MutateAsync(secondSession, HttpMethod.Post, "/api/v1/account/login", new { email, password = Password }))
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var list = await secondSession.GetAsync("/api/v1/saved-products");
        using var json = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Assert.Contains(json.RootElement.EnumerateArray(), item => item.GetProperty("productId").GetGuid() == productId);
    }

    [RequiresPostgresFact]
    public async Task User_isolation_prevents_cross_user_read_or_delete()
    {
        var userA = CreateClient();
        var userB = CreateClient();
        var emailA = $"isolation-a-{Guid.NewGuid():N}@example.test";
        await RegisterAsync(userA, emailA);
        await RegisterAsync(userB, $"isolation-b-{Guid.NewGuid():N}@example.test");
        var productId = await GetProductIdAsync(userA);
        using (var save = await MutateAsync(userA, HttpMethod.Put, $"/api/v1/saved-products/{productId}"))
            Assert.Equal(HttpStatusCode.Created, save.StatusCode);

        using var listB = await userB.GetAsync("/api/v1/saved-products");
        Assert.Equal("[]", await listB.Content.ReadAsStringAsync());
        using var deleteByB = await MutateAsync(userB, HttpMethod.Delete, $"/api/v1/saved-products/{productId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteByB.StatusCode);

        using var listA = await userA.GetAsync("/api/v1/saved-products");
        using var jsonA = JsonDocument.Parse(await listA.Content.ReadAsStringAsync());
        Assert.Single(jsonA.RootElement.EnumerateArray());
        Assert.Equal(productId, jsonA.RootElement[0].GetProperty("productId").GetGuid());
    }

    [RequiresPostgresFact]
    public async Task Database_enforces_unique_intent_and_cascades_only_user_owned_saves()
    {
        var client = CreateClient();
        var email = $"constraints-{Guid.NewGuid():N}@example.test";
        await RegisterAsync(client, email);
        var productId = await GetProductIdAsync(client);
        using (var saved = await MutateAsync(client, HttpMethod.Put, $"/api/v1/saved-products/{productId}"))
            saved.EnsureSuccessStatusCode();

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var user = await db.Users.SingleAsync(x => x.Email == email);
        db.SavedProducts.Add(SavedProduct.Create(user.Id, productId, DateTimeOffset.UtcNow));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        user = (await users.FindByEmailAsync(email))!;
        Assert.True((await users.DeleteAsync(user)).Succeeded);
        Assert.False(await db.SavedProducts.AnyAsync(x => x.UserId == user.Id));
        Assert.True(await db.Products.AnyAsync(x => x.Id == productId));
    }
}
