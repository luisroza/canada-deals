using System.Net;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace CanadaDeals.Infrastructure.Catalogs;

internal static class CatalogHttp
{
    public const int MaximumJsonBytes = 4 * 1024 * 1024;
    public const int MaximumXmlBytes = 4 * 1024 * 1024;

    public static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        Func<HttpRequestMessage> requestFactory,
        string providerCode,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var request = requestFactory();
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.IsSuccessStatusCode) return response;

            var retryAfter = response.Headers.RetryAfter?.Delta;
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                response.Dispose();
                if (attempt == 0 && retryAfter.HasValue && retryAfter.Value > TimeSpan.Zero && retryAfter.Value <= TimeSpan.FromSeconds(5))
                {
                    await Task.Delay(retryAfter.Value, cancellationToken);
                    continue;
                }
                throw new CatalogProviderException(CatalogFailureKind.RateLimited, $"{providerCode}_RATE_LIMITED", retryAfter);
            }

            if ((int)response.StatusCode >= 500)
            {
                response.Dispose();
                if (attempt == 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
                    continue;
                }
                throw new CatalogProviderException(CatalogFailureKind.ProviderUnavailable, $"{providerCode}_UNAVAILABLE");
            }

            var status = response.StatusCode;
            response.Dispose();
            if (status == HttpStatusCode.Unauthorized)
                throw new CatalogProviderException(CatalogFailureKind.Authentication, $"{providerCode}_AUTHENTICATION_FAILED");
            if (status == HttpStatusCode.Forbidden)
                throw new CatalogProviderException(CatalogFailureKind.Authorization, $"{providerCode}_ACCESS_DENIED");
            throw new CatalogProviderException(CatalogFailureKind.InvalidRequest, $"{providerCode}_REQUEST_REJECTED");
        }
        throw new CatalogProviderException(CatalogFailureKind.ProviderUnavailable, $"{providerCode}_UNAVAILABLE");
    }

    public static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, string providerCode, CancellationToken cancellationToken)
    {
        await using var stream = await BoundedStreamAsync(response, MaximumJsonBytes, providerCode, cancellationToken);
        try
        {
            return await JsonDocument.ParseAsync(stream, new JsonDocumentOptions { MaxDepth = 32 }, cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new CatalogProviderException(CatalogFailureKind.MalformedResponse, $"{providerCode}_MALFORMED_RESPONSE", innerException: exception);
        }
    }

    public static async Task<XDocument> ReadXmlAsync(HttpResponseMessage response, string providerCode, CancellationToken cancellationToken)
    {
        await using var stream = await BoundedStreamAsync(response, MaximumXmlBytes, providerCode, cancellationToken);
        try
        {
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = MaximumXmlBytes,
                MaxCharactersFromEntities = 0,
                Async = true
            });
            return await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken);
        }
        catch (XmlException exception)
        {
            throw new CatalogProviderException(CatalogFailureKind.MalformedResponse, $"{providerCode}_MALFORMED_RESPONSE", innerException: exception);
        }
    }

    public static async Task<Stream> BoundedStreamAsync(
        HttpResponseMessage response,
        int maximumBytes,
        string providerCode,
        CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength is > 0 && response.Content.Headers.ContentLength > maximumBytes)
            throw new CatalogProviderException(CatalogFailureKind.PayloadTooLarge, $"{providerCode}_PAYLOAD_TOO_LARGE");
        var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        return Bound(source, maximumBytes, providerCode);
    }

    public static Stream Bound(Stream source, int maximumBytes, string providerCode) =>
        new BoundedReadStream(source, maximumBytes, providerCode);

    private sealed class BoundedReadStream(Stream inner, long limit, string providerCode) : Stream
    {
        private long _read;
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => _read; set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => Check(inner.Read(buffer, offset, count));
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            Check(await inner.ReadAsync(buffer, cancellationToken));
        private int Check(int count)
        {
            _read += count;
            if (_read > limit) throw new CatalogProviderException(CatalogFailureKind.PayloadTooLarge, $"{providerCode}_PAYLOAD_TOO_LARGE");
            return count;
        }
        protected override void Dispose(bool disposing) { if (disposing) inner.Dispose(); base.Dispose(disposing); }
        public override async ValueTask DisposeAsync() { await inner.DisposeAsync(); await base.DisposeAsync(); }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
