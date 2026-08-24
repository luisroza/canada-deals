namespace CanadaDeals.Domain.Retailers;

public sealed class StoreBannerAsset
{
    public const int MaxFileNameLength = 180;
    public const int MaxContentTypeLength = 40;
    public const int MaxBytes = 2 * 1024 * 1024;
    public const string PublicPathPrefix = "/api/v1/store-banner-assets/";

    private StoreBannerAsset() { }

    public Guid Id { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public byte[] Content { get; private set; } = [];
    public Guid UploadedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public string PublicPath => $"{PublicPathPrefix}{Id:D}";

    public static StoreBannerAsset Create(
        string fileName,
        string contentType,
        byte[] content,
        Guid uploadedByUserId,
        DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > MaxFileNameLength)
            throw new ArgumentException($"A file name of at most {MaxFileNameLength} characters is required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(contentType) || contentType.Length > MaxContentTypeLength)
            throw new ArgumentException("A supported content type is required.", nameof(contentType));
        if (content.Length is 0 or > MaxBytes)
            throw new ArgumentOutOfRangeException(nameof(content), $"Banner images must be between 1 byte and {MaxBytes / 1024 / 1024} MB.");
        if (uploadedByUserId == Guid.Empty) throw new ArgumentException("The uploading administrator is required.", nameof(uploadedByUserId));

        return new StoreBannerAsset
        {
            Id = Guid.NewGuid(),
            FileName = fileName.Trim(),
            ContentType = contentType.Trim().ToLowerInvariant(),
            Content = content,
            UploadedByUserId = uploadedByUserId,
            CreatedAt = createdAt
        };
    }

    public static bool IsReviewedPath(string? path) =>
        !string.IsNullOrWhiteSpace(path) &&
        ((path.StartsWith("/store-banners/", StringComparison.Ordinal) && !path.Contains("..", StringComparison.Ordinal)) ||
         (path.StartsWith(PublicPathPrefix, StringComparison.Ordinal) &&
          Guid.TryParse(path[PublicPathPrefix.Length..], out _)));
}
