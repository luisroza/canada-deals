namespace CanadaDeals.Domain.Catalog;

public enum ProductImageOrigin
{
    OwnerReviewed,
    MerchantApproved
}

public enum ProductImageState
{
    PendingReview,
    Active,
    Blocked,
    Archived
}

public sealed class ProductImage
{
    public const int MaxBytes = 1024 * 1024;
    public const int MaxDimension = 2400;
    public const int MaxFileNameLength = 180;
    public const string PublicPathPrefix = "/api/v1/product-images/";
    public const string DefaultPlacements = "DEAL_CARD,PRODUCT_PAGE,WISHLIST";

    private ProductImage() { }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public Guid? SourceListingId { get; private set; }
    public Guid? MerchantPolicyId { get; private set; }
    public ProductImageOrigin Origin { get; private set; }
    public ProductImageState State { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public byte[] Content { get; private set; } = [];
    public int Width { get; private set; }
    public int Height { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;
    public string? Provider { get; private set; }
    public string RightsEvidenceReference { get; private set; } = string.Empty;
    public string AllowedPlacements { get; private set; } = DefaultPlacements;
    public DateTimeOffset? EffectiveAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset LastValidatedAt { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public string PublicPath => $"{PublicPathPrefix}{Id:D}";

    public static ProductImage CreateOwnerReviewed(
        Guid productId,
        string fileName,
        string contentType,
        byte[] content,
        int width,
        int height,
        string contentHash,
        string rightsEvidenceReference,
        string allowedPlacements,
        DateTimeOffset? effectiveAt,
        DateTimeOffset? expiresAt,
        Guid uploadedByUserId,
        DateTimeOffset now,
        bool activate)
    {
        Validate(productId, fileName, contentType, content, width, height, contentHash,
            rightsEvidenceReference, allowedPlacements, effectiveAt, expiresAt, uploadedByUserId);
        if (activate && effectiveAt is not null && effectiveAt > now)
            throw new ArgumentException("A future-dated image must remain pending until its effective date.", nameof(effectiveAt));
        if (activate && expiresAt is not null && expiresAt <= now)
            throw new ArgumentException("An expired product image cannot be activated.", nameof(expiresAt));

        return new ProductImage
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Origin = ProductImageOrigin.OwnerReviewed,
            State = activate ? ProductImageState.Active : ProductImageState.PendingReview,
            FileName = fileName.Trim(),
            ContentType = contentType.Trim().ToLowerInvariant(),
            Content = content,
            Width = width,
            Height = height,
            ContentHash = contentHash.Trim().ToLowerInvariant(),
            RightsEvidenceReference = rightsEvidenceReference.Trim(),
            AllowedPlacements = NormalizePlacements(allowedPlacements),
            EffectiveAt = effectiveAt,
            ExpiresAt = expiresAt,
            LastValidatedAt = now,
            UploadedByUserId = uploadedByUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public bool CanDisplay(DateTimeOffset now, string placement) =>
        State == ProductImageState.Active &&
        (EffectiveAt is null || EffectiveAt <= now) &&
        (ExpiresAt is null || ExpiresAt > now) &&
        AllowedPlacements.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Contains(placement.Trim().ToUpperInvariant(), StringComparer.Ordinal);

    public void Activate(DateTimeOffset now)
    {
        if (EffectiveAt is not null && EffectiveAt > now)
            throw new InvalidOperationException("A product image cannot be activated before its effective date.");
        if (ExpiresAt is not null && ExpiresAt <= now)
            throw new InvalidOperationException("An expired product image cannot be activated.");
        State = ProductImageState.Active;
        LastValidatedAt = now;
        UpdatedAt = now;
    }

    public void Archive(DateTimeOffset now)
    {
        State = ProductImageState.Archived;
        UpdatedAt = now;
    }

    private static void Validate(
        Guid productId, string fileName, string contentType, byte[] content, int width, int height,
        string contentHash, string rightsEvidenceReference, string allowedPlacements,
        DateTimeOffset? effectiveAt, DateTimeOffset? expiresAt, Guid uploadedByUserId)
    {
        if (productId == Guid.Empty) throw new ArgumentException("A product is required.", nameof(productId));
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Trim().Length > MaxFileNameLength)
            throw new ArgumentException($"A file name of at most {MaxFileNameLength} characters is required.", nameof(fileName));
        if (content.Length is 0 or > MaxBytes)
            throw new ArgumentOutOfRangeException(nameof(content), "Product images must be no larger than 1 MB.");
        if (contentType is not ("image/png" or "image/jpeg" or "image/webp"))
            throw new ArgumentException("Only PNG, JPEG, and WebP product images are supported.", nameof(contentType));
        if (width is <= 0 or > MaxDimension || height is <= 0 or > MaxDimension)
            throw new ArgumentOutOfRangeException(nameof(width), $"Product images must be at most {MaxDimension} by {MaxDimension} pixels.");
        if (string.IsNullOrWhiteSpace(contentHash) || contentHash.Trim().Length != 64)
            throw new ArgumentException("A SHA-256 content hash is required.", nameof(contentHash));
        if (string.IsNullOrWhiteSpace(rightsEvidenceReference) || rightsEvidenceReference.Trim().Length > 1000)
            throw new ArgumentException("A rights evidence reference of at most 1000 characters is required.", nameof(rightsEvidenceReference));
        if (string.IsNullOrWhiteSpace(allowedPlacements))
            throw new ArgumentException("At least one allowed placement is required.", nameof(allowedPlacements));
        if (expiresAt is not null && effectiveAt is not null && expiresAt <= effectiveAt)
            throw new ArgumentException("Expiry must be later than the effective date.", nameof(expiresAt));
        if (uploadedByUserId == Guid.Empty) throw new ArgumentException("The uploading administrator is required.", nameof(uploadedByUserId));
    }

    private static string NormalizePlacements(string value)
    {
        var supported = new[] { "DEAL_CARD", "PRODUCT_PAGE", "WISHLIST" };
        var placements = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (placements.Length == 0 || placements.Any(item => !supported.Contains(item, StringComparer.Ordinal)))
            throw new ArgumentException("Allowed placements are DEAL_CARD, PRODUCT_PAGE, and WISHLIST.", nameof(value));
        return string.Join(',', placements);
    }
}
