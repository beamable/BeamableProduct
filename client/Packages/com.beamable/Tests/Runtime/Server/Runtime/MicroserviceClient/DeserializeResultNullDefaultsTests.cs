using Beamable.Common.Api;
using Beamable.Common.Inventory;
using Beamable.Tests.Runtime;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.TestTools;

namespace Beamable.Server.Tests.Runtime
{
	/// <summary>
	/// Document the non-null guarantees that <see cref="MicroserviceClientHelper.DeserializeResult{T}"/>
	/// provided while it was backed by JsonUtility.FromJson (SDK 5.0.x and earlier).
	///
	/// A C# microservice serializes response DTOs with Newtonsoft using default NullValueHandling,
	/// so an unassigned reference-type field arrives at the client as an explicit JSON null member,
	/// for example {"name": null}. JsonUtility deserialized such a member to its Unity-serialization
	/// default: an empty string, an empty array or list, or a default-constructed nested object.
	/// Client code written against SDK 5.0.x and earlier relies on those fields never being null.
	///
	/// Members that are entirely absent from the JSON are a distinct case with a narrower
	/// contract: JsonUtility treated absent string and string[] members as null, so no non-null
	/// guarantee ever existed for those, but it default-constructed absent List and nested
	/// object members. Absence occurs on the wire when a field is a valueless Optional, or when
	/// a service hand-serializes its response with null-omitting settings.
	/// </summary>
	public class DeserializeResultNullDefaultsTests : BeamableTest
	{
		private const string ROUTE = "test";

		[Serializable]
		public class NestedPayload
		{
			public string label;
		}

		[Serializable]
		public class PlayerStatusResponse
		{
			public int level;
			public string name;
			public string[] roles;
			public List<string> tags;
			public NestedPayload nested;
			public ItemRef itemRef;
		}

		private const string EXPLICIT_NULLS_JSON =
			"{\"level\": 3, \"name\": null, \"roles\": null, \"tags\": null, \"nested\": null, \"itemRef\": null}";

		private static void AssertReferenceFieldsAreNotNull(PlayerStatusResponse result)
		{
			Assert.NotNull(result, "the response object itself should never be null");
			Assert.NotNull(result.name, "an explicitly null string member should deserialize to an empty string, not null");
			Assert.NotNull(result.roles, "an explicitly null array member should deserialize to an empty array, not null");
			Assert.NotNull(result.tags, "an explicitly null list member should deserialize to an empty list, not null");
			Assert.NotNull(result.nested, "an explicitly null object member should deserialize to a default instance, not null");
			Assert.NotNull(result.itemRef, "an explicitly null content ref member should deserialize to a default instance, not null");
		}

		[Test]
		public void ExplicitNullReferenceFields_DeserializeToNonNullDefaults()
		{
			var result = MicroserviceClientHelper.DeserializeResult<PlayerStatusResponse>(EXPLICIT_NULLS_JSON);

			Assert.AreEqual(3, result.level);
			AssertReferenceFieldsAreNotNull(result);
			Assert.AreEqual(string.Empty, result.name);
			Assert.AreEqual(0, result.roles.Length);
			Assert.AreEqual(0, result.tags.Count);
		}

		[Test]
		public void NestedObjectWithExplicitNullString_DeserializesToEmptyString()
		{
			var result = MicroserviceClientHelper.DeserializeResult<PlayerStatusResponse>("{\"nested\": {\"label\": null}}");

			Assert.NotNull(result.nested);
			Assert.NotNull(result.nested.label, "an explicitly null string member of a nested object should deserialize to an empty string, not null");
		}

		[Test]
		public void ListOfObjects_ExplicitNullReferenceFields_DeserializeToNonNullDefaults()
		{
			var result = MicroserviceClientHelper.DeserializeResult<List<PlayerStatusResponse>>(
				"[" + EXPLICIT_NULLS_JSON + "," + EXPLICIT_NULLS_JSON + "]");

			Assert.AreEqual(2, result.Count);
			AssertReferenceFieldsAreNotNull(result[0]);
			AssertReferenceFieldsAreNotNull(result[1]);
		}

		[Test]
		public void PopulatedReferenceFields_DeserializeToTheirValues()
		{
			var json = "{\"level\": 9, \"name\": \"beam\", \"roles\": [\"admin\"], \"tags\": [\"a\", \"b\"], " +
					   "\"nested\": {\"label\": \"x\"}, \"itemRef\": {\"Id\": \"items.sword\"}}";
			var result = MicroserviceClientHelper.DeserializeResult<PlayerStatusResponse>(json);

			Assert.AreEqual(9, result.level);
			Assert.AreEqual("beam", result.name);
			Assert.AreEqual(new[] { "admin" }, result.roles);
			Assert.AreEqual(new List<string> { "a", "b" }, result.tags);
			Assert.AreEqual("x", result.nested.label);
			Assert.AreEqual("items.sword", result.itemRef.Id);
		}

		[Test]
		public void AbsentCollectionAndObjectFields_DeserializeToNonNullDefaults()
		{
			// absent string and string[] members deserialized to null even under JsonUtility,
			// so only list, nested object, and content ref fields carry a non-null guarantee
			// when a member is omitted from the JSON entirely.
			var result = MicroserviceClientHelper.DeserializeResult<PlayerStatusResponse>("{\"level\": 3}");

			Assert.AreEqual(3, result.level);
			Assert.NotNull(result.tags, "an absent list member should deserialize to an empty list, not null");
			Assert.NotNull(result.nested, "an absent object member should deserialize to a default instance, not null");
			Assert.NotNull(result.itemRef, "an absent content ref member should deserialize to a default instance, not null");
		}

		[UnityTest]
		public IEnumerator Request_ExplicitNullReferenceFields_DeserializeToNonNullDefaults()
		{
			var client = new TestClient(ROUTE, MockRequester);

			MockRequester.MockRequest<PlayerStatusResponse>(Method.POST,
					client.GetMockPath(MockApi.Token.Cid, MockApi.Token.Pid, ROUTE))
				.WithRawResponse(EXPLICIT_NULLS_JSON);

			var req = client.Request<PlayerStatusResponse>(ROUTE, new string[] { });

			yield return req.ToYielder();
			var result = req.GetResult();
			Assert.AreEqual(3, result.level);
			AssertReferenceFieldsAreNotNull(result);
		}
	}
}
