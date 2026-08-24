using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Affiliates;
using CanadaDeals.Domain.Catalog;
using CanadaDeals.Domain.Integrations;
using CanadaDeals.Domain.Policies;
using CanadaDeals.Domain.Retailers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using System.Security.Cryptography;
using System.Text;

namespace CanadaDeals.Infrastructure.Persistence;

public static class DatabaseServices
{
    public const string DefaultConnection = "Host=localhost;Port=5432;Database=canadadeals;Username=canadadeals;Password=canadadeals";

    public static IServiceCollection AddCanadaDealsPersistence(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var connectionString = GetValidatedConnectionString(configuration, environment);
        services.AddDbContext<DealsDbContext>(options => options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(DealsDbContext).Assembly.FullName)));
        return services;
    }

    public static string GetValidatedConnectionString(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration.GetConnectionString("Database");
        if (string.IsNullOrWhiteSpace(configured))
        {
            if (environment.IsProduction())
                throw new InvalidOperationException("ConnectionStrings:Database is required in Production.");
            configured = DefaultConnection;
        }

        var builder = configured.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                      configured.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
            ? FromPostgresUri(configured)
            : new NpgsqlConnectionStringBuilder(configured);

        if (!environment.IsProduction()) return builder.ConnectionString;
        if (builder.Host is "localhost" or "127.0.0.1" or "::1")
            throw new InvalidOperationException("Production PostgreSQL must not use a loopback host.");

        var certificateAuthority = configuration["Database:CaCertificate"];
        if (string.IsNullOrWhiteSpace(certificateAuthority))
            throw new InvalidOperationException("Database:CaCertificate is required for Production TLS verification.");

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(certificateAuthority)))[..16];
        var certificatePath = Path.Combine(Path.GetTempPath(), $"canada-deals-postgres-{hash}.crt");
        if (!File.Exists(certificatePath)) File.WriteAllText(certificatePath, certificateAuthority, Encoding.ASCII);
        builder.SslMode = SslMode.VerifyFull;
        builder.RootCertificate = certificatePath;
        return builder.ConnectionString;
    }

    private static NpgsqlConnectionStringBuilder FromPostgresUri(string value)
    {
        var uri = new Uri(value);
        var userInfo = uri.UserInfo.Split(':', 2);
        if (userInfo.Length != 2 || string.IsNullOrWhiteSpace(uri.Host))
            throw new InvalidOperationException("The PostgreSQL URL is missing credentials or host information.");

        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = Uri.UnescapeDataString(userInfo[1]),
            SslMode = SslMode.Require
        };
    }

    public static async Task ApplyMigrationsAndSeedAsync(this IServiceProvider services, bool seedDemoData, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        await context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_lock(1120260810)", cancellationToken);
            await context.Database.MigrateAsync(cancellationToken);
        }
        finally
        {
            await context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock(1120260810)", cancellationToken);
            await context.Database.CloseConnectionAsync();
        }
        if (seedDemoData)
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            await context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(1120260811)", cancellationToken);
            await DemoDataSeeder.SeedAsync(context, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
    }
}

