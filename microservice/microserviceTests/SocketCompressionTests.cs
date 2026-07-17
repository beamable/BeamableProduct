using Beamable.Server;
using NUnit.Framework;
using System.Text;

namespace microserviceTests;

public class SocketCompressionTests
{
	[Test]
	public void CompressAndDecompress_RoundTrips()
	{
		var original = new string('x', 500) + """{"id":1,"status":200,"body":{"data":"hello"}}""";
		var compressed = SocketCompression.Compress(original, SocketCompression.CodecZstd);
		var decompressed = SocketCompression.Decompress(compressed);
		Assert.AreEqual(original, decompressed);
	}

	[Test]
	public void Compress_HasZstdPrefix()
	{
		var data = new string('a', 300);
		var compressed = SocketCompression.Compress(data, SocketCompression.CodecZstd);
		Assert.AreEqual(SocketCompression.BinaryPrefixZstd, compressed[0]);
	}

	[Test]
	public void Compress_ProducesSmallerOutput()
	{
		var original = new string('a', 1000);
		var compressed = SocketCompression.Compress(original, SocketCompression.CodecZstd);
		Assert.Less(compressed.Length, Encoding.UTF8.GetByteCount(original));
	}

	[Test]
	public void Decompress_EmptyArray_ReturnsEmptyString()
	{
		var result = SocketCompression.Decompress(System.Array.Empty<byte>());
		Assert.AreEqual("", result);
	}

	[TestCase(100, false)]
	[TestCase(200, true)]
	[TestCase(1000, true)]
	public void ShouldCompress_RespectsThreshold(int length, bool expected)
	{
		var message = new string('x', length);
		Assert.AreEqual(expected, SocketCompression.ShouldCompress(message));
	}

	[Test]
	public void GetPreferredCodec_WithZstd_ReturnsZstd()
	{
		var codec = SocketCompression.GetPreferredCodec(new[] { "zstd" });
		Assert.AreEqual(SocketCompression.CodecZstd, codec);
	}

	[Test]
	public void GetPreferredCodec_WithNull_ReturnsNull()
	{
		var codec = SocketCompression.GetPreferredCodec(null);
		Assert.IsNull(codec);
	}

	[Test]
	public void GetPreferredCodec_WithUnknown_ReturnsNull()
	{
		var codec = SocketCompression.GetPreferredCodec(new[] { "gzip" });
		Assert.IsNull(codec);
	}

	[Test]
	public void GetPreferredCodec_WithEmpty_ReturnsNull()
	{
		var codec = SocketCompression.GetPreferredCodec(System.Array.Empty<string>());
		Assert.IsNull(codec);
	}
}
