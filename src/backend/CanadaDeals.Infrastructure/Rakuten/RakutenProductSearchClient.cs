using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace CanadaDeals.Infrastructure.Rakuten;

public sealed class RakutenProductSearchClient(RakutenAuthenticatedClient client) : IRakutenProductSearchClient
{
    public async Task<RakutenProductPage> GetPageAsync(
        string advertiserMid,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(advertiserMid)) throw new ArgumentException("Rakuten advertiser MID is required.", nameof(advertiserMid));
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        if (pageSize is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(pageSize));

        var path = $"productsearch/1.0?mid={Uri.EscapeDataString(advertiserMid)}&language=en_US&max={pageSize}&pagenumber={pageNumber}";
        using var response = await client.SendAsync(() => new HttpRequestMessage(HttpMethod.Get, path), cancellationToken);
        RakutenProviderResponse.EnsureSuccess(response, "RAKUTEN_PRODUCT_SEARCH");

        XDocument document;
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 5_000_000,
                MaxCharactersFromEntities = 0,
                Async = true
            });
            document = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken);
        }
        catch (Exception exception) when (exception is XmlException or InvalidOperationException)
        {
            throw new RakutenProviderException(RakutenFailureKind.MalformedResponse, "RAKUTEN_PRODUCT_SEARCH_XML_MALFORMED");
        }

        var root = document.Root;
        if (root is null) throw new RakutenProviderException(RakutenFailureKind.MalformedResponse, "RAKUTEN_PRODUCT_SEARCH_RESPONSE_EMPTY");
        var totalMatches = Int(root, "TotalMatches");
        var totalPages = Int(root, "TotalPages");
        var returnedPage = Int(root, "PageNumber");
        if (returnedPage <= 0) returnedPage = pageNumber;

        var products = root.Elements().Where(element => element.Name.LocalName == "item")
            .Select(ParseProduct)
            .Where(product => product is not null)
            .Cast<RakutenProductRecord>()
            .ToList();
        return new RakutenProductPage(totalMatches, totalPages, returnedPage, products);
    }

    private static RakutenProductRecord? ParseProduct(XElement item)
    {
        var mid = Value(item, "mid");
        var name = Value(item, "merchantname");
        var productName = Value(item, "productname");
        if (string.IsNullOrWhiteSpace(mid) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(productName)) return null;

        var priceElement = Element(item, "price");
        var saleElement = Element(item, "saleprice");
        var category = Element(item, "category");
        var description = Element(item, "description");
        return new RakutenProductRecord(
            mid, name, Value(item, "linkid"), ParseCreatedOn(Value(item, "createdon")), Value(item, "sku"), productName,
            category is null ? null : Value(category, "primary"), category is null ? null : Value(category, "secondary"),
            Decimal(priceElement?.Value), Attribute(priceElement, "currency"), Decimal(saleElement?.Value), Attribute(saleElement, "currency"),
            Value(item, "upccode"), description is null ? null : Value(description, "short"),
            description is null ? null : Value(description, "long"), Value(item, "keywords"), Value(item, "linkurl"), Value(item, "imageurl"));
    }

    private static XElement? Element(XElement root, string localName) => root.Elements().FirstOrDefault(element => element.Name.LocalName == localName);
    private static string? Value(XElement root, string localName) => Element(root, localName)?.Value.Trim() is { Length: > 0 } value ? value : null;
    private static string? Attribute(XElement? element, string localName) => element?.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == localName)?.Value.Trim() is { Length: > 0 } value ? value : null;
    private static int Int(XElement root, string localName) => int.TryParse(Value(root, localName), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
    private static decimal? Decimal(string? value) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) && parsed > 0 ? parsed : null;
    private static DateTimeOffset? ParseCreatedOn(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var formats = new[] { "yyyy-MM-dd/HH:mm:ss", "yyyy-MM-dd'T'HH:mm:ssK", "O" };
        return DateTimeOffset.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ? parsed : null;
    }
}
