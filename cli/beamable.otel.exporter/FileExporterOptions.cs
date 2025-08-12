namespace beamable.otel.exporter;

public class FileExporterOptions
{
	private string? _path;

	public string ExportPath
	{
		get
		{
			if (this._path == null)
			{
				return "."; //TODO change this to a better default path
			}

			return this._path;
		}
		set
		{
			if (value is null)
			{
				throw new Exception("Value for ExportPath can't be null");
			}

			this._path = value;
		}
	}
}
