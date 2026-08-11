using System.Text.Json;
using CanadaDeals.Domain.Common;

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

    public static Category Create(string name, string slug) => new()
    {
        Id = Guid.NewGuid(), Name = name, Slug = slug
    };
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
        VariantAttributesJson = JsonSerializer.Serialize(variantAttributes ?? new Dictionary<string, string>())
    };

    public IReadOnlyDictionary<string, string> VariantAttributes =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(VariantAttributesJson) ?? new Dictionary<string, string>();
}
