using System.Buffers.Binary;
using System.Security.Cryptography;

namespace CanadaDeals.Api.Services;

public sealed record ProductImageInspection(string ContentType, int Width, int Height, string Sha256);

public static class ProductImageFileInspector
{
    public static bool TryInspect(string claimedContentType, byte[] content, out ProductImageInspection? inspection)
    {
        inspection = null;
        var contentType = claimedContentType.Trim().ToLowerInvariant();
        var dimensions = contentType switch
        {
            "image/png" => PngDimensions(content),
            "image/jpeg" => JpegDimensions(content),
            "image/webp" => WebpDimensions(content),
            _ => null
        };
        if (dimensions is null) return false;

        inspection = new ProductImageInspection(
            contentType,
            dimensions.Value.Width,
            dimensions.Value.Height,
            Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant());
        return true;
    }

    private static (int Width, int Height)? PngDimensions(byte[] bytes)
    {
        ReadOnlySpan<byte> signature = [137, 80, 78, 71, 13, 10, 26, 10];
        if (bytes.Length < 24 || !bytes.AsSpan(0, 8).SequenceEqual(signature) ||
            !bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8)) return null;
        return Valid(BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4)), BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)));
    }

    private static (int Width, int Height)? JpegDimensions(byte[] bytes)
    {
        if (bytes.Length < 4 || bytes[0] != 0xff || bytes[1] != 0xd8) return null;
        var offset = 2;
        while (offset + 3 < bytes.Length)
        {
            if (bytes[offset++] != 0xff) return null;
            while (offset < bytes.Length && bytes[offset] == 0xff) offset++;
            if (offset >= bytes.Length) return null;
            var marker = bytes[offset++];
            if (marker is 0xd8 or 0xd9) continue;
            if (marker is 0x01 or >= 0xd0 and <= 0xd7) continue;
            if (offset + 2 > bytes.Length) return null;
            var length = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset, 2));
            if (length < 2 || offset + length > bytes.Length) return null;
            if (marker is >= 0xc0 and <= 0xc3 or >= 0xc5 and <= 0xc7 or >= 0xc9 and <= 0xcb or >= 0xcd and <= 0xcf)
            {
                if (length < 7) return null;
                return Valid(BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset + 3, 2)), BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset + 5, 2)));
            }
            offset += length;
        }
        return null;
    }

    private static (int Width, int Height)? WebpDimensions(byte[] bytes)
    {
        if (bytes.Length < 30 || !bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) || !bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8)) return null;
        var chunk = bytes.AsSpan(12, 4);
        if (chunk.SequenceEqual("VP8X"u8))
            return Valid(1 + UInt24(bytes, 24), 1 + UInt24(bytes, 27));
        if (chunk.SequenceEqual("VP8L"u8) && bytes.Length >= 25 && bytes[20] == 0x2f)
        {
            var width = 1 + bytes[21] + ((bytes[22] & 0x3f) << 8);
            var height = 1 + ((bytes[22] & 0xc0) >> 6) + (bytes[23] << 2) + ((bytes[24] & 0x0f) << 10);
            return Valid(width, height);
        }
        if (chunk.SequenceEqual("VP8 "u8) && bytes.Length >= 30 && bytes[23] == 0x9d && bytes[24] == 0x01 && bytes[25] == 0x2a)
            return Valid(BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(26, 2)) & 0x3fff,
                BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(28, 2)) & 0x3fff);
        return null;
    }

    private static int UInt24(byte[] bytes, int offset) => bytes[offset] | bytes[offset + 1] << 8 | bytes[offset + 2] << 16;
    private static (int Width, int Height)? Valid(int width, int height) => width > 0 && height > 0 ? (width, height) : null;
}
