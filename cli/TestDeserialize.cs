using Newtonsoft.Json;
using System;
using System.Collections.Generic;

var json = @"{""eventSchemas"":[{""eventName"":""SchemaName"",""uri"":""https://dev-content.beamable.com/1731504735486980/DE_1731504735486983/binary/public/analytics_events/SchemaName/schema.json/079090f0184016f1f5e3d32b1ed8d1c5""}]}";

var response = JsonConvert.DeserializeObject<GetAnalyticEventSchemasResponse>(json);

Console.WriteLine($"eventSchemas count: {response?.eventSchemas?.Count ?? 0}");
if (response?.eventSchemas?.Count > 0)
{
    Console.WriteLine($"First event name: {response.eventSchemas[0].EventName}");
    Console.WriteLine($"First event uri: {response.eventSchemas[0].Uri}");
}

[Serializable]
public class GetAnalyticEventSchemasResponse
{
	[JsonProperty("eventSchemas")]
	public List<AnalyticEventUrl> eventSchemas { get; set; } = new();
}

[Serializable]
public class AnalyticEventUrl
{
	[JsonProperty("eventName")]
	public string EventName { get; set; } = string.Empty;

	[JsonProperty("uri")]
	public string Uri { get; set; } = string.Empty;
}
