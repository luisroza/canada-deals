using System.Security.Cryptography;
using CanadaDeals.Domain.Catalog;

namespace CanadaDeals.Domain.Tests;

public sealed class ProductImageTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 24, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Reviewed_image_is_visible_only_when_active_current_and_allowed_for_the_placement()
    {
        var image = Create(activate: true, effectiveAt: Now.AddMinutes(-1), expiresAt: Now.AddDays(1));

        Assert.True(image.CanDisplay(Now, "DEAL_CARD"));
        Assert.False(image.CanDisplay(Now, "STORE_BANNER"));
        Assert.False(image.CanDisplay(Now.AddDays(2), "DEAL_CARD"));

        image.Archive(Now);
        Assert.False(image.CanDisplay(Now, "DEAL_CARD"));
    }

    [Fact]
    public void Pending_image_fails_closed_until_an_administrator_activates_it()
    {
        var image = Create(activate: false);
        Assert.Equal(ProductImageState.PendingReview, image.State);
        Assert.False(image.CanDisplay(Now, "PRODUCT_PAGE"));

        image.Activate(Now);
        Assert.True(image.CanDisplay(Now, "PRODUCT_PAGE"));
    }

    [Fact]
    public void Rights_evidence_and_bounded_dimensions_are_required()
    {
        Assert.Throws<ArgumentException>(() => Create(activate: true, evidence: ""));
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(activate: true, width: ProductImage.MaxDimension + 1));
    }

    private static ProductImage Create(bool activate, string evidence = "Owner-created image", int width = 800,
        DateTimeOffset? effectiveAt = null, DateTimeOffset? expiresAt = null)
    {
        byte[] bytes = [1, 2, 3];
        return ProductImage.CreateOwnerReviewed(Guid.NewGuid(), "product.png", "image/png", bytes, width, 800,
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), evidence, ProductImage.DefaultPlacements,
            effectiveAt, expiresAt, Guid.NewGuid(), Now, activate);
    }
}
