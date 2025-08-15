using OpenTelemetry.Exporter;

namespace beamable.microservice.otel.exporter;

public class MicroserviceOtelExporterOptions
{
	private string? _endpoint;
	private OtlpExportProtocol? _protocol;

	public string OtlpEndpoint
	{
		get
		{
			if (this._endpoint == null)
			{
				return $"http://127.0.0.1:4318";
			}

			return this._endpoint;
		}
		set
		{
			if (value is null)
			{
				throw new Exception("Value for Endpoint property can't be null");
			}

			this._endpoint = value;
		}
	}

	public OtlpExportProtocol Protocol{
		get
		{
			if (this._protocol == null)
			{
				return OtlpExportProtocol.Grpc;
			}

			return (OtlpExportProtocol)this._protocol;
		}
		set
		{
			this._protocol = value;
		}
	}
}
