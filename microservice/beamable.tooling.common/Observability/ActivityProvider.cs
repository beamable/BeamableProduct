using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
using Beamable.Common;
using Beamable.Server;

namespace microservice.Observability;

public interface IActivityProvider
{
}

public interface IActivityProviderArgs
{
    string NamePrefix { get; }
}

public class BeamActivity : IDisposable
{
    private readonly Activity _activity;

    public ActivityTraceId TraceId => _activity.TraceId;
    public ActivitySpanId SpanId => _activity.SpanId;
    
    public BeamActivity(Activity activity)
    {
        // activity CAN be null if there is no emitter. 
        _activity = activity;
    }

    public void Start()
    {
        if (_activity == null) return;
        _activity.Start();
    }

    public void SetTags(IEnumerable<KeyValuePair<string, object>> tags)
    {
        if (_activity == null) return;
        foreach (var tag in tags)
        {
            _activity.SetTag(tag.Key, tag.Value);
        }
    }

    public void SetTag(string tag, object value)
    {
        if (_activity == null) return;
        _activity.SetTag(tag, value);
    }

    public void AddEvent(ActivityEvent evt)
    {
        if (_activity == null) return;
        _activity.AddEvent(evt);
    }

    public void SetStatus(ActivityStatusCode status)
    {
        if (_activity == null) return;
        _activity.SetStatus(status);
    }

    public void Stop()
    {
        if (_activity == null) return;
        _activity.Stop();
    }
    
    public void Stop(ActivityStatusCode status)
    {
        if (_activity == null) return;
        _activity.SetStatus(status);
        _activity.Stop();
    }

    public void Dispose()
    {
        if (_activity == null) return;
        _activity.Stop();
        _activity.Dispose();
    }
}

public class DefaultActivityProvider
{
    public AsyncLocal<BeamActivity> CurrentActivity = new AsyncLocal<BeamActivity>();
    
    private ActivitySource _activitySource;
    private Meter _meter;

    public Counter<long> TestCounter;
    public Gauge<long> TestGauge;

    public readonly string ServiceName;
    public readonly string ServiceNamespace;
    public readonly string ServiceId;
    
    public DefaultActivityProvider(IActivityProviderArgs args, MicroserviceAttribute attribute)
    {
        ServiceNamespace = attribute.MicroserviceName;
        ServiceName = string.IsNullOrEmpty(args.NamePrefix) ? attribute.MicroserviceName : $"{attribute.MicroserviceName}.{args.NamePrefix}";
        ServiceId = Guid.NewGuid().ToString();
        
        _activitySource = new ActivitySource(Constants.Features.Otel.METER_NAME);
        _meter = new Meter(_activitySource.Name);
        
        TestCounter = _meter.CreateCounter<long>("Test2", description: "a simple test");
        TestGauge = _meter.CreateGauge<long>("TestG");
    }
    
    public BeamActivity Create(string operationName, bool autoStart=true)
    {
        var activity = _activitySource.CreateActivity(operationName, ActivityKind.Server);
        var beamActivity = new BeamActivity(activity);
        CurrentActivity.Value = beamActivity;
        if (autoStart)
        {
            activity?.Start();
        }
        return beamActivity;
    }

    public BeamActivity Create(string operationName, BeamActivity parent, bool autoStart=true)
    {
        var context = new ActivityContext(
            parent.TraceId, 
            parent.SpanId, 
            ActivityTraceFlags.Recorded
            );
        
        var activity = _activitySource.CreateActivity(
            operationName, 
            ActivityKind.Server, parentContext: context, idFormat: ActivityIdFormat.W3C);
        
        var beamActivity = new BeamActivity(activity);
        CurrentActivity.Value = beamActivity;
        
        if (autoStart)
        {
            activity?.Start();
        }
        return beamActivity;
    }
    
}