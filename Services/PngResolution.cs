using System.Buffers.Binary;

namespace HappyColorSplit.Services;

internal static class PngResolution
{
    public static byte[] WithDpi(byte[] png, int dpi)
    {
        if (dpi is < 1 or > 2400) throw new ArgumentOutOfRangeException(nameof(dpi));
        // Skia's freshly encoded PNG has IHDR first and no pHYs. Insert physical pixel density.
        byte[] result = new byte[png.Length + 21];
        png.AsSpan(0, 33).CopyTo(result);
        var chunk = result.AsSpan(33, 21);
        BinaryPrimitives.WriteUInt32BigEndian(chunk, 9);
        "pHYs"u8.CopyTo(chunk[4..]);
        uint pixelsPerMeter = (uint)Math.Round(dpi / 0.0254, MidpointRounding.AwayFromZero);
        BinaryPrimitives.WriteUInt32BigEndian(chunk[8..], pixelsPerMeter);
        BinaryPrimitives.WriteUInt32BigEndian(chunk[12..], pixelsPerMeter);
        chunk[16] = 1;
        uint crc = uint.MaxValue;
        foreach (byte value in chunk.Slice(4, 13))
        {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
        }
        BinaryPrimitives.WriteUInt32BigEndian(chunk[17..], ~crc);
        png.AsSpan(33).CopyTo(result.AsSpan(54));
        return result;
    }
}
