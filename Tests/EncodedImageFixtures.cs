using System.Buffers.Binary;
using System.Text;

internal static class EncodedImageFixtures
{
    public static byte[] WithExifOrientation(byte[] jpeg, byte orientation)
    {
        byte[] app1 = [0xFF, 0xE1, 0, 34, (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0, 0,
            (byte)'I', (byte)'I', 42, 0, 8, 0, 0, 0, 1, 0, 0x12, 1, 3, 0, 1, 0, 0, 0,
            orientation, 0, 0, 0, 0, 0, 0, 0];
        return [.. jpeg[..2], .. app1, .. jpeg[2..]];
    }

    public static byte[] AnimatedPng(byte[] first, byte[] second)
    {
        byte[] header = Chunks(first, "IHDR").Single();
        byte[] compressedFirst = Chunks(first, "IDAT").SelectMany(x => x).ToArray();
        byte[] compressedSecond = Chunks(second, "IDAT").SelectMany(x => x).ToArray();
        using var stream = new MemoryStream();
        stream.Write(first.AsSpan(0, 8));
        Chunk(stream, "IHDR", header);
        byte[] animation = new byte[8]; Write(animation, 0, 2);
        Chunk(stream, "acTL", animation);
        Chunk(stream, "fcTL", FrameControl(header, 0));
        Chunk(stream, "IDAT", compressedFirst);
        Chunk(stream, "fcTL", FrameControl(header, 1));
        byte[] frameData = new byte[4 + compressedSecond.Length]; Write(frameData, 0, 2);
        compressedSecond.CopyTo(frameData, 4);
        Chunk(stream, "fdAT", frameData);
        Chunk(stream, "IEND", []);
        return stream.ToArray();
    }

    private static byte[] FrameControl(byte[] header, uint sequence)
    {
        byte[] frame = new byte[26]; Write(frame, 0, sequence);
        header.AsSpan(0, 8).CopyTo(frame.AsSpan(4));
        frame[21] = 1; frame[23] = 10; // Delay 1/10 second; source blend, no disposal.
        return frame;
    }

    private static IEnumerable<byte[]> Chunks(byte[] png, string type)
    {
        for (int offset = 8; offset + 12 <= png.Length;)
        {
            int length = checked((int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(offset, 4)));
            if (Encoding.ASCII.GetString(png, offset + 4, 4) == type)
                yield return png.AsSpan(offset + 8, length).ToArray();
            offset += 12 + length;
        }
    }

    private static void Chunk(Stream stream, string type, byte[] data)
    {
        byte[] length = new byte[4]; Write(length, 0, (uint)data.Length); stream.Write(length);
        byte[] payload = [.. Encoding.ASCII.GetBytes(type), .. data]; stream.Write(payload);
        uint crc = uint.MaxValue;
        foreach (byte value in payload)
        {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
        }
        byte[] checksum = new byte[4]; Write(checksum, 0, ~crc); stream.Write(checksum);
    }

    private static void Write(byte[] buffer, int offset, uint value) => BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(offset, 4), value);
}
