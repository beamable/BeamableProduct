using System.Text.Json;
using cli.Content;
using NUnit.Framework;

namespace tests.ContentChecksumTests;

/// <summary>
/// Guards the canonical form that <see cref="ContentService.CalculateChecksum(string)"/> hashes.
/// The hash is published as the manifest checksum, so these are wire-format tests.
/// </summary>
public class ContentChecksumTest
{
	private static ContentFile FileWithProperties(string propertiesJson) => new()
	{
		Id = "items.sword",
		Properties = JsonSerializer.Deserialize<JsonElement>(propertiesJson),
		Tags = JsonSerializer.Deserialize<JsonElement>("[]"),
	};

	private static string ChecksumOf(string propertiesJson) =>
		ContentService.CalculateChecksum(FileWithProperties(propertiesJson));

	[Test]
	public void Checksum_IgnoresTopLevelFieldOrder()
	{
		var a = ChecksumOf("""{"damage":{"data":5},"armor":{"data":2},"name":{"data":"sword"}}""");
		var b = ChecksumOf("""{"name":{"data":"sword"},"armor":{"data":2},"damage":{"data":5}}""");

		Assert.That(b, Is.EqualTo(a));
	}

	[Test]
	public void Checksum_IgnoresNestedFieldOrder()
	{
		var a = ChecksumOf("""{"stats":{"data":{"agility":1,"strength":9,"luck":3}}}""");
		var b = ChecksumOf("""{"stats":{"data":{"luck":3,"agility":1,"strength":9}}}""");

		Assert.That(b, Is.EqualTo(a));
	}

	[Test]
	public void Checksum_IsStableForKeysDifferingOnlyInCase()
	{
		// Without an Ordinal tiebreak these sort differently despite carrying identical values.
		var a = ChecksumOf("""{"Damage":{"data":1},"damage":{"data":2}}""");
		var b = ChecksumOf("""{"damage":{"data":2},"Damage":{"data":1}}""");

		Assert.That(b, Is.EqualTo(a));
	}

	[Test]
	public void Checksum_RespectsArrayOrder()
	{
		// Array order is meaningful content, not incidental formatting, so it must NOT be normalized away.
		var a = ChecksumOf("""{"slots":{"data":["head","chest"]}}""");
		var b = ChecksumOf("""{"slots":{"data":["chest","head"]}}""");

		Assert.That(b, Is.Not.EqualTo(a));
	}

	[Test]
	public void Checksum_DistinguishesDifferentValues()
	{
		var a = ChecksumOf("""{"damage":{"data":5}}""");
		var b = ChecksumOf("""{"damage":{"data":6}}""");

		Assert.That(b, Is.Not.EqualTo(a));
	}

	[Test]
	public void SerializationOptions_EscapeNonAsciiAsTheyAlwaysHave()
	{
		// Pinning the encoder must not have changed the bytes we hash.
		var json = JsonSerializer.Serialize(
			JsonSerializer.Deserialize<JsonElement>("""{"name":"Café & Co"}"""),
			ContentService.GetContentFileSerializationOptions(false));

		// JavaScriptEncoder.Default escapes non-ASCII and HTML-sensitive characters, as it always has.
		Assert.That(json, Is.EqualTo("""{"name":"Caf\u00E9 \u0026 Co"}"""));
	}

	[Test]
	public void Checksum_PreservesRawNumberText()
	{
		// Numbers are replayed as written, keeping the float-versus-int distinction Unity schemas rely on.
		var a = ChecksumOf("""{"price":{"data":1.0}}""");
		var b = ChecksumOf("""{"price":{"data":1}}""");

		Assert.That(b, Is.Not.EqualTo(a));
	}
}
