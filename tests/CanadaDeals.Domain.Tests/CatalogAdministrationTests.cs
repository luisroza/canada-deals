using CanadaDeals.Domain.Catalog;
using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Retailers;

namespace CanadaDeals.Domain.Tests;

public sealed class CatalogAdministrationTests
{
    [Fact]
    public void Category_can_be_created_inactive_and_reactivated_without_changing_slug()
    {
        var category = Category.Create("Home Audio", "home-audio", enabled: false);

        category.UpdateAdministrativeName("Audio");
        category.SetEnabled(true);

        Assert.Equal("Audio", category.Name);
        Assert.Equal("home-audio", category.Slug);
        Assert.True(category.IsEnabled);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Category_rejects_blank_names(string name)
    {
        Assert.Throws<ArgumentException>(() => Category.Create(name, "valid-slug"));
    }

    [Fact]
    public void Brand_can_be_created_inactive_and_reactivated_without_changing_slug()
    {
        var brand = Brand.Create("North Star", "north-star", enabled: false);

        brand.UpdateAdministrativeName("NorthStar");
        brand.SetEnabled(true);

        Assert.Equal("NorthStar", brand.Name);
        Assert.Equal("north-star", brand.Slug);
        Assert.True(brand.IsEnabled);
    }

    [Fact]
    public void Retailer_listing_stops_being_publishable_at_its_optional_valid_until_time()
    {
        var observedAt = new DateTimeOffset(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);
        var validUntil = observedAt.AddHours(2);
        var listing = RetailerListing.Create(
            Guid.NewGuid(), Guid.NewGuid(), "external-1", "Controlled offer", "https://example.test/offer",
            Guid.NewGuid(), MatchState.Confirmed, observedAt, observedAt, 99.99m, "CAD",
            FreshnessState.Recent, EvidenceState.Partial, HistoryAvailability.Unavailable,
            offerValidUntil: validUntil);

        Assert.True(listing.IsPublishedAt(validUntil.AddTicks(-1)));
        Assert.False(listing.IsPublishedAt(validUntil));

        listing.SetEnabled(false);
        Assert.False(listing.IsPublishedAt(observedAt));
    }

    [Fact]
    public void Retailer_can_be_created_inactive_and_reactivated_without_changing_key()
    {
        var retailer = Retailer.Create("sample-store", "Sample Store", enabled: false);

        retailer.UpdateAdministrativeName("Sample Canada");
        retailer.SetEnabled(true);

        Assert.Equal("Sample Canada", retailer.Name);
        Assert.Equal("sample-store", retailer.Key);
        Assert.Equal("CA", retailer.CountryCode);
        Assert.True(retailer.IsEnabled);
    }

    [Fact]
    public void Retailer_rejects_blank_names()
    {
        Assert.Throws<ArgumentException>(() => Retailer.Create("valid-key", " "));
    }
}
