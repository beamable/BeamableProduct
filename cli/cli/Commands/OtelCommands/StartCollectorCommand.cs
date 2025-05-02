using Beamable.Common;
using Beamable.Server;
using cli.Services;
using System.Net;

namespace cli.OtelCommands;


[Serializable]
public class StartCollectorCommandArgs : CommandArgs
{
}

public class CollectorItemToDownload
{
	public string fileName;
	public string downloadUrl;
	public string filePath;
}

public class StartCollectorCommand : AppCommand<StartCollectorCommandArgs>
{
	public StartCollectorCommand() : base("start", "Starts the collector process")
	{
	}

	public override void Configure()
	{
	}

	public override async Task Handle(StartCollectorCommandArgs args)
	{
		AssertEnvironmentVars();//TODO this requirement is just while we don't have a way to get credentials from beamo

		string executablePath = CollectorManager.GetCollectorExecutablePath();

		if (!File.Exists(executablePath))
		{
			Log.Information($"Couldn't find collector at path: [{executablePath}]");
			Log.Information($"Starting download of collector...");

			var exeDownloadLink = CollectorManager.CollectorDownloadUrl.Replace("BEAM_VERSION", "0.0.0-PREVIEW.NIGHTLY-202504302222")
				.Replace("BEAM_FILE_NAME", CollectorManager.GetCollectorName());

			var configDownloadLink = CollectorManager.CollectorDownloadUrl.Replace("BEAM_VERSION", "0.0.0-PREVIEW.NIGHTLY-202504302222")
				.Replace("BEAM_FILE_NAME", CollectorManager.configFileName);

			List<CollectorItemToDownload> itemsToDownload = new List<CollectorItemToDownload>();

			itemsToDownload.Add(new CollectorItemToDownload()
			{
				downloadUrl = exeDownloadLink,
				filePath = executablePath
			});

			itemsToDownload.Add(new CollectorItemToDownload()
			{
				downloadUrl = configDownloadLink,
				filePath = Path.Combine(Path.GetDirectoryName(executablePath), CollectorManager.configFileName)
			});

			using var client = new HttpClient();
			client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

			try
			{
				foreach (var item in itemsToDownload)
				{
					using var response = await client.GetAsync(item.downloadUrl, HttpCompletionOption.ResponseHeadersRead);
					response.EnsureSuccessStatusCode();

					await using var stream = await response.Content.ReadAsStreamAsync();
					await using var fileStream = new FileStream(item.filePath, FileMode.Create, FileAccess.Write, FileShare.None);
					await stream.CopyToAsync(fileStream);
				}
			}
			catch (Exception ex)
			{
				throw new Exception($"Error when downloading collector binaries. Message=[{ex.Message}] StackTrace=[{ex.StackTrace}]");
			}

			Log.Information($"Finished downloading of collector...");
		}

		var processId = await CollectorManager.StartCollector(true, args.Lifecycle.Source, BeamableZLoggerProvider.LogContext.Value);

		Log.Information($"Collector with process id [{processId}] started successfully");
	}

	private void AssertEnvironmentVars()
	{
		var port = Environment.GetEnvironmentVariable("BEAM_COLLECTOR_DISCOVERY_PORT");
		if(string.IsNullOrEmpty(port))
		{
			throw new Exception("There is no port configured for the collector discovery");
		}

		if (!Int32.TryParse(port, out int portNumber))
		{
			throw new Exception("Invalid value for port");
		}

		var host = Environment.GetEnvironmentVariable("BEAM_COLLECTOR_DISCOVERY_HOST");

		if(string.IsNullOrEmpty(host))
		{
			throw new Exception("There is no host configured for the collector discovery");
		}

		var user = Environment.GetEnvironmentVariable("BEAM_CLICKHOUSE_USER");
		if(string.IsNullOrEmpty(user))
		{
			throw new Exception("There is no user configured for the collector startup");
		}

		var passd = Environment.GetEnvironmentVariable("BEAM_CLICKHOUSE_PASSWORD");
		if(string.IsNullOrEmpty(passd))
		{
			throw new Exception("There is no password configured for the collector startup");
		}
	}
}
