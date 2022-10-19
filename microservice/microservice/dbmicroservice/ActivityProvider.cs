using beamable.server.Tracing;
using OpenTelemetry.Trace;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Beamable.Server;


public interface IActivityProvider
{
	Activity StartActivity(string name);
	Counter<T> GetCounter<T>(string name, string unit = null, string description = null) where T : struct;
}

public class ActivityProvider : IActivityProvider
{
	public const string NAME = "Beamable.BeamService.Core";

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

	public Activity StartActivity(string name)
	{
		var activity = _activitySource.StartActivity(name, ActivityKind.Server);
		activity?.SetTag("peer.service", "Microservice");
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
	public void Add(T value)
	{
		_counter.Add(value);
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
