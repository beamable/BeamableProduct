using Beamable.Common;
using Beamable.Common.Api;
using Newtonsoft.Json;

namespace cli.Services.Analytics;

/// <summary>
/// Client for the realm-level <c>/api/analytics/event/schemas</c> service. Mirrors the read
/// endpoints from the OpenAPI spec — list, list-urls, read-one. The colon-prefixed action
/// segments (e.g. <c>schemas:urls</c>) live at a different segment depth than <c>{name}</c>,
/// so a literal action segment can never collide with an event name.
/// </summary>
public class AnalyticEventsApi
{
	public const string SERVICE = "/api/analytics/event/schemas";

	private readonly IBeamableRequester _requester;

	public AnalyticEventsApi(IBeamableRequester requester)
	{
		_requester = requester;
	}

	/// <summary>
	/// GET <c>/api/analytics/event/schemas</c> — every event schema on the active manifest,
	/// including archived entries. Each item carries a resolved CDN <c>uri</c> alongside
	/// <c>(contentId, version)</c>. Requires TesterPolicy.
	/// </summary>
	public Promise<List<AnalyticEventView>> GetAll()
	{
		return _requester.Request<GetAnalyticEventsResponse>(Method.GET, SERVICE)
			.Map(res => res.Events ?? new List<AnalyticEventView>());
	}

	/// <summary>
	/// GET <c>/api/analytics/event/schemas:urls</c> — active schemas projected as
	/// <c>(eventName, url)</c> pairs for game-client consumption. Available to any
	/// authenticated user.
	/// </summary>
	public Promise<List<AnalyticEventUrl>> GetUrls()
	{
		return _requester.Request<GetAnalyticEventSchemasResponse>(Method.GET, $"{SERVICE}:urls")
			.Map(res => res.EventSchemas ?? new List<AnalyticEventUrl>());
	}

	/// <summary>
	/// GET <c>/api/analytics/event/schemas/{name}</c> — read a single event by name.
	/// </summary>
	public Promise<AnalyticEventView> GetByName(string name)
	{
		return _requester.Request<AnalyticEventView>(Method.GET, $"{SERVICE}/{name}");
	}
}
[Serializable]
public class GetAnalyticEventsResponse
{
	[JsonProperty("events")]
	public List<AnalyticEventView> Events = new();
}
[Serializable]
public class GetAnalyticEventSchemasResponse
{
	[JsonProperty("eventSchemas")]
	public List<AnalyticEventUrl> EventSchemas = new();
}
[Serializable]
public class AnalyticEventView
{
	[JsonProperty("name")]
	public string Name = string.Empty;

	[JsonProperty("category")]
	public string Category = string.Empty;

	[JsonProperty("description")]
	public string Description = string.Empty;

	[JsonProperty("enabled")]
	public bool Enabled;

	[JsonProperty("archived")]
	public bool Archived;

	[JsonProperty("schema")]
	public AnalyticEventSchemaRef Schema = new();
}

[Serializable]
public class AnalyticEventSchemaRef
{
	[JsonProperty("contentId")]
	public string ContentId = string.Empty;

	[JsonProperty("version")]
	public string Version = string.Empty;

	[JsonProperty("uri")]
	public string Uri = string.Empty;
}
[Serializable]
public class AnalyticEventUrl
{
	[JsonProperty("eventName")]
	public string EventName;

	[JsonProperty("uri")]
	public string Uri;
}
