using System.Collections.Generic;
using System.Text.Json;
using cli.Content;
using NUnit.Framework;

namespace tests.ContentChecksumTests;

/// <summary>
/// Guards the content id ordering that <see cref="SortedSnapshotConverter"/> writes.
/// Snapshot bytes are not hashed, so a regression here costs diff churn rather than
/// wrong status -- but stable diffs are the whole reason the sort exists.
/// </summary>
public class SnapshotOrderingTest
{
	// The payload is irrelevant here; only the position of each id in the output is under test.
	private static ContentFileSnapshot Entry() => new()
	{
		Properties = JsonSerializer.Deserialize<JsonElement>("""{"name":{"data":"x"}}"""),
		Tags = JsonSerializer.Deserialize<JsonElement>("[]"),
		Checksum = "unused",
	};

	private static string Write(params string[] idsInInsertionOrder)
	{
		var contents = new Dictionary<string, ContentFileSnapshot>();
		foreach (var id in idsInInsertionOrder)
		{
			contents[id] = Entry();
		}

		return JsonSerializer.Serialize(contents, ContentService.GetContentFileSerializationOptions());
	}

	[Test]
	public void Snapshot_IgnoresInsertionOrder()
	{
		var a = Write("items.sword", "items.axe", "currency.gold");
		var b = Write("currency.gold", "items.sword", "items.axe");

		Assert.That(b, Is.EqualTo(a));
	}

	[Test]
	public void Snapshot_IsDeterministicForIdsDifferingOnlyInCase()
	{
		// OrderBy is stable over input ordering, so without the Ordinal tiebreak these two
		// insertion orders each keep their own arrival order and the diff gains a phantom pair.
		var a = Write("items.Sword", "items.sword");
		var b = Write("items.sword", "items.Sword");

		Assert.That(b, Is.EqualTo(a));
	}

	[Test]
	public void Snapshot_SortsCaseInsensitivelySoCapitalsDoNotClump()
	{
		// The reason the comparer is OrdinalIgnoreCase rather than Ordinal: plain Ordinal
		// would group every capitalized id ahead of every lowercase one.
		var written = Write("beta", "Alpha", "gamma");

		Assert.That(written.IndexOf("Alpha"), Is.LessThan(written.IndexOf("beta")));
		Assert.That(written.IndexOf("beta"), Is.LessThan(written.IndexOf("gamma")));
	}
}
