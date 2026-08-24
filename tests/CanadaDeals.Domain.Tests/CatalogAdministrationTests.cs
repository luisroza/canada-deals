using CanadaDeals.Domain.Catalog;
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
