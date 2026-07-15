using System;
using System.Threading;
using Beamable.Common.BeamCli;
using Beamable.Common.Dependencies;
using Beamable.Server.Common;
using Newtonsoft.Json;

namespace cli.Services.Sandbox;

/// <summary>
/// <see cref="IDataReporterService"/> implementation that routes the CLI's structured
/// output protocol into an invocation's <see cref="IInvocationSink"/>. Each
/// <c>Report&lt;T&gt;</c> call is serialized into the same <see cref="ReportDataPoint{T}"/>
/// envelope every other consumer (Unity Editor, Portal) already parses, then handed to
/// the sink as one <see cref="SandboxEvent.InvocationOutput"/> line. Channel == the
/// <c>type</c> the CLI command emitted (<c>stream</c>, <c>error</c>, <c>logs</c>, …) so
/// Portal can route lines without re-parsing the envelope.
///
/// Also bridges an external <see cref="CancellationToken"/> into the App's
/// <see cref="AppLifecycle"/>: when the invocation is cancelled, the App's cooperative
/// cancellation fires, mirroring how <c>ServerReporterService</c> cancels on pipe break.
/// </summary>
public sealed class SandboxReporterService : IDataReporterService
{
	private readonly IInvocationSink _sink;
	private readonly AppLifecycle? _lifecycle;
	private readonly CancellationTokenRegistration _cancelBridge;

	public SandboxReporterService(IDependencyProvider provider, IInvocationSink sink, CancellationToken externalToken)
	{
		_sink = sink ?? throw new ArgumentNullException(nameof(sink));
		_lifecycle = provider?.GetService<AppLifecycle>();
		if (_lifecycle != null && externalToken.CanBeCanceled)
		{
			_cancelBridge = externalToken.Register(static state => ((AppLifecycle)state!).Cancel(), _lifecycle);
		}
	}

	public void Report<T>(string type, T data)
	{
		var envelope = new ReportDataPoint<T>
		{
			data = data,
			type = type,
			ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
		};
		string json;
		try
		{
			json = JsonConvert.SerializeObject(envelope, UnitySerializationSettings.Instance);
		}
		catch (Exception ex)
		{
			// Serialization failures here are programmer errors (a result type that can't
			// be serialized); surface them via the error channel rather than crashing the
			// runner.
			_sink.EmitOutput("error", $"sandbox-reporter serialization failed: {ex.Message}");
			return;
		}
		_sink.EmitOutput(type ?? string.Empty, json);
	}

	public void Dispose() => _cancelBridge.Dispose();
}
