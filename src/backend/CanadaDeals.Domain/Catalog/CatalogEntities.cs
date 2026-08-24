using System.Text.Json;
using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Search;

namespace CanadaDeals.Domain.Catalog;

public sealed class Brand
{
    private Brand() { }
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;

    public static Brand Create(string name, string slug) => new()
    {
        Id = Guid.NewGuid(), Name = name, Slug = slug
    };
}

public sealed class Category
{
    private Category() { }
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; } = true;

    public static Category Create(string name, string slug, bool enabled = true)
    {
        ValidateName(name);
        ValidateSlug(slug);
        return new Category
        {
            Id = Guid.NewGuid(), Name = name.Trim(), Slug = slug.Trim(), IsEnabled = enabled
        };
    }

    public void UpdateAdministrativeName(string name)
    {
        ValidateName(name);
        Name = name.Trim();
    }

    public void SetEnabled(bool enabled) => IsEnabled = enabled;

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120)
            throw new ArgumentException("A category name of at most 120 characters is required.", nameof(name));
    }

    private static void ValidateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug) || slug.Trim().Length > 140)
            throw new ArgumentException("A category slug of at most 140 characters is required.", nameof(slug));
    }
}

public sealed class Product
{
    private Product() { }
    public Guid Id { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? ModelNumber { get; private set; }
    public string? ManufacturerPartNumber { get; private set; }
    public string? Gtin { get; private set; }
    public Guid BrandId { get; private set; }
    public Brand Brand { get; private set; } = null!;
    public Guid CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;
    public string VariantAttributesJson { get; private set; } = "{}";
    public string SearchDocument { get; private set; } = string.Empty;
    public string? NormalizedModelNumber { get; private set; }
    public string? NormalizedManufacturerPartNumber { get; private set; }

    public static Product Create(
        string slug,
        string title,
        Brand brand,
        Category category,
        string? modelNumber = null,
        string? manufacturerPartNumber = null,
        string? gtin = null,
        IReadOnlyDictionary<string, string>? variantAttributes = null) => new()
    {
        Id = Guid.NewGuid(),
        Slug = slug,
        Title = title,
        Brand = brand,
        BrandId = brand.Id,
        Category = category,
        CategoryId = category.Id,
        ModelNumber = modelNumber,
        ManufacturerPartNumber = manufacturerPartNumber,
        Gtin = gtin,
        SearchDocument = string.Join(' ', new[] { title, brand.Name, category.Name, modelNumber, manufacturerPartNumber, gtin }.Where(x => !string.IsNullOrWhiteSpace(x))),
        NormalizedModelNumber = string.IsNullOrWhiteSpace(modelNumber) ? null : DiscoveryRules.NormalizeIdentifier(modelNumber),
        NormalizedManufacturerPartNumber = string.IsNullOrWhiteSpace(manufacturerPartNumber) ? null : DiscoveryRules.NormalizeIdentifier(manufacturerPartNumber),
        VariantAttributesJson = JsonSerializer.Serialize(variantAttributes ?? new Dictionary<string, string>())
    };

    public IReadOnlyDictionary<string, string> VariantAttributes =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(VariantAttributesJson) ?? new Dictionary<string, string>();

    public void UpdateAdministrativeIdentity(
        string slug,
        string title,
        Brand brand,
        Category category,
        string? modelNumber,
        string? manufacturerPartNumber,
        string? gtin,
        IReadOnlyDictionary<string, string>? variantAttributes)
    {
        var replacement = Create(slug, title, brand, category, modelNumber, manufacturerPartNumber, gtin, variantAttributes);
        Slug = replacement.Slug;
        Title = replacement.Title;
        Brand = brand;
        BrandId = brand.Id;
        Category = category;
        CategoryId = category.Id;
        ModelNumber = replacement.ModelNumber;
        ManufacturerPartNumber = replacement.ManufacturerPartNumber;
        Gtin = replacement.Gtin;
        SearchDocument = replacement.SearchDocument;
        NormalizedModelNumber = replacement.NormalizedModelNumber;
        NormalizedManufacturerPartNumber = replacement.NormalizedManufacturerPartNumber;
        VariantAttributesJson = replacement.VariantAttributesJson;
    }

    public void RefreshSearchDocument()
    {
        SearchDocument = string.Join(' ', new[] { Title, Brand.Name, Category.Name, ModelNumber, ManufacturerPartNumber, Gtin }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
