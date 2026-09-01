using CanadaDeals.Domain.Retailers;

namespace CanadaDeals.Domain.Accounts;

public sealed class SavedOffer
{
    private SavedOffer() { }

    public Guid UserId { get; private set; }
    public Guid RetailerListingId { get; private set; }
    public RetailerListing RetailerListing { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    public static SavedOffer Create(Guid userId, Guid retailerListingId, DateTimeOffset createdAt)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A saved offer requires a user.", nameof(userId));
        if (retailerListingId == Guid.Empty) throw new ArgumentException("A saved offer requires a retailer listing.", nameof(retailerListingId));

        return new SavedOffer
        {
            UserId = userId,
            RetailerListingId = retailerListingId,
            CreatedAt = createdAt
        };
    }
}
