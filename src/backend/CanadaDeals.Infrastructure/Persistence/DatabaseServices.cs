using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Catalog;
using CanadaDeals.Domain.Policies;
using CanadaDeals.Domain.Retailers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CanadaDeals.Infrastructure.Persistence;

public static class DatabaseServices
{
    public const string DefaultConnection = "Host=localhost;Port=5432;Database=canadadeals;Username=canadadeals;Password=canadadeals";

    public static IServiceCollection AddCanadaDealsPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database") ?? DefaultConnection;
        services.AddDbContext<DealsDbContext>(options => options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(DealsDbContext).Assembly.FullName)));
        return services;
    }

    public static async Task ApplyMigrationsAndSeedAsync(this IServiceProvider services, bool seedDemoData, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
        if (seedDemoData) await DemoDataSeeder.SeedAsync(context, cancellationToken);
    }
}

public static class DemoDataSeeder
{
    public static async Task SeedAsync(DealsDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Products.AnyAsync(cancellationToken)) return;

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
            now);
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

        db.AddRange(policy, unknownPolicy, electronics, tools, northstar, mapleforge, ridgeway, demoNorth, demoHome, demoMarket);
        db.AddRange(productA, productB, productC, productD, productE, productF);
        await db.SaveChangesAsync(cancellationToken);

        var listingA1 = RetailerListing.Create(productA.Id, demoNorth.Id, "DEMO-A-NORTH", productA.Title, "https://demo.local/products/northstar-55-qled-tv", policy.Id, MatchState.Confirmed, now.AddHours(-2), now.AddHours(-2), 1099.99m, "CAD", FreshnessState.Recent, EvidenceState.Strong, HistoryAvailability.Reliable, productA.VariantAttributes, new Dictionary<string, string> { ["gtin"] = "000000000001" }, retailerSku: "DN-NS55", seller: "Demo North Electronics", isMarketplaceSeller: false, condition: ProductCondition.New, packQuantity: 1, regionAvailabilityContext: "Canada", onlineAvailability: OnlineAvailabilityState.Available, shippingContext: "Shipping calculated at checkout", approvedAffiliateDestinationReference: "https://demo.local/retailer/demo-a-north");
        var listingA2 = RetailerListing.Create(productA.Id, demoHome.Id, "DEMO-A-HOME", productA.Title, "https://demo.local/products/northstar-55-qled-tv-home", policy.Id, MatchState.Confirmed, now.AddHours(-4), now.AddHours(-4), 1129.99m, "CAD", FreshnessState.Recent, EvidenceState.Strong, HistoryAvailability.Reliable, productA.VariantAttributes, new Dictionary<string, string> { ["gtin"] = "000000000001" }, retailerSku: "DH-NS55", seller: "Demo Home & Tool", isMarketplaceSeller: false, approvedAffiliateDestinationReference: "https://demo.local/retailer/demo-a-home");
        var listingB = RetailerListing.Create(productB.Id, demoNorth.Id, "DEMO-B-NORTH", productB.Title, "https://demo.local/products/northstar-quiet-headphones", policy.Id, MatchState.Confirmed, now.AddHours(-8), now.AddHours(-8), 249.99m, "CAD", FreshnessState.Aging, EvidenceState.Unknown, HistoryAvailability.Unavailable, productB.VariantAttributes, new Dictionary<string, string> { ["gtin"] = "000000000002" }, "DN-NSQH");
        var listingC1 = RetailerListing.Create(productC.Id, demoHome.Id, "DEMO-C-KIT", productC.Title, "https://demo.local/products/mapleforge-20v-drill-kit", policy.Id, MatchState.Confirmed, now.AddHours(-3), now.AddHours(-3), 179.99m, "CAD", FreshnessState.Recent, EvidenceState.Partial, HistoryAvailability.Partial, productC.VariantAttributes, new Dictionary<string, string> { ["gtin"] = "000000000003" }, "DH-MF20KIT");
        var listingC2 = RetailerListing.Create(productC.Id, demoMarket.Id, "DEMO-C-TOOL", "MapleForge 20V Cordless Drill Tool-Only", "https://demo.local/products/mapleforge-20v-drill-tool-only", policy.Id, MatchState.PossibleMatchReview, now.AddHours(-2), now.AddHours(-2), 89.99m, "CAD", FreshnessState.Recent, EvidenceState.Partial, HistoryAvailability.Partial, new Dictionary<string, string> { ["voltage"] = "20V", ["toolOnly"] = "true", ["batteryCount"] = "0" }, new Dictionary<string, string> { ["mpn"] = "MF-20-TOOL" }, retailerSku: "DM-MF20TOOL", seller: "Demo Market Lab", isMarketplaceSeller: false, condition: ProductCondition.New, packQuantity: 1, bundleContents: "Tool only; batteries and charger excluded");
        var listingD = RetailerListing.Create(productD.Id, demoNorth.Id, "DEMO-D-NORTH", productD.Title, "https://demo.local/products/northstar-65-oled-tv", policy.Id, MatchState.Confirmed, now.AddDays(-3), now.AddDays(-3), 1399.99m, "CAD", FreshnessState.Stale, EvidenceState.Partial, HistoryAvailability.Partial, productD.VariantAttributes, new Dictionary<string, string> { ["gtin"] = "000000000004" }, "DN-NS65");
        var listingE = RetailerListing.Create(productE.Id, demoMarket.Id, "DEMO-E-TOOL", productE.Title, "https://demo.local/products/ridgeway-20v-drill-tool-only", policy.Id, MatchState.PossibleMatchReview, now.AddHours(-1), now.AddHours(-1), 79.99m, "CAD", FreshnessState.Recent, EvidenceState.Partial, HistoryAvailability.Unavailable, productE.VariantAttributes, new Dictionary<string, string> { ["gtin"] = "000000000005" }, retailerSku: "DM-RW20TOOL", seller: "Demo Market Lab", isMarketplaceSeller: false);
        var listingF = RetailerListing.Create(productF.Id, demoMarket.Id, "DEMO-F-UNKNOWN", productF.Title, "https://demo.local/products/mapleforge-compact-impact-driver", policy.Id, MatchState.NoMatch, now.AddHours(-2), now.AddHours(-2), 119.99m, "CAD", FreshnessState.Recent, EvidenceState.Unknown, HistoryAvailability.Unavailable, productF.VariantAttributes, new Dictionary<string, string> { ["gtin"] = "000000000006" }, "DM-MFID");

        db.AddRange(listingA1, listingA2, listingB, listingC1, listingC2, listingD, listingE, listingF);
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
    }
}
