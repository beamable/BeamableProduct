using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;
using System.Threading;
using Beamable.Common;
using Beamable.Server;

namespace Beamable.Server;

public interface IActivityProvider
{
    BeamActivity Create(string operationName, BeamActivity parent, bool autoStart = true,
        TelemetryImportance importance = TelemetryImportance.INFO, TelemetryAttributeCollection attributes = null);
}

public class NoopActivityProvider : IActivityProvider
{
    public BeamActivity Create(string operationName, BeamActivity parent, bool autoStart = true,
        TelemetryImportance importance = TelemetryImportance.INFO, TelemetryAttributeCollection attributes = null)
    {
        return BeamActivity.Noop;
    }
}

public static class ActivityProviderExtensions
{
    public static BeamActivity Create(this IActivityProvider provider, string operationName, bool autoStart = true,
        TelemetryImportance importance = TelemetryImportance.INFO, TelemetryAttributeCollection attributes = null)
    {
        return provider.Create(operationName, null, autoStart, importance, attributes);
    }
}

public interface IActivityProviderArgs
{
    string NamePrefix { get; }
}

public interface IInternalBeamActivity
{
    void AddTagsDict(Dictionary<string, object> tags);
}

public class BeamActivity : IDisposable, IInternalBeamActivity
{
    private readonly Activity _activity;
    private IActivityProvider _provider;

    public ActivityTraceId TraceId => _activity?.TraceId ?? default;
    public ActivitySpanId SpanId => _activity?.SpanId ?? default;

    public ActivityContext Context => _activity?.Context ?? default;

    public ActivityContext Root
    {
        get
        {
            var p = _activity;
            ActivityContext ctx = default;
            while (p != null)
            {
                ctx = p.Context;
                p = p.Parent;
            }

            return ctx;
        }
    }

    /// <summary>
    /// The activity provider returns null instances of <see cref="Activity"/>
    /// when there is no configured listener. 
    /// </summary>
    public bool IsReal => _activity != null;

    public readonly static BeamActivity Noop = new BeamActivity(null, null);

    public BeamActivity(IActivityProvider provider, Activity activity)
    {
        _provider = provider;
        
        // activity CAN be null if there is no emitter. 
        _activity = activity;
    }

    public void Start()
    {
        if (_activity == null) return;
        _activity.Start();
    }

    public BeamActivity CreateChild(string operationName, bool autoStart=true)
    {
        return _provider.Create(operationName, this, autoStart);
    }

    public void SetTags(TelemetryAttributeCollection attributes)
    {
        if (_activity == null) return;
        var dict = attributes.ToDictionary();
        foreach (var kvp in dict)
        {
            _activity.SetTag(kvp.Key, kvp.Value);
        }
    }

    public void SetTag(TelemetryAttribute tag)
    {
        if (_activity == null) return;
        _activity.SetTag(tag.name, tag.type == TelemetryAttributeType.LONG ? tag.longValue : tag.stringValue);
    }
    
    public bool TryGetTags(out IEnumerable<KeyValuePair<string, object>> tags)
    {
        tags = null;
        if (_activity == null) return false;

        tags = _activity.TagObjects;
        return true;
    }

    public void SetDisplay(string name)
    {
        if (_activity == null) return;
        _activity.DisplayName = name;
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

    public void StopAndDispose(Exception ex)
    {
        if (_activity == null) return;
        
        SetException(ex);
        _activity.Stop();
        _activity.Dispose();
    }

    public void SetException(Exception ex)
    {
        if (_activity == null) return;

        // OTEL link for exception attributes. 
        //  https://opentelemetry.io/docs/specs/semconv/attributes-registry/exception/
        _activity.SetStatus(ActivityStatusCode.Error);
        // _activity.SetTag("exception.type", ex.GetType().Name);
        // _activity.SetTag("exception.message", ex.Message);
        // _activity.SetTag("exception.stack", ex.StackTrace);
        
        // TODO: It seems that the otel standard wants "exception", but some tools like datadog want "error" :/
        _activity.SetTag("error.type", ex.GetType().Name);
        _activity.SetTag("error.message", ex.Message);
        _activity.SetTag("error.stack", ex.StackTrace);


    }

    public void StopAndDispose(ActivityStatusCode status)
    {
        if (_activity == null) return;
        // _activity.SetStatus(status);
        _activity.Stop();
        _activity.Dispose();
    }
    
    public void Dispose()
    {
        if (_activity == null) return;
        _activity.Stop();
        _activity.Dispose();
    }

    void IInternalBeamActivity.AddTagsDict(Dictionary<string, object> tags)
    {
        if (_activity == null) return;

        foreach (var kvp in tags)
        {
            _activity.SetTag(kvp.Key, kvp.Value);
        }
    }
}


public class DefaultActivityProvider : IActivityProvider
{
    private ActivitySource _activitySource;
    private Meter _meter;

    public readonly string ServiceName;
    public readonly string ServiceNamespace;
    public readonly string ServiceId;

    public static DefaultActivityProvider CreateMicroServiceProvider(IActivityProviderArgs args, IMicroserviceAttributes attribute)
    {
        return new DefaultActivityProvider(Constants.Features.Otel.METER_SERVICE_NAME, 
            string.IsNullOrEmpty(args.NamePrefix) ? attribute.MicroserviceName : $"{attribute.MicroserviceName}.{args.NamePrefix}",
            attribute.MicroserviceName);
    }

    public static DefaultActivityProvider CreateCliServiceProvider()
    {
        return new DefaultActivityProvider(Constants.Features.Otel.METER_CLI_NAME,
            "beam.cli", "cli");
    }
    
    public DefaultActivityProvider(string meterName, string serviceName, string serviceNamepsace)
    {
        ServiceNamespace = serviceNamepsace;
        ServiceName = serviceName;
        ServiceId = Guid.NewGuid().ToString();
        
        _activitySource = new ActivitySource(meterName);
        _meter = new Meter(_activitySource.Name);
        //
        // TestCounter = _meter.CreateCounter<long>("Test2", description: "a simple test");
        // TestGauge = _meter.CreateGauge<long>("TestG");
    }
    
    public BeamActivity Create(string operationName, BeamActivity parent, bool autoStart = true, TelemetryImportance importance=TelemetryImportance.INFO, TelemetryAttributeCollection attributes = null)
    {
        ActivityContext context = default;
        // Dictionary<string, object> tags = null;
        if (parent?.IsReal ?? false)
        {
            context = parent.Context;
            // parent.TryGetTags(out var parentTags);
            // tags = parentTags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        var tags = attributes?.ToDictionary() ?? new Dictionary<string, object>();
        tags[Constants.Features.Otel.ATTR_TRACE_LEVEL] = importance;
        
        var activity = _activitySource.CreateActivity(
            operationName, 
            ActivityKind.Server, 
            parentContext: context, 
            tags);
        
        var beamActivity = new BeamActivity(this, activity);
        if (autoStart)
        {
            activity?.Start();
        }

        return beamActivity;
    }
}