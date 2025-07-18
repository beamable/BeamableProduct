using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;

namespace cli.OtelCommands;

[Serializable]
public class PushTelemetryCollectorCommandArgs : CommandArgs
{
}

public class PushTelemetryCollectorCommand : AppCommand<PushTelemetryCollectorCommandArgs>
{
	public PushTelemetryCollectorCommand() : base("push", "Pushes local telemetry data saved through the BeamableExporter to a running collector")
	{
	}

	public override void Configure()
	{
	}

	public override Task Handle(PushTelemetryCollectorCommandArgs args)
	{
		//TODO remove this later, testing sending through otlp
		// otlp endpoint 127.0.0.1:4355
// 		{
// 			var exporterOptions = new OtlpExporterOptions
// 			{
// 				Endpoint = new Uri($"http://127.0.0.1:4355/v1/logs"),
// 				Protocol = OtlpExportProtocol.HttpProtobuf,
// 			};
//
// #pragma warning disable CA2000 // Dispose objects before losing scope
// 			BaseExporter<LogRecord> otlpExporter = new OtlpLogExporter(
// 				exporterOptions);
// #pragma warning restore CA2000 // Dispose objects before losing scope
//
// 			otlpExporter.Export(batch);
// 		}

		return Task.CompletedTask;
	}
}
