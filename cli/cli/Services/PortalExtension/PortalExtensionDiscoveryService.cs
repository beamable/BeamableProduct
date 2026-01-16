using Beamable.Server;
using Beamable.Server.Api.Notifications;
using cli.Utils;
using System.IO.Compression;

namespace cli.Services.PortalExtension;

public class PortalExtensionDiscoveryService : Microservice
{
	[ClientCallable]
	public string RequestPortalExtensionData()
	{
		var observer = Provider.GetService<PortalExtensionObserver>();

		if (observer.TryGetNewAppBuild(out string build))
		{
			return build;
		}

		return string.Empty;
	}
}

public class PortalExtensionObserver
{
	private bool _alreadyStarted;
	private string _appPath;
	private bool _hasChanges = true;
	private CancellationTokenSource _cancelToken;
	private IMicroserviceNotificationsApi _notificationsApi;
	private IMicroserviceAttributes _attributes;

	public string AppFilesPath
	{
		get
		{
			if (_appPath == null)
			{
				throw new Exception("The property AppFilesPath needs a valid path");
			}

			return _appPath;
		}
		set
		{
			if (value is null)
			{
				throw new Exception("Value for this property cannot be null");
			}

			_appPath = value;
		}
	}

	public PortalExtensionObserver()
	{
	}

	public void CancelDiscovery()
	{
		_cancelToken.Cancel();
	}

	public void ConfigureServiceData(IMicroserviceNotificationsApi notificationApi, IMicroserviceAttributes attributes)
	{
		_notificationsApi = notificationApi;
		_attributes = attributes;
	}

	public bool TryGetNewAppBuild(out string bundle)
	{
		if (!_hasChanges)
		{
			bundle = string.Empty;
			return false;
		}


		var mainJsPath = Path.Combine(_appPath, "assets", "main.js");
		var mainCssPath = Path.Combine(_appPath, "assets", "main.css");

		if (!File.Exists(mainJsPath) || !File.Exists(mainCssPath))
		{
			throw new CliException($"Could not find the portal extension built files. These should exist: [\"{mainJsPath}\", \"{mainCssPath}\"]");
		}

		bundle = ConvertBuiltFiles(new string[]{mainJsPath, mainCssPath});
		_hasChanges = false;
		return true;
	}

	public async Task StartExtensionFileWatcher(CancellationToken token = default)
	{
		if (_alreadyStarted)
		{
			return;
		}

		_alreadyStarted = true;

		using var watcher = new FileSystemWatcher(_appPath);

		watcher.Filters.Clear();
		watcher.Filters.Add("*.css");
		watcher.Filters.Add("*.svelte");
		watcher.Filters.Add("*.js");
		watcher.Filters.Add("*.html");

		watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;

		watcher.IncludeSubdirectories = true;
		watcher.EnableRaisingEvents = true;

		watcher.Changed += OnChanged;
		watcher.Created += OnChanged;
		watcher.Deleted += OnChanged;
		watcher.Renamed += OnChanged;

		while (!token.IsCancellationRequested)
		{
			await Task.Delay(250, token);
		}
	}

	private string ConvertBuiltFiles(string[] paths)
	{
		using (var memoryStream = new MemoryStream())
		{
			using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
			{
				foreach (var path in paths)
				{
					var file1Entry = archive.CreateEntry(Path.GetFileName(path));
					using (var entryStream = file1Entry.Open())
					using (var streamWriter = new StreamWriter(entryStream))
					{
						streamWriter.Write(File.ReadAllText(path));
					}
				}
			}

			memoryStream.Position = 0;
			byte[] zipBytes = memoryStream.ToArray();
			return Convert.ToBase64String(zipBytes);
		}
	}

	private void OnChanged(object sender, FileSystemEventArgs e)
	{
		if (e.Name != null && e.Name.Contains("assets/main"))
		{
			return; // this case we ignore because these are the build files
		}

		// run a build in case there was changes
		var result = StartProcessUtil.Run("npm", "run build", workingDirectoryPath: _appPath);
		if (result.exit != 0)
		{
			Log.Error($"Failed to generate portal extension build. Check errors: \n{result.stderr}".Trim());
		}

		Log.Information($"Change detected in file: {e.Name}");

		_notificationsApi.NotifyServer(true, "notify-portalextension",
			new PortalExtensionNotifyPayload() { serviceName = _attributes.MicroserviceName });

		_hasChanges = true;
	}

	[Serializable]
	public class PortalExtensionNotifyPayload
	{
		public string serviceName;
	}
}
