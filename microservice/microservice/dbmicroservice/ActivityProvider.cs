using beamable.server.Tracing;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Beamable.Server;


public interface IActivityProvider
{
	Activity StartActivity(string name, string parentId = null);
	Counter<T> GetCounter<T>(string name, string unit = null, string description = null) where T : struct;
}

public static class ActivityProviderExtensions
{
	public static Counter<long> RequesterExceptionCounter(this IActivityProvider provider) =>
		provider.GetCounter<long>(OTElConstants.METRIC_REQUEST_CLIENT_ERROR_COUNT,
			description: "The number of request errors for client driven messages");
	
	public static Counter<long> RequestCounter(this IActivityProvider provider) =>
		provider.GetCounter<long>(OTElConstants.METRIC_REQUEST_COUNT,
			description: $"The number of received requests, not to be confused with {OTElConstants.METRIC_REQUEST_PLATFORM_COUNT} or {OTElConstants.METRIC_REQUEST_CLIENT_COUNT}");

	public static Counter<long> PlatformRequestCounter(this IActivityProvider provider) =>
		provider.GetCounter<long>(OTElConstants.METRIC_REQUEST_PLATFORM_COUNT,
			description: "The number of received requests for platform driven messages");

	public static Counter<long> ClientRequestCounter(this IActivityProvider provider) =>
		provider.GetCounter<long>(OTElConstants.METRIC_REQUEST_CLIENT_COUNT,
			description: "The number of received requests for client driven messages");


	public static void IncrementRequesterExceptionCounter(this IActivityProvider provider, Exception ex) =>
		RequesterExceptionCounter(provider)
			.Increment(new KeyValuePair<string, object>(OTElConstants.TAG_BEAM_ERROR_TYPE, ex?.GetType().Name));
	
	
	public static void IncrementRequestCounter(this IActivityProvider provider) =>
		RequestCounter(provider).Increment();
	
	public static void IncrementPlatformRequestCounter(this IActivityProvider provider) =>
		PlatformRequestCounter(provider).Increment();
	
	public static void IncrementClientRequestCounter(this IActivityProvider provider) =>
		ClientRequestCounter(provider).Increment();
}

public static class CounterExtensions
{
	public static void Increment(this Counter<long> counter, params KeyValuePair<string, object>[] tags) => counter.Add(1, tags);
}

public static class OTElConstants
{
	public const string BeamableActivityName = "Beamable.BeamService.Core";

	public const string TAG_PEER_SERVICE = "peer.service";
	public const string TAG_NET_SOCK_PEER_NAME = "net.sock.peer.name";
	public const string TAG_BEAM_ROUTE = "beam.route";
	public const string TAG_BEAM_PARAM_PROVIDER = "beam.param.provider";
	public const string TAG_BEAM_RES_BODY = "beam.res.body";
	
	public const string TAG_BEAM_CID = "beam.cid";
	public const string TAG_BEAM_PID = "beam.pid";
	public const string TAG_BEAM_REQ_ID = "beam.req.id";
	public const string TAG_BEAM_REQ_METHOD = "beam.req.method";
	public const string TAG_BEAM_REQ_PATH = "beam.req.path";
	public const string TAG_BEAM_REQ_BODY = "beam.req.body";
	public const string TAG_BEAM_REQ_TYPE = "beam.req.type";
	public const string TAG_BEAM_USER_ID = "beam.user.id";
	public const string TAG_BEAM_USER_SCOPES = "beam.user.scopes";
	


	public const string TAG_BEAM_ERROR_TYPE = "beam.error.type";

	public const string ACT_SERIALIZE = "Serialize";
	public const string ACT_MESSAGE = "Message";
	public const string ACT_CLIENT_CALLABLE = "Method";
	public const string ACT_SEND_WEBSOCKET = "WebsocketOut";
	public const string ACT_PLATFORM_MESSAGE = "Platform";
	public const string ACT_CLIENT_MESSAGE = "Client";

	public const string METRIC_REQUEST_COUNT = "beam.request.count";
	public const string METRIC_REQUEST_PLATFORM_COUNT = "beam.request.platform.count";
	public const string METRIC_REQUEST_CLIENT_COUNT = "beam.request.client.count";
	public const string METRIC_REQUEST_CLIENT_ERROR_COUNT = "beam.request.client.error.count";
}

public class ActivityProvider : IActivityProvider
{
	public const string NAME = OTElConstants.BeamableActivityName;

	private ActivitySource _activitySource;
	private Meter _meter;

	public string ActivityName => _activitySource.Name;
	public string ActivityVersion => _activitySource.Version;

	public ActivityProvider(string version=null)
	{
		version ??= "0.0.0";
		_activitySource = new ActivitySource(NAME, version);
		_meter = new Meter(NAME, version);
	}

	public Activity StartActivity(string name, string parentId = null)
	{
		var activity = _activitySource.StartActivity(name, ActivityKind.Server, parentId);
		activity?.SetTag(OTElConstants.TAG_PEER_SERVICE, "Microservice");
		return activity;
	}

	public Counter<T> GetCounter<T>(string name, string unit = null, string description = null)
		where T: struct
	{
		return CounterCollection<T>.Counters.GetOrAdd(name, name => _meter.CreateCounter<T>(name, unit, description));
	}

	private static class CounterCollection<T> where T : struct
	{
		public static ConcurrentDictionary<string, Counter<T>> Counters = new ConcurrentDictionary<string, Counter<T>>();
	}
}

public class BeamableTracer : IBeamableTracer
{
	private readonly IActivityProvider _provider;

	public BeamableTracer(IActivityProvider provider)
	{
		_provider = provider;
	}

	public IBeamTrace Start(string name)
	{
		var activity = _provider.StartActivity(name);
		var trace = new BeamTrace(activity);
		return trace;
	}

	public IBeamMetricCounter<T> GetCounter<T>(string name, string unit = null, string description = null) where T : struct
	{
		var meter = _provider.GetCounter<T>(name, unit, description);
		return new BeamMetricCounter<T>(meter);
	}
}

public class BeamMetricCounter<T> : IBeamMetricCounter<T>
	where T : struct
{
	private readonly Counter<T> _counter;

	public BeamMetricCounter(Counter<T> counter)
	{
		_counter = counter;
	}
	public void Add(T value, params KeyValuePair<string, object>[] tags)
	{
		_counter.Add(value, tags);
	}
}

public class BeamTrace : IBeamTrace
{
	private readonly Activity _activity;

	public BeamTrace(Activity activity)
	{
		_activity = activity;
	}

	public void Dispose()
	{
		_activity?.SetStatus(ActivityStatusCode.Ok);
		_activity?.Dispose();
	}

	public void SetTag(string tagName, object value)
	{
		if (!_activity.IsAllDataRequested) return;
		_activity?.SetTag(tagName, value);
	}

	public void SetTags(IEnumerable<KeyValuePair<string, object>> tags)
	{
		if (!_activity.IsAllDataRequested) return;
		foreach (var tag in tags)
		{
			_activity?.SetTag(tag.Key, tag.Value);
		}
	}

	public void RecordException(Exception ex)
	{
		_activity.RecordException(ex);
		_activity.SetStatus(ActivityStatusCode.Error, ex?.Message);
	}
}
