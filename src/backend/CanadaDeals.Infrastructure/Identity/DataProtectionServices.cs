using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CanadaDeals.Infrastructure.Identity;

public sealed class DataProtectionOptions
{
    public const string SectionName = "DataProtection";
    public bool PersistKeysToDatabase { get; set; } = true;
    public string ApplicationName { get; set; } = "CanadaDeals";
    public string? CertificateBase64 { get; set; }
    public string? CertificatePassword { get; set; }
}

public static class DataProtectionServices
{
    public static IServiceCollection AddCanadaDealsDataProtection(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var settings = configuration.GetSection(DataProtectionOptions.SectionName).Get<DataProtectionOptions>() ?? new();
        if (string.IsNullOrWhiteSpace(settings.ApplicationName))
            throw new InvalidOperationException("DataProtection:ApplicationName is required.");
        if (environment.IsProduction() && !settings.PersistKeysToDatabase)
            throw new InvalidOperationException("Production Data Protection keys must be persisted to PostgreSQL.");

        var builder = services.AddDataProtection().SetApplicationName(settings.ApplicationName);
        if (settings.PersistKeysToDatabase)
            builder.PersistKeysToDbContext<DealsDbContext>();

        if (!string.IsNullOrWhiteSpace(settings.CertificateBase64))
        {
            X509Certificate2 certificate;
            try
            {
                certificate = X509CertificateLoader.LoadPkcs12(
                    Convert.FromBase64String(settings.CertificateBase64),
                    settings.CertificatePassword,
                    X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
            }
            catch (Exception exception) when (exception is FormatException or CryptographicException)
            {
                throw new InvalidOperationException("Data Protection certificate configuration is invalid.", exception);
            }

            if (!certificate.HasPrivateKey)
                throw new InvalidOperationException("Data Protection certificate must include a private key.");
            builder.ProtectKeysWithCertificate(certificate);
        }
        else if (environment.IsProduction())
        {
            throw new InvalidOperationException("DataProtection:CertificateBase64 is required in Production.");
        }

        return services;
    }
}
