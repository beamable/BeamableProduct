using cli.Utils;

namespace cli.OtelCommands;

public class OtelStatusCommand : AtomicCommand<OtelStatusCommandArgs, OtelStatusResult>, ISkipManifest
{
	public OtelStatusCommand() : base("status", "Get the Otel Logs status on current local machine")
	{
	}

	public override void Configure()
	{
	}

	public override async Task<OtelStatusResult> GetResult(OtelStatusCommandArgs args)
	{
		string otelDirectory = args.ConfigService.ConfigTempOtelDirectoryPath;
		if (!Directory.Exists(otelDirectory))
		{
			return new OtelStatusResult();
		}

		DirectoryInfoUtils directoryInfo = DirectoryUtils.CalculateDirectorySize(otelDirectory);
		string lastPublishedFilePath = Path.Join(otelDirectory, PushTelemetryCommand.LAST_PUBLISH_OTEL_FILE_NAME);
		long lastPublished = 0;
		if(File.Exists(lastPublishedFilePath))
		{
			string fileText = await File.ReadAllTextAsync(lastPublishedFilePath);
			long.TryParse(fileText, out lastPublished);
		}

		return new OtelStatusResult()
		{
			FileCount = directoryInfo.FileCount, FolderSize = directoryInfo.Size, LastPublishTimestamp = lastPublished
		};

	}
}

[Serializable]
public class OtelStatusCommandArgs : CommandArgs
{
	public List<string> Paths;
}

[Serializable]
public class OtelStatusResult
{
	public int FileCount;
	public long FolderSize;
	public long LastPublishTimestamp;
}
