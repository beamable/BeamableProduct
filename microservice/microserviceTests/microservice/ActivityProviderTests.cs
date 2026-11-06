using System.Collections.Generic;
using System.Diagnostics;
using Beamable.Server;
using NUnit.Framework;

namespace microserviceTests.microservice;

public class ActivityProviderTests
{
    const string SourceName = "microserviceTests.activity";
    const string CallerTraceId = "4bf92f3577b34da6a3ce929d0e0e4736";

    static ActivityListener Listen()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    [Test]
    public void A_request_with_a_traceparent_joins_the_callers_trace()
    {
        // Header names are case-insensitive, as the gateway forwards them.
        var headers = new RequestHeaders(new Dictionary<string, string>
        {
            ["Traceparent"] = $"00-{CallerTraceId}-00f067aa0ba902b7-01"
        });
        Assert.That(ActivityProviderExtensions.TryGetRemoteParent(headers, out var parent), Is.True);

        using var listener = Listen();
        IActivityProvider provider = new DefaultActivityProvider(SourceName, "test", "test");
        using var activity = provider.Create("request", parent);

        Assert.That(activity.TraceId.ToHexString(), Is.EqualTo(CallerTraceId));
    }

    [TestCase(null)]
    [TestCase("not-a-traceparent")]
    public void A_request_without_a_valid_traceparent_starts_its_own_trace(string traceParent)
    {
        var headers = new RequestHeaders(traceParent == null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string> { ["traceparent"] = traceParent });

        Assert.That(ActivityProviderExtensions.TryGetRemoteParent(headers, out _), Is.False);
    }
}
