using System;
using System.Diagnostics;
using Beamable.Server;
using beamable.microservice.otel.exporter;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;

namespace microserviceTests.microservice.Otel;

public class CollectorFailureTests
{
    [TearDown]
    public void TearDown()
    {
        MicroserviceOtelRetry.ResetForTests();
    }

    [Test]
    public void DiscoveryFailure_WithCallback_CallsItInsteadOfExiting()
    {
        Exception received = null;
        var failure = new Exception("The collector couldn't start.");

        // Without the callback this calls Environment.Exit, so reaching the assert means the process lived.
        CollectorManager.HandleDiscoveryFailure(failure, NullLogger.Instance, ex => received = ex);

        Assert.That(received, Is.SameAs(failure));
    }

    [Test]
    public void CollectorUnavailable_DisablesExporterRetry()
    {
        MicroserviceStartupUtil.OnCollectorUnavailable(NullLogger.Instance, new Exception("The collector couldn't start."));

        Assert.That(MicroserviceOtelRetry.Disabled, Is.True);
    }

    [Test]
    public void FailedExports_StopQueueing_OnceRetryIsDisabled()
    {
        // Nothing listens on port 1, so every export fails, as it does when the collector is gone.
        var exporter = new MicroserviceOtelActivityExporter(new MicroserviceOtelExporterOptions
        {
            OtlpEndpoint = "http://127.0.0.1:1",
            Protocol = OtlpExportProtocol.HttpProtobuf
        });
        using var source = new ActivitySource(nameof(FailedExports_StopQueueing_OnceRetryIsDisabled));
        using var provider = Sdk.CreateTracerProviderBuilder()
            .AddSource(source.Name)
            .AddProcessor(new SimpleActivityExportProcessor(exporter))
            .Build();

        // The exporter only sends failed activities.
        void ExportOneFailedActivity()
        {
            using var activity = source.StartActivity("work");
            activity!.SetStatus(ActivityStatusCode.Error);
        }

        ExportOneFailedActivity();
        ExportOneFailedActivity();
        Assert.That(exporter.QueuedBatchCount, Is.EqualTo(2), "with retry on, failed batches are queued");

        MicroserviceOtelRetry.Disable();
        ExportOneFailedActivity();
        ExportOneFailedActivity();
        Assert.That(exporter.QueuedBatchCount, Is.EqualTo(0), "with retry off, the queue is dropped and stays empty");
    }
}
