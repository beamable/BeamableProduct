using Newtonsoft.Json;
using NUnit.Framework;
using cli.Services.Analytics;
using Beamable.Server.Common;
using System;

namespace tests;

public class AnalyticEventUrlDeserializationTest
{
	[Test]
	public void TestDeserializationWithDefaultSettings()
	{
		var json = @"{""eventSchemas"":[{""eventName"":""SchemaName"",""uri"":""https://dev-content.beamable.com/1731504735486980/DE_1731504735486983/binary/public/analytics_events/SchemaName/schema.json/079090f0184016f1f5e3d32b1ed8d1c5""}]}";

		// Test with default JSON settings first
		var response = JsonConvert.DeserializeObject<GetAnalyticEventSchemasResponse>(json);

		Console.WriteLine($"Deserialized with default settings: EventSchemas count = {response?.EventSchemas?.Count ?? 0}");
		
		Assert.IsNotNull(response);
		Assert.IsNotNull(response.EventSchemas);
		Assert.AreEqual(1, response.EventSchemas.Count);
		Assert.AreEqual("SchemaName", response.EventSchemas[0].EventName);
		Assert.AreEqual("https://dev-content.beamable.com/1731504735486980/DE_1731504735486983/binary/public/analytics_events/SchemaName/schema.json/079090f0184016f1f5e3d32b1ed8d1c5", response.EventSchemas[0].Uri);
	}
	
	[Test]
	public void TestDeserializationWithUnitySettings()
	{
		var json = @"{""eventSchemas"":[{""eventName"":""SchemaName"",""uri"":""https://dev-content.beamable.com/1731504735486980/DE_1731504735486983/binary/public/analytics_events/SchemaName/schema.json/079090f0184016f1f5e3d32b1ed8d1c5""}]}";

		// Test with Unity serialization settings
		var response = JsonConvert.DeserializeObject<GetAnalyticEventSchemasResponse>(json, UnitySerializationSettings.Instance);

		Console.WriteLine($"Deserialized with Unity settings: EventSchemas count = {response?.EventSchemas?.Count ?? 0}");
		
		Assert.IsNotNull(response);
		Assert.IsNotNull(response.EventSchemas);
		Assert.AreEqual(1, response.EventSchemas.Count);
		Assert.AreEqual("SchemaName", response.EventSchemas[0].EventName);
		Assert.AreEqual("https://dev-content.beamable.com/1731504735486980/DE_1731504735486983/binary/public/analytics_events/SchemaName/schema.json/079090f0184016f1f5e3d32b1ed8d1c5", response.EventSchemas[0].Uri);
	}
}
