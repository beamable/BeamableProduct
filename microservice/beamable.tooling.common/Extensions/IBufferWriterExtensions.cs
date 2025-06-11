using System.Buffers;

namespace microservice.Extensions;

public static class IBufferWriterExtensions
{
    
    /// <summary>
    /// The regular .Write() method can throw an argument exception when the given byte array is
    /// large enough. While I was looking into the ZLogger implementation, I saw that they do not use
    /// the regular .Write() method, but instead do the copy directly. 
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="bytes"></param>
    public static void BeamWrite(this IBufferWriter<byte> writer, byte[] bytes)
    {
        Span<byte> span = writer.GetSpan(bytes.Length);
        bytes.CopyTo(span);
        writer.Advance(bytes.Length);
    }
}