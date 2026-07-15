using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Beamable.Common.BeamCli;
using Beamable.Common.Dependencies;
using cli.Services;
using cli.Services.Sandbox;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace tests.SandboxTests;

[TestFixture]
public class SandboxReporterServiceTests
{
	private RecordingSink _sink = null!;

	[SetUp]
	public void SetUp() => _sink = new RecordingSink();

	[Test]
	public void Report_EmitsEnvelopeJson_OnChannelMatchingType()
	{
		var reporter = new SandboxReporterService(provider: null, _sink, CancellationToken.None);

		reporter.Report("stream", new SamplePayload { value = 42, name = "hello" });

		Assert.That(_sink.OutputLines, Has.Count.EqualTo(1));
		var (channel, line) = _sink.OutputLines[0];
		Assert.That(channel, Is.EqualTo("stream"));

		// The line is a serialized ReportDataPoint<SamplePayload> envelope — the same shape
		// every other CLI consumer already parses.
		var envelope = JsonConvert.DeserializeObject<ReportDataPoint<SamplePayload>>(line);
		Assert.That(envelope, Is.Not.Null);
		Assert.That(envelope!.type, Is.EqualTo("stream"));
		Assert.That(envelope.data!.value, Is.EqualTo(42));
		Assert.That(envelope.data.name, Is.EqualTo("hello"));
		Assert.That(envelope.ts, Is.GreaterThan(0));
	}

	[Test]
	public void Report_OnDifferentTypes_RoutesToDifferentChannels()
	{
		var reporter = new SandboxReporterService(null, _sink, CancellationToken.None);

		reporter.Report("stream", new SamplePayload { value = 1 });
		reporter.Report("error", new SamplePayload { value = 2 });
		reporter.Report("logs", new SamplePayload { value = 3 });

		Assert.That(_sink.OutputLines.Select(o => o.channel),
			Is.EqualTo(new[] { "stream", "error", "logs" }));
	}

	[Test]
	public void Report_NullType_RoutesToEmptyChannel_DoesNotThrow()
	{
		var reporter = new SandboxReporterService(null, _sink, CancellationToken.None);
		reporter.Report(null!, new SamplePayload { value = 1 });
		Assert.That(_sink.OutputLines[0].channel, Is.EqualTo(string.Empty));
	}

	[Test]
	public void NoProvider_CancellationToken_DoesNotThrow()
	{
		// With no provider, no AppLifecycle is available — the reporter still constructs
		// cleanly and silently drops the cancellation bridge. Callers without a provider
		// don't get cancellation forwarding (and shouldn't expect it).
		using var cts = new CancellationTokenSource();
		Assert.DoesNotThrow(() =>
		{
			var reporter = new SandboxReporterService(null, _sink, cts.Token);
			cts.Cancel(); // should be a no-op
			reporter.Dispose();
		});
	}

	[Test]
	public void WithProvider_CancellationToken_BridgesToAppLifecycle()
	{
		var lifecycle = new AppLifecycle();
		var provider = new Mock<IDependencyProvider>();
		// IDependencyProvider exposes its own generic GetService<T>(), separate from the
		// IServiceProvider.GetService(Type) variant. The reporter calls the generic one.
		provider.Setup(p => p.GetService<AppLifecycle>()).Returns(lifecycle);

		using var cts = new CancellationTokenSource();
		var reporter = new SandboxReporterService(provider.Object, _sink, cts.Token);

		Assert.That(lifecycle.IsCancelled, Is.False);
		cts.Cancel();
		Assert.That(lifecycle.IsCancelled, Is.True);

		reporter.Dispose();
	}

	[Test]
	public void CircularPayload_FailsToErrorChannel_NotThrow()
	{
		// Newtonsoft's default serializer throws on cyclic references; this exercises the
		// reporter's serialization-failure catch path.
		var cyclic = new Cyclic();
		cyclic.Self = cyclic;

		var reporter = new SandboxReporterService(null, _sink, CancellationToken.None);
		Assert.DoesNotThrow(() => reporter.Report("stream", cyclic));

		// Should land on the error channel with a serialization-failure marker.
		Assert.That(_sink.OutputLines, Has.Some.Matches<(string channel, string line)>(
			o => o.channel == "error" && o.line.Contains("serialization failed")));
	}

	private sealed class SamplePayload
	{
		public int value;
		public string? name;
	}

	private sealed class Cyclic
	{
		public Cyclic? Self;
	}

	private sealed class RecordingSink : IInvocationSink
	{
		public List<(string channel, string line)> OutputLines { get; } = new();
		public List<(InvocationStatusKind status, int? exit, string? reason)> Statuses { get; } = new();

		public void EmitOutput(string channel, string line) => OutputLines.Add((channel, line));
		public void EmitStatus(InvocationStatusKind status, int? exitCode = null, string? failureReason = null)
			=> Statuses.Add((status, exitCode, failureReason));
	}
}