public static class DemoDataSeeder
{
    public static async Task SeedAsync(DealsDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Products.AnyAsync(cancellationToken))
        {
            var demoPolicy = await db.MerchantPolicies.SingleOrDefaultAsync(policy => policy.SourceKey == "demo-fixture", cancellationToken);
            if (demoPolicy is not null && !demoPolicy.CanUseAffiliateLinks)
            {
                demoPolicy.SetAffiliateLinkPermission(PolicyPermission.Allowed, DateTimeOffset.UtcNow);
                await db.SaveChangesAsync(cancellationToken);
            }
            await EnsureSlice6HistoryFixturesAsync(db, DateTimeOffset.UtcNow, cancellationToken);
            await EnsureAffiliateFixturesAsync(db, DateTimeOffset.UtcNow, cancellationToken);
            await EnsureRakutenE2eFixtureAsync(db, DateTimeOffset.UtcNow, cancellationToken);
            await EnsureStoreBannerFixturesAsync(db, DateTimeOffset.UtcNow, cancellationToken);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var policy = MerchantPolicy.Create(
            "demo-fixture",
            PolicyPermission.Allowed,
            PolicyPermission.Allowed,
            PolicyPermission.Denied,
            PolicyPermission.Allowed,
            24,
            "SAME_PRODUCT_ONLY",
            "DEMO_ONLY",
            "Demo fixture data - no live retailer relationship.",
            7,
            "Local synthetic data only",
            now,
            PolicyPermission.Allowed);
        var unknownPolicy = MerchantPolicy.Create(
            "unknown-fixture-policy",
            PolicyPermission.Unknown,
            PolicyPermission.Unknown,
            PolicyPermission.Unknown,
            PolicyPermission.Unknown,
            null,
            "UNKNOWN",
            "UNKNOWN",
            string.Empty,
            null,
            "UNKNOWN",
            now);

        var electronics = Category.Create("Electronics", "electronics");
        var tools = Category.Create("Home Improvement & Tools", "home-improvement-tools");
        var northstar = Brand.Create("Northstar Demo", "northstar-demo");
        var mapleforge = Brand.Create("MapleForge Demo", "mapleforge-demo");
        var ridgeway = Brand.Create("Ridgeway Demo", "ridgeway-demo");
        var demoNorth = Retailer.Create("demo-north-electronics", "Demo North Electronics");
        var demoHome = Retailer.Create("demo-home-tool", "Demo Home & Tool");
        var demoMarket = Retailer.Create("demo-market-lab", "Demo Market Lab");

        var productA = Product.Create("northstar-55-qled-tv", "Northstar 55-inch QLED TV", northstar, electronics, "NS55QLED-2026", "NS-55-QLED", "000000000001", new Dictionary<string, string>
        {
            ["screenSize"] = "55-inch", ["generation"] = "2026", ["colour"] = "Black"
        });
        var productB = Product.Create("northstar-quiet-headphones", "Northstar Quiet Wireless Headphones", northstar, electronics, "NS-QH100", "NS-QH100", "000000000002", new Dictionary<string, string>
        {
            ["generation"] = "2", ["colour"] = "Black"
        });
        var productC = Product.Create("mapleforge-20v-drill-kit", "MapleForge 20V Cordless Drill Kit", mapleforge, tools, "MF20-KIT", "MF-20-KIT", "000000000003", new Dictionary<string, string>
        {
            ["voltage"] = "20V", ["toolOnly"] = "false", ["batteryCount"] = "2", ["chargerIncluded"] = "true"
        });
        var productD = Product.Create("northstar-65-oled-tv", "Northstar 65-inch OLED TV", northstar, electronics, "NS65OLED-2025", "NS-65-OLED", "000000000004", new Dictionary<string, string>
        {
            ["screenSize"] = "65-inch", ["generation"] = "2025"
        });
        var productE = Product.Create("ridgeway-20v-drill-tool-only", "Ridgeway 20V Cordless Drill Tool-Only", ridgeway, tools, "RW20-TOOL", "RW-20-TOOL", "000000000005", new Dictionary<string, string>
        {
            ["voltage"] = "20V", ["toolOnly"] = "true", ["batteryCount"] = "0", ["chargerIncluded"] = "false"
        });
        var productF = Product.Create("mapleforge-compact-impact-driver", "MapleForge Compact Impact Driver", mapleforge, tools, "MF-ID-COMPACT", "MF-ID-COMPACT", "000000000006", new Dictionary<string, string>
        {
            ["voltage"] = "18V", ["toolOnly"] = "true"
        });
        var productG = Product.Create("search-fixture-unavailable-kettle", "Northstar Search Fixture Kettle", northstar, electronics, "NS-KETTLE-404", "NS-KETTLE-404", "990000000001", new Dictionary<string, string>());
        var productH = Product.Create("search-fixture-policy-hidden-speaker", "Northstar Policy Hidden Speaker", northstar, electronics, "NS-HIDDEN-404", "NS-HIDDEN-404", "990000000002", new Dictionary<string, string>());

        db.AddRange(policy, unknownPolicy, electronics, tools, northstar, mapleforge, ridgeway, demoNorth, demoHome, demoMarket);
        db.AddRange(productA, productB, productC, productD, productE, productF, productG, productH);
        await db.SaveChangesAsync(cancellationToken);

        var listingA1 = RetailerListing.Create(productA.Id, demoNorth.Id, "DEMO-A-NORTH", productA.Title, "https://demo.local/products/northstar-55-qled-tv", policy.Id, MatchState.Confirmed, now.AddHours(-2), now.AddHours(-2), 1099.99m, "CAD", FreshnessState.Recent, EvidenceState.Strong, HistoryAvailability.Reliable, productA.VariantAttributes, new Dictionary<string, string> { ["gtin"] = "000000000001" }, retailerSku: "DN-NS55", seller: "Demo North Electronics", isMarketplaceSeller: false, condition: ProductCondition.New, packQuantity: 1, regionAvailabilityContext: "Canada", onlineAvailability: OnlineAvailabilityState.Available, shippingContext: "Shipping calculated at checkout", approvedAffiliateDestinationReference: "https://demo.local/retailer/demo-a-north");
        var listingA2 = RetailerListing.Create(productA.Id, demoHome.Id, "DEMO-A-HOME", productA.Title, "https://demo.local/products/northstar-55-qled-tv-home", policy.Id, MatchState.Confirmed, now.AddHours(-4), now.AddHours(-4), 1129.99m, "CAD", FreshnessState.Recent, EvidenceState.Strong, HistoryAvailability.Reliable, productA.VariantAttributes, new Dictionary<string, string> { ["gtin"] = "000000000001" }, retailerSku: "DH-NS55", seller: "Demo Home & Tool", isMarketplaceSeller: false, approvedAffiliateDestinationReference: "https://demo.local/retailer/demo-a-home");
        var listingB = RetailerListing.Create(productB.Id, demoNorth.Id, "DEMO-B-NORTH", productB.Title, "https://demo.local/products/northstar-quiet-headphones", policy.Id, MatchState.Confirmed, now.AddHours(-8), now.AddHours(-8), 249.99m, "CAD", FreshnessState.Aging, EvidenceState.Unknown, HistoryAvailability.Unavailable, productB.VariantAttributes, new Dictionary<string, string> { ["gtin"] = "000000000002" }, "DN-NSQH");
        var listingC1 = RetailerListing.Create(productC.Id, demoHome.Id, "DEMO-C-KIT", productC.Title, "https://demo.local/products/mapleforge-20v-drill-kit", policy.Id, MatchState.Confirmed, now.AddHours(-3), now.AddHours(-3), 179.99m, "CAD", FreshnessState.Recent, EvidenceState.Partial, HistoryAvailability.Partial, productC.VariantAttributes, new Dictionary<string, string> { ["gtin"] = "000000000003" }, "DH-MF20KIT");
        var listingC2 = RetailerListing.Create(productC.Id, demoMarket.Id, "DEMO-C-TOOL", "MapleForge 20V Cordless Drill Tool-Only", "https://demo.local/products/mapleforge-20v-drill-tool-only", policy.Id, MatchState.PossibleMatchReview, now.AddHours(-2), now.AddHours(-2), 89.99m, "CAD", FreshnessState.Recent, EvidenceState.Partial, HistoryAvailability.Partial, new Dictionary<string, string> { ["voltage"] = "20V", ["toolOnly"] = "true", ["batteryCount"] = "0" }, new Dictionary<string, string> { ["mpn"] = "MF-20-TOOL" }, retailerSku: "DM-MF20TOOL", seller: "Demo Market Lab", isMarketplaceSeller: false, condition: ProductCondition.New, packQuantity: 1, bundleContents: "Tool only; batteries and charger excluded");
        var listingD = RetailerListing.Create(productD.Id, demoNorth.Id, "DEMO-D-NORTH", productD.Title, "https://demo.local/products/northstar-65-oled-tv", policy.Id, MatchState.Confirmed, now.AddDays(-3), now.AddDays(-3), 1399.99m, "CAD", FreshnessState.Stale, EvidenceState.Partial, HistoryAvailability.Partial, productD.VariantAttributes, new Dictionary<string, string> { ["gtin"] = "000000000004" }, "DN-NS65");
        var listingE = RetailerListing.Create(productE.Id, demoMarket.Id, "DEMO-E-TOOL", productE.Title, "https://demo.local/products/ridgeway-20v-drill-tool-only", policy.Id, MatchState.PossibleMatchReview, now.AddHours(-1), now.AddHours(-1), 79.99m, "CAD", FreshnessState.Recent, EvidenceState.Partial, HistoryAvailability.Unavailable, productE.VariantAttributes, new Dictionary<string, string> { ["gtin"] = "000000000005" }, retailerSku: "DM-RW20TOOL", seller: "Demo Market Lab", isMarketplaceSeller: false);
        var listingF = RetailerListing.Create(productF.Id, demoMarket.Id, "DEMO-F-UNKNOWN", productF.Title, "https://demo.local/products/mapleforge-compact-impact-driver", policy.Id, MatchState.NoMatch, now.AddHours(-2), now.AddHours(-2), 119.99m, "CAD", FreshnessState.Recent, EvidenceState.Unknown, HistoryAvailability.Unavailable, productF.VariantAttributes, new Dictionary<string, string> { ["gtin"] = "000000000006" }, "DM-MFID");
        var listingG = RetailerListing.Create(productG.Id, demoNorth.Id, "SEARCH-UNAVAILABLE", productG.Title, "https://demo.local/search-unavailable", policy.Id, MatchState.Confirmed, now.AddMinutes(-30), now.AddMinutes(-30), 49.99m, "CAD", FreshnessState.Recent, EvidenceState.Unknown, HistoryAvailability.Unavailable, productG.VariantAttributes, new Dictionary<string, string>(), onlineAvailability: OnlineAvailabilityState.Unavailable);
        var listingH = RetailerListing.Create(productH.Id, demoNorth.Id, "SEARCH-HIDDEN", productH.Title, "https://demo.local/search-hidden", unknownPolicy.Id, MatchState.Confirmed, now, now, 9.99m, "CAD", FreshnessState.Recent, EvidenceState.Unknown, HistoryAvailability.Unavailable, productH.VariantAttributes, new Dictionary<string, string>(), onlineAvailability: OnlineAvailabilityState.Available);

        db.AddRange(listingA1, listingA2, listingB, listingC1, listingC2, listingD, listingE, listingF, listingG, listingH);
        await db.SaveChangesAsync(cancellationToken);

        var observations = new List<PriceObservation>
        {
            PriceObservation.Create(listingA1.Id, 1099.99m, "CAD", now.AddHours(-2), now.AddHours(-2), true, "demo-a1-current"),
            PriceObservation.Create(listingA1.Id, 1199.99m, "CAD", now.AddDays(-12), now.AddDays(-12), true, "demo-a1-history"),
            PriceObservation.Create(listingA2.Id, 1129.99m, "CAD", now.AddHours(-4), now.AddHours(-4), true, "demo-a2-current"),
            PriceObservation.Create(listingA2.Id, 1249.99m, "CAD", now.AddDays(-10), now.AddDays(-10), true, "demo-a2-history"),
            PriceObservation.Create(listingB.Id, 249.99m, "CAD", now.AddHours(-8), now.AddHours(-8), true, "demo-b-current"),
            PriceObservation.Create(listingC1.Id, 179.99m, "CAD", now.AddHours(-3), now.AddHours(-3), true, "demo-c-current"),
            PriceObservation.Create(listingC1.Id, 199.99m, "CAD", now.AddDays(-8), now.AddDays(-8), true, "demo-c-gap"),
            PriceObservation.Create(listingD.Id, 1399.99m, "CAD", now.AddDays(-3), now.AddDays(-3), true, "demo-d-stale"),
            PriceObservation.Create(listingE.Id, 79.99m, "CAD", now.AddHours(-1), now.AddHours(-1), true, "demo-e-current"),
            PriceObservation.Create(listingF.Id, 119.99m, "CAD", now.AddHours(-2), now.AddHours(-2), true, "demo-f-current")
        };
        db.AddRange(observations);
        await db.SaveChangesAsync(cancellationToken);
        await EnsureSlice6HistoryFixturesAsync(db, now, cancellationToken);
        await EnsureAffiliateFixturesAsync(db, now, cancellationToken);
        await EnsureRakutenE2eFixtureAsync(db, now, cancellationToken);
        await EnsureStoreBannerFixturesAsync(db, now, cancellationToken);
    }

