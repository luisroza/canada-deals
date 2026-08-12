using CanadaDeals.Domain.Catalog;

namespace CanadaDeals.Domain.Accounts;

public sealed class SavedProduct
{
    private SavedProduct() { }

    public Guid UserId { get; private set; }
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    public static SavedProduct Create(Guid userId, Guid productId, DateTimeOffset createdAt)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A saved product requires a user.", nameof(userId));
        if (productId == Guid.Empty) throw new ArgumentException("A saved product requires a product.", nameof(productId));

        return new SavedProduct
        {
            UserId = userId,
            ProductId = productId,
            CreatedAt = createdAt
        };
    }
}
