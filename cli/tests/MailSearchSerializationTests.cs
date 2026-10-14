using System.Collections.Generic;
using System.Text;
using Beamable.Common.Api.Mail;
using Beamable.Serialization;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server.Common;
using Newtonsoft.Json;
using NUnit.Framework;

namespace tests;

public class MailSearchSerializationTests
{
	public static IEnumerable<TestCaseData> OptionalValues()
	{
		yield return new TestCaseData(null, null, null).SetName("{m}_AllOmitted");
		yield return new TestCaseData(0L, 0L, 0L).SetName("{m}_ExplicitZeros");
		yield return new TestCaseData(42L, null, null).SetName("{m}_SenderOnly");
		yield return new TestCaseData(null, 25L, null).SetName("{m}_LimitOnly");
		yield return new TestCaseData(null, null, 100L).SetName("{m}_StartOnly");
		yield return new TestCaseData(42L, 25L, 100L).SetName("{m}_AllPresent");
		yield return new TestCaseData(long.MaxValue, (long)int.MaxValue, long.MaxValue).SetName("{m}_NumericBoundaries");
	}

	[TestCaseSource(nameof(OptionalValues))]
	public void CurrentAndLegacyRequestsPreserveOptionalFields(long? sender, long? limit, long? start)
	{
		var clause = Clause(sender, limit, start);
		var request = new SearchMailRequest(clause, new SearchMailRequestClause { name = "omitted", onlyCount = true });
		var current = JsonSerializable.ToJson(Utility.Convert(request));
		var legacy = Json.Serialize(request.Serialize(), new StringBuilder());
		foreach (var json in new[] { current, legacy })
		{
			var clauses = (List<object>)((ArrayDict)Json.Deserialize(json))["clauses"];
			Assert.That(clauses, Has.Count.EqualTo(2));
			var payload = (ArrayDict)clauses[0];
			AssertOptional(payload, "forSender", sender);
			AssertOptional(payload, "limit", limit);
			AssertOptional(payload, "start", start);
			Assert.That(payload["name"], Is.EqualTo("page"));
			Assert.That(payload["onlyCount"], Is.EqualTo(false));
			Assert.That((List<object>)payload["categories"], Is.EqualTo(new[] { "rewards" }));
			Assert.That((List<object>)payload["states"], Is.EqualTo(new[] { "Unread", "Read" }));
			var omitted = (ArrayDict)clauses[1];
			Assert.That(omitted["name"], Is.EqualTo("omitted"));
			Assert.That(omitted["onlyCount"], Is.EqualTo(true));
			foreach (var key in new[] { "forSender", "limit", "start", "categories", "states" })
				Assert.That(omitted.ContainsKey(key), Is.False, key);
		}
	}

	[TestCaseSource(nameof(OptionalValues))]
	public void ReflectionJsonPreservesFieldsAndExistingReadback(long? sender, long? limit, long? start)
	{
		var json = Json.Serialize(Clause(sender, limit, start), new StringBuilder());
		var payload = (ArrayDict)Json.Deserialize(json);
		Assert.That(payload.ContainsKey("forSender"), Is.True);
		Assert.That(payload.ContainsKey("limit"), Is.True);
		Assert.That(payload.ContainsKey("start"), Is.True);
		Assert.That(payload["forSender"], Is.EqualTo(sender));
		Assert.That(payload["limit"], Is.EqualTo(limit));
		Assert.That(payload["start"], Is.EqualTo(start));
		var restored = Json.Deserialize<SearchMailRequestClause>(json);
		// SmallerJSON currently reads null nullable longs as zero.
		Assert.That(restored.forSender, Is.EqualTo(sender ?? 0));
		Assert.That(restored.limit, Is.EqualTo(limit ?? 0));
		Assert.That(restored.start, Is.EqualTo(start ?? 0));
	}

	[TestCase(2147483648L, int.MinValue)]
	[TestCase(long.MaxValue, -1)]
	public void OversizedLimitsKeepExistingCurrentAndLegacyBehavior(long limit, int currentLimit)
	{
		var clause = Clause(null, limit, null);
		var current = Utility.Convert(new SearchMailRequest(clause));
		Assert.That(current.clauses[0].limit.Value, Is.EqualTo(currentLimit));
		Assert.That(clause.Serialize()["limit"], Is.EqualTo(limit));
	}

	[TestCaseSource(nameof(OptionalValues))]
	public void UnityCompatibleNewtonsoftPreservesNullableFields(long? sender, long? limit, long? start)
	{
		var json = JsonConvert.SerializeObject(Clause(sender, limit, start), UnitySerializationSettings.Instance);
		var restored = JsonConvert.DeserializeObject<SearchMailRequestClause>(json, UnitySerializationSettings.Instance)!;
		Assert.That(restored.forSender, Is.EqualTo(sender));
		Assert.That(restored.limit, Is.EqualTo(limit));
		Assert.That(restored.start, Is.EqualTo(start));
	}

	private static SearchMailRequestClause Clause(long? sender, long? limit, long? start) => new()
	{
		name = "page", categories = new[] { "rewards" }, states = new[] { "Unread", "Read" },
		forSender = sender, limit = limit, start = start
	};

	private static void AssertOptional(ArrayDict payload, string key, long? value)
	{
		Assert.That(payload.ContainsKey(key), Is.EqualTo(value.HasValue), key);
		if (value.HasValue) Assert.That(payload[key], Is.EqualTo(value.Value), key);
	}
}