    private static async Task EnsureStoreBannerFixturesAsync(DealsDbContext db, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var definitions = new Dictionary<string, (string Title, string Subtitle, string AssetPath, int Order)>
        {
            ["demo-north-electronics"] = ("Shop Demo North", "Electronics and everyday tech", "/store-banners/electronics-devices.svg", 10),
            ["demo-home-tool"] = ("Shop Demo Home & Tool", "Tools and home essentials", "/store-banners/home-decor.svg", 20),
            ["demo-market-lab"] = ("Explore Demo Market", "A broad marketplace selection", "/store-banners/marketplace-packages.svg", 30),
            ["rakuten-controlled-fixture-retailer"] = ("Shop the controlled tech store", "Headphones and personal electronics", "/store-banners/pc-hardware.svg", 40)
        };
        var retailers = await db.Retailers.Where(retailer => definitions.Keys.Contains(retailer.Key)).ToListAsync(cancellationToken);
        var existingProfiles = await db.StoreBannerProfiles.Where(profile => retailers.Select(retailer => retailer.Id).Contains(profile.RetailerId))
            .Select(profile => profile.RetailerId).ToListAsync(cancellationToken);
        foreach (var retailer in retailers.Where(retailer => !existingProfiles.Contains(retailer.Id)))
        {
            var definition = definitions[retailer.Key];
            db.StoreBannerProfiles.Add(StoreBannerProfile.CreateOriginal(
                retailer.Id, definition.Title, definition.Subtitle, definition.AssetPath, definition.Order));
        }
        await db.SaveChangesAsync(cancellationToken);

        var activeFixtureKeys = new[] { "demo-north-electronics", "rakuten-controlled-fixture-retailer" };
        var activeRetailers = retailers.Where(retailer => activeFixtureKeys.Contains(retailer.Key)).ToList();
        var existingDestinations = await db.StoreAffiliateDestinations
            .Where(destination => activeRetailers.Select(retailer => retailer.Id).Contains(destination.RetailerId))
            .Select(destination => destination.RetailerId).ToListAsync(cancellationToken);
        foreach (var retailer in activeRetailers.Where(retailer => !existingDestinations.Contains(retailer.Id)))
        {
            var program = await db.AffiliatePrograms.SingleAsync(candidate =>
                candidate.RetailerId == retailer.Id && candidate.Status == AffiliateProgramStatus.Active, cancellationToken);
            var rakuten = program.Provider == AffiliateProviderType.Rakuten;
            db.StoreAffiliateDestinations.Add(StoreAffiliateDestination.CreateActive(
                retailer.Id,
                program.Id,
                program.Provider,
                rakuten ? "https://click.linksynergy.test/store?id=fixture-only" : $"https://demo.local/go/{retailer.Key}",
                rakuten ? "https://merchant.safe.test" : $"https://demo.local/stores/{retailer.Key}",
                now,
                now.AddYears(1),
                now.AddYears(2),
                "FIXTURE_ONLY_STORE_DESTINATION"));
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureAffiliateFixturesAsync(DealsDbContext db, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var retailers = await db.Retailers
            .Where(retailer => retailer.Key == "demo-north-electronics" || retailer.Key == "demo-home-tool")
            .ToListAsync(cancellationToken);
        var retailerIdsWithFixtureProgram = await db.AffiliatePrograms
            .Where(program => program.Provider == AffiliateProviderType.Other && retailers.Select(retailer => retailer.Id).Contains(program.RetailerId))
            .Select(program => program.RetailerId)
            .ToListAsync(cancellationToken);
        foreach (var retailer in retailers)
        {
            if (retailerIdsWithFixtureProgram.Contains(retailer.Id)) continue;

            var program = AffiliateProgram.Create(
                retailer.Id, AffiliateProviderType.Other, AffiliateProgramStatus.Active, now,
                providerProgramId: $"fixture-{retailer.Key}", mediaPropertyId: "fixture-web",
                allowsDeepLinking: true, destinationDomains: ["demo.local"], trackingDomains: ["demo.local"],
                relationshipEvidenceReference: "FIXTURE_ONLY", relationshipValidatedAt: now);
            db.AffiliatePrograms.Add(program);

            var listings = await db.RetailerListings
                .Where(listing => listing.RetailerId == retailer.Id && listing.ApprovedAffiliateDestinationReference != null)
                .ToListAsync(cancellationToken);
            foreach (var listing in listings)
            {
                db.AffiliateLinks.Add(AffiliateLink.CreateActive(
                    listing.Id, program.Id, AffiliateProviderType.Other,
                    listing.ApprovedAffiliateDestinationReference!, listing.ApprovedAffiliateDestinationReference!,
                    now, now.AddYears(1), now.AddYears(2), "fixture-link"));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureRakutenE2eFixtureAsync(DealsDbContext db, DateTimeOffset now, CancellationToken cancellationToken)
    {
        const string productSlug = "rakuten-controlled-fixture-headphones";
        const string retailerKey = "rakuten-controlled-fixture-retailer";
        const string listingKey = "RAKUTEN-FIXTURE-LINK-1";
        const string advertiserMid = "900000001";

        if (await db.Products.AnyAsync(product => product.Slug == productSlug, cancellationToken)) return;

        var policy = await db.MerchantPolicies.SingleAsync(item => item.SourceKey == "demo-fixture", cancellationToken);
        var category = await db.Categories.SingleAsync(item => item.Slug == "electronics", cancellationToken);
        var brand = await db.Brands.SingleAsync(item => item.Slug == "northstar-demo", cancellationToken);
        var retailer = Retailer.Create(retailerKey, "Rakuten Controlled Fixture Retailer");
        var product = Product.Create(
            productSlug,
            "Rakuten Controlled Fixture Headphones",
            brand,
            category,
            "RKT-FIXTURE-100",
            "RKT-FIXTURE-100",
            "990000000009",
            new Dictionary<string, string> { ["fixtureSource"] = "rakuten-controlled" });

        db.AddRange(retailer, product);
        await db.SaveChangesAsync(cancellationToken);

        var destinationUrl = "https://merchant.safe.test/products/rakuten-controlled-fixture-headphones";
        var listing = RetailerListing.Create(
            product.Id,
            retailer.Id,
            listingKey,
            product.Title,
            destinationUrl,
            policy.Id,
            MatchState.Confirmed,
            now.AddMinutes(-10),
            now.AddMinutes(-10),
            199.99m,
            "CAD",
            FreshnessState.Recent,
            EvidenceState.Strong,
            HistoryAvailability.Unavailable,
            product.VariantAttributes,
            new Dictionary<string, string> { ["upc"] = "990000000009" },
            retailerSku: "RKT-FIXTURE-100",
            approvedAffiliateDestinationReference: destinationUrl,
            seller: null,
            isMarketplaceSeller: null,
            condition: ProductCondition.Unknown,
            regionAvailabilityContext: "Canada",
            onlineAvailability: OnlineAvailabilityState.Unknown);
        db.RetailerListings.Add(listing);
        await db.SaveChangesAsync(cancellationToken);

        var capability = RakutenAdvertiserCapability.Create(
            advertiserMid,
            "Rakuten Controlled Fixture Advertiser",
            "https://merchant.safe.test",
            IntegrationAdvertiserStatus.Active,
            IntegrationPartnershipStatus.Active,
            ["CA"],
            productFeedAvailable: true,
            deepLinksAvailable: true,
            checkedAt: now,
            partnershipApprovedAt: now,
            partnershipStatusUpdatedAt: now);
        capability.ConfigureOperatorMapping(retailer.Id, policy.Id, canadaRelevant: true, affiliateEnabled: true, catalogEnabled: true, now);

        var program = AffiliateProgram.Create(
            retailer.Id,
            AffiliateProviderType.Rakuten,
            AffiliateProgramStatus.Active,
            now,
            providerProgramId: advertiserMid,
            allowsDeepLinking: true,
            destinationDomains: ["merchant.safe.test"],
            trackingDomains: ["click.linksynergy.test"],
            relationshipEvidenceReference: "FIXTURE_ONLY_RAKUTEN_CONTROLLED_CONTRACT",
            relationshipValidatedAt: now);

        db.AddRange(
            capability,
            program,
            RakutenSourceMapping.Create(advertiserMid, listingKey, listing.Id, now),
            PriceObservation.Create(listing.Id, 199.99m, "CAD", now.AddMinutes(-10), now.AddMinutes(-10), true, "rakuten-fixture-current"));
        await db.SaveChangesAsync(cancellationToken);

        db.AffiliateLinks.Add(AffiliateLink.CreateActive(
            listing.Id,
            program.Id,
            AffiliateProviderType.Rakuten,
            "https://click.linksynergy.test/deep?id=fixture-only",
            destinationUrl,
            now,
            now.AddYears(1),
            now.AddYears(2),
            "FIXTURE_ONLY"));
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureSlice6HistoryFixturesAsync(DealsDbContext db, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var listingKeys = new[] { "DEMO-A-NORTH", "DEMO-A-HOME", "DEMO-D-NORTH", "DEMO-C-TOOL", "SEARCH-HIDDEN" };
        var listings = await db.RetailerListings
            .Where(listing => listingKeys.Contains(listing.ExternalListingId))
            .ToDictionaryAsync(listing => listing.ExternalListingId, cancellationToken);
        if (listings.Count == 0) return;

        var definitions = new List<(string ListingKey, int DaysAgo, decimal Amount, string SourceHash)>
        {
            ("DEMO-A-NORTH", 84, 1299.99m, "slice6-a1-84"),
            ("DEMO-A-NORTH", 72, 1279.99m, "slice6-a1-72"),
            ("DEMO-A-NORTH", 60, 1249.99m, "slice6-a1-60"),
            ("DEMO-A-NORTH", 48, 1229.99m, "slice6-a1-48"),
            ("DEMO-A-NORTH", 36, 1199.99m, "slice6-a1-36"),
            ("DEMO-A-NORTH", 29, 1179.99m, "slice6-a1-29"),
            ("DEMO-A-NORTH", 24, 1169.99m, "slice6-a1-24"),
            ("DEMO-A-NORTH", 20, 1149.99m, "slice6-a1-20"),
            ("DEMO-A-HOME", 20, 1049.99m, "slice6-a2-20"),
            ("DEMO-A-NORTH", 18, 1139.99m, "slice6-a1-18"),
            ("DEMO-A-NORTH", 8, 1089.99m, "slice6-a1-8"),
            ("DEMO-A-NORTH", 4, 1119.99m, "slice6-a1-4"),
            ("DEMO-A-NORTH", 1, 1099.99m, "slice6-a1-1"),
            ("DEMO-D-NORTH", 86, 1599.99m, "slice6-d-86"),
            ("DEMO-D-NORTH", 74, 1579.99m, "slice6-d-74"),
            ("DEMO-D-NORTH", 62, 1549.99m, "slice6-d-62"),
            ("DEMO-D-NORTH", 50, 1529.99m, "slice6-d-50"),
            ("DEMO-D-NORTH", 38, 1499.99m, "slice6-d-38"),
            ("DEMO-D-NORTH", 29, 1479.99m, "slice6-d-29"),
            ("DEMO-D-NORTH", 24, 1469.99m, "slice6-d-24"),
            ("DEMO-D-NORTH", 18, 1449.99m, "slice6-d-18"),
            ("DEMO-D-NORTH", 12, 1429.99m, "slice6-d-12"),
            ("DEMO-D-NORTH", 7, 1419.99m, "slice6-d-7"),
            ("DEMO-C-TOOL", 20, 49.99m, "slice6-unsafe-c2-20"),
            ("DEMO-C-TOOL", 4, 59.99m, "slice6-unsafe-c2-4"),
            ("SEARCH-HIDDEN", 10, 19.99m, "slice6-policy-hidden-10"),
            ("SEARCH-HIDDEN", 2, 9.99m, "slice6-policy-hidden-2")
        };
        var hashes = definitions.Select(definition => definition.SourceHash).ToArray();
        var existingHashes = await db.PriceObservations
            .Where(observation => hashes.Contains(observation.SourceHash))
            .Select(observation => observation.SourceHash)
            .ToHashSetAsync(cancellationToken);

        foreach (var definition in definitions.Where(definition => !existingHashes.Contains(definition.SourceHash)))
        {
            if (!listings.TryGetValue(definition.ListingKey, out var listing)) continue;
            var observedAt = now.AddDays(-definition.DaysAgo);
            db.Add(PriceObservation.Create(listing.Id, definition.Amount, "CAD", observedAt, observedAt, true, definition.SourceHash));
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
