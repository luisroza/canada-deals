using CanadaDeals.Domain.Accounts;

namespace CanadaDeals.Domain.Tests;

public sealed class SavedProductTests
{
    [Fact]
    public void Create_preserves_the_user_product_intent_and_timestamp()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var createdAt = DateTimeOffset.Parse("2026-08-11T19:30:00Z");

        var saved = SavedProduct.Create(userId, productId, createdAt);

        Assert.Equal(userId, saved.UserId);
        Assert.Equal(productId, saved.ProductId);
        Assert.Equal(createdAt, saved.CreatedAt);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Create_rejects_an_empty_relationship(bool emptyUser, bool emptyProduct)
    {
        Assert.Throws<ArgumentException>(() => SavedProduct.Create(
            emptyUser ? Guid.Empty : Guid.NewGuid(),
            emptyProduct ? Guid.Empty : Guid.NewGuid(),
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Saved_intent_contains_no_price_quality_evidence_or_ranking_fields()
    {
        var publicProperties = typeof(SavedProduct).GetProperties().Select(x => x.Name).ToArray();

        Assert.Equal(["UserId", "ProductId", "Product", "CreatedAt"], publicProperties);
        Assert.DoesNotContain(publicProperties, x => x.Contains("Price", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicProperties, x => x.Contains("Quality", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicProperties, x => x.Contains("Evidence", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicProperties, x => x.Contains("Rank", StringComparison.OrdinalIgnoreCase));
    }
}
