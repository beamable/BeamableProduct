using System.Text;
using ZstdSharp;

namespace Beamable.Server;

public static class SocketCompression
{
    public const int CompressionThresholdBytes = 200;
    public const string CodecZstd = "zstd";
    public const byte BinaryPrefixZstd = 0x01;

    public static readonly string[] SupportedCodecs = { CodecZstd };

    public static byte[] Compress(string data, string codec)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        return codec switch
        {
            CodecZstd => CompressZstd(bytes),
            _ => throw new ArgumentException($"Unknown codec: {codec}")
        };
    }

    public static string Decompress(byte[] data)
    {
        if (data.Length == 0)
            return "";

        var prefix = data[0];
        var payload = new ReadOnlySpan<byte>(data, 1, data.Length - 1);

        return prefix switch
        {
            BinaryPrefixZstd => DecompressZstd(payload),
            _ => throw new ArgumentException($"Unknown compression prefix: {prefix}")
        };
    }

    private static byte[] CompressZstd(byte[] data)
    {
        using var compressor = new Compressor();
        var compressed = compressor.Wrap(data);
        var result = new byte[compressed.Length + 1];
        result[0] = BinaryPrefixZstd;
        compressed.CopyTo(result.AsSpan(1));
        return result;
    }

    private static string DecompressZstd(ReadOnlySpan<byte> data)
    {
        using var decompressor = new Decompressor();
        var decompressed = decompressor.Unwrap(data);
        return Encoding.UTF8.GetString(decompressed);
    }

    public static bool ShouldCompress(string message)
    {
        return Encoding.UTF8.GetByteCount(message) >= CompressionThresholdBytes;
    }

    public static string GetPreferredCodec(string[] negotiatedCodecs)
    {
        if (negotiatedCodecs != null && Array.IndexOf(negotiatedCodecs, CodecZstd) >= 0)
            return CodecZstd;
        return null;
    }
}
