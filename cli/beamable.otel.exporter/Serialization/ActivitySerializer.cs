using System.Diagnostics;

namespace beamable.otel.exporter.Serialization;

[Serializable]
public class ActivityBatch
{
	public List<SerializableActivity> AllTraces { get; set; }
	public Dictionary<string, string> ResourceAttributes { get; set; }
	public int SchemaVersion { get; set; }
}

[Serializable]
public class SerializableActivity
{
	public string TraceId { get; set; }
	public string SpanId { get; set; }
	public string ParentSpanId { get; set; }
	public string DisplayName { get; set; }
	public DateTime StartTimeUtc { get; set; }
	public DateTime? EndTimeUtc { get; set; }
	public string Kind { get; set; }
	public string StatusCode { get; set; }
	public string StatusDescription { get; set; }
	public Dictionary<string, string> Tags { get; set; }
	public List<SerializableActivityEvent> Events { get; set; }
	public List<SerializableActivityLink> Links { get; set; }
}

[Serializable]
public class SerializableActivityEvent
{
	public string Name { get; set; }
	public DateTime Timestamp { get; set; }
	public Dictionary<string, string> Attributes { get; set; }
}

[Serializable]
public class SerializableActivityLink
{
	public string TraceId { get; set; }
	public string SpanId { get; set; }
	public Dictionary<string, string> Attributes { get; set; }
}

public static class ActivitySerializer
{
	public static Activity DeserializeActivity(SerializableActivity activityData)
	{
		var context = new ActivityContext(
			ActivityTraceId.CreateFromString(activityData.TraceId),
			ActivitySpanId.CreateFromString(activityData.SpanId),
			ActivityTraceFlags.Recorded
		);

		var kind = Enum.TryParse<ActivityKind>(activityData.Kind, out var parsedKind) ? parsedKind : ActivityKind.Internal;

		var activity = new Activity(activityData.DisplayName);
		activity.SetIdFormat(ActivityIdFormat.W3C);
		activity.SetParentId(context.TraceId, context.SpanId);


		activity.SetStartTime(activityData.StartTimeUtc);

		foreach (var tag in activityData.Tags)
		{
			activity.AddTag(tag.Key, tag.Value);
		}

		foreach (var evt in activityData.Events)
		{
			var tags = evt.Attributes.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value));
			activity.AddEvent(new ActivityEvent(
				name: evt.Name,
				timestamp: evt.Timestamp,
				tags: new ActivityTagsCollection(tags)
			));
		}

		foreach (var link in activityData.Links)
		{
			var linkContext = new ActivityContext(
				ActivityTraceId.CreateFromString(link.TraceId),
				ActivitySpanId.CreateFromString(link.SpanId),
				ActivityTraceFlags.Recorded
			);
			activity.AddLink(new ActivityLink(linkContext));
		}

		activity.Start();

		if (activityData.EndTimeUtc.HasValue)
		{
			activity.SetEndTime(activityData.EndTimeUtc.Value);
			activity.Stop();
		}

		if (!string.IsNullOrWhiteSpace(activityData.StatusCode) &&
		    Enum.TryParse<ActivityStatusCode>(activityData.StatusCode, out var statusCode))
		{
			activity.SetStatus(statusCode, activityData.StatusDescription ?? "");
		}

		return activity;
	}

	public static SerializableActivity SerializeActivity(Activity activity)
	{
		var activityTags = activity.Tags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty);

		var links = activity.Links.Select(l =>
		{
			Dictionary<string, string> tags = new Dictionary<string, string>();
			if (l.Tags != null)
			{
				tags = l.Tags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty);
			}

			return new SerializableActivityLink
			{
				TraceId = l.Context.TraceId.ToString(),
				SpanId = l.Context.SpanId.ToString(),
				Attributes = tags
			};
		}).ToList();

		var events = activity.Events.Select(e => new SerializableActivityEvent
		{
			Name = e.Name,
			Timestamp = e.Timestamp.UtcDateTime,
			Attributes = e.Tags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty)
		}).ToList();

		return new SerializableActivity
		{
			TraceId = activity.TraceId.ToString(),
			SpanId = activity.SpanId.ToString(),
			ParentSpanId = activity.ParentSpanId.ToString(),
			DisplayName = activity.DisplayName,
			StartTimeUtc = activity.StartTimeUtc,
			EndTimeUtc = activity.Duration != default ? activity.StartTimeUtc + activity.Duration : (DateTime?)null,
			Kind = activity.Kind.ToString(),
			StatusCode = activity.Status.ToString(),
			StatusDescription = activity.StatusDescription ?? "",
			Tags = activityTags,
			Events = events,
			Links = links
		};
	}

}
