using Microsoft.Extensions.Options;

namespace CanadaDeals.Infrastructure.Affiliates;

public sealed class AffiliateOptionsValidator : IValidateOptions<AffiliateOptions>
{
    public ValidateOptionsResult Validate(string? name, AffiliateOptions options)
    {
        var failures = new List<string>();
        if (options.RevalidateHours is < 1 or > 8760) failures.Add("Affiliate:RevalidateHours must be between 1 and 8760.");
        if (options.FailureRetryMinutes is < 1 or > 1440) failures.Add("Affiliate:FailureRetryMinutes must be between 1 and 1440.");

        if (options.Impact.Enabled)
        {
            if (!ValidHttpsBase(options.Impact.BaseUrl)) failures.Add("Affiliate:Impact:BaseUrl must be an HTTPS origin.");
            if (string.IsNullOrWhiteSpace(options.Impact.AccountSid)) failures.Add("Affiliate:Impact:AccountSid is required when Impact is enabled.");
            if (string.IsNullOrWhiteSpace(options.Impact.AuthToken)) failures.Add("Affiliate:Impact:AuthToken is required when Impact is enabled.");
        }

        if (options.Cj.Enabled)
        {
            if (!ValidHttpsBase(options.Cj.BaseUrl)) failures.Add("Affiliate:Cj:BaseUrl must be an HTTPS origin.");
            if (string.IsNullOrWhiteSpace(options.Cj.PersonalAccessToken)) failures.Add("Affiliate:Cj:PersonalAccessToken is required when CJ is enabled.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static bool ValidHttpsBase(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo);
}
