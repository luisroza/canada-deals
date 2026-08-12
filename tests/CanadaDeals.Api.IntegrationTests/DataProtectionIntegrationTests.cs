using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CanadaDeals.Api.IntegrationTests;

public sealed class DataProtectionIntegrationTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private const string Password = "SecurePass42";

    [RequiresPostgresFact]
    public async Task Authentication_cookie_remains_valid_after_API_host_restart()
    {
        var email = $"restart-cookie-{Guid.NewGuid():N}@example.test";
        string authCookie;

        using (var firstFactory = fixture.WithWebHostBuilder(_ => { }))
        using (var firstClient = firstFactory.CreateClient(new() { AllowAutoRedirect = false }))
        using (var registration = await MutateAsync(firstClient, "/api/v1/account/register", new { email, password = Password }))
        {
            Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
            authCookie = CookiePair(registration, "CanadaDeals.Auth");
            await using var scope = firstFactory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
            Assert.True(await db.DataProtectionKeys.AnyAsync());
        }

        using var restartedFactory = fixture.WithWebHostBuilder(_ => { });
        using var restartedClient = restartedFactory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = false });
        restartedClient.DefaultRequestHeaders.Add("Cookie", authCookie);
        using var me = await restartedClient.GetAsync("/api/v1/account/me");
        me.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("isAuthenticated").GetBoolean());
        Assert.Equal(email, json.RootElement.GetProperty("email").GetString());
    }

    [RequiresPostgresFact]
    public async Task Email_confirmation_token_remains_valid_after_API_host_restart()
    {
        var email = $"restart-token-{Guid.NewGuid():N}@example.test";
        Guid userId;
        string code;

        using (var firstFactory = fixture.WithWebHostBuilder(builder =>
                   builder.UseSetting("Email:AutoConfirmDevelopmentAccounts", "false")))
        using (var firstClient = firstFactory.CreateClient(new() { AllowAutoRedirect = false }))
        using (var registration = await MutateAsync(firstClient, "/api/v1/account/register", new { email, password = Password }))
        {
            Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
            await using var scope = firstFactory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
            var capture = await db.ControlledEmailCaptures.SingleAsync(x => x.DestinationAddress == email);
            var link = capture.TextBody.Split('\n').Single(line => line.StartsWith("http://localhost:3000/account/confirm-email?", StringComparison.Ordinal));
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(link).Query);
            userId = Guid.Parse(query["userId"]!);
            code = query["code"].ToString();
        }

        using var restartedFactory = fixture.WithWebHostBuilder(builder =>
            builder.UseSetting("Email:AutoConfirmDevelopmentAccounts", "false"));
        using var restartedClient = restartedFactory.CreateClient(new() { AllowAutoRedirect = false });
        using var confirmation = await MutateAsync(restartedClient, "/api/v1/account/confirm-email", new { userId, code });
        Assert.Equal(HttpStatusCode.OK, confirmation.StatusCode);
        Assert.Contains("CONFIRMED", await confirmation.Content.ReadAsStringAsync());
    }

    private static async Task<HttpResponseMessage> MutateAsync(HttpClient client, string path, object body)
    {
        using var tokenResponse = await client.GetAsync("/api/v1/account/antiforgery");
        using var tokenJson = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", tokenJson.RootElement.GetProperty("requestToken").GetString());
        return await client.SendAsync(request);
    }

    private static string CookiePair(HttpResponseMessage response, string name)
    {
        var setCookie = response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith($"{name}=", StringComparison.Ordinal));
        return setCookie.Split(';', 2)[0];
    }
}
