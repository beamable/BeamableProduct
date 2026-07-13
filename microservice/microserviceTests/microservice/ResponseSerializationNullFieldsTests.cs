using Beamable.Server;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;

namespace microserviceTests.microservice;

/// <summary>
/// Document what a C# microservice puts on the wire for a [Callable] response DTO whose
/// reference-type fields were never assigned. The DefaultResponseSerializer serializes with
/// Newtonsoft using default NullValueHandling, so a null reference field is emitted as an
/// explicit JSON null: the member is present with a null value, not omitted. Any client
/// deserializer therefore has to decide what a null member means for a reference-type field.
/// </summary>
public class ResponseSerializationNullFieldsTests
{
	[System.Serializable]
	public class PlayerStatusResponse
	{
		public int level;
		public string name;
		public string[] roles;
		public List<string> tags;
	}

	[Test]
	public void NullReferenceFields_AreEmittedAsExplicitNulls()
	{
		var serializer = new DefaultResponseSerializer(useLegacySerialization: false);
		var ctx = new RequestContext("cid", "pid", 1, 200, 1, "path", "POST", "");

		var json = serializer.SerializeResponse(ctx, new PlayerStatusResponse { level = 3 });

		var body = JObject.Parse(json)["body"] as JObject;
		Assert.NotNull(body, "response envelope should carry the DTO in its body member");
		Assert.AreEqual(3, (int)body["level"]);
		foreach (var member in new[] { "name", "roles", "tags" })
		{
			Assert.IsTrue(body.ContainsKey(member), $"the {member} member should be present in the wire JSON");
			Assert.AreEqual(JTokenType.Null, body[member].Type, $"the {member} member should be an explicit JSON null");
		}
	}
}
