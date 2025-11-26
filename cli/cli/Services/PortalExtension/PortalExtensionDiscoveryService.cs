using Beamable.Server;
using cli.Utils;

namespace cli.Services.PortalExtension;

public class PortalExtensionDiscoveryService : Microservice
{
	[ClientCallable]
	public string RequestPortalExtensionData()
	{
		Log.Information("Trying to request portal extension data");

		var observer = Provider.GetService<PortalExtensionObserver>();

		if (observer.TryGetNewAppBuild(out string build))
		{
			return build;
		}

		Log.Information("No changes were detected since last build of portal extension");

		return string.Empty;
	}
}

public class PortalExtensionObserver
{
	private bool _alreadyStarted;
	private string _appPath;
	private bool _hasChanges = true;
	private CancellationTokenSource _cancelToken;

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

	public bool TryGetNewAppBuild(out string javascript)
	{
		if (!_hasChanges)
		{
			javascript = string.Empty;
			return false;
		}

		// First lets build the app
		var result = StartProcessUtil.Run("npm", "run build", workingDirectoryPath: _appPath);
		if (result.exit != 0)
		{
			Log.Error($"Failed to generate portal extension build. Check errors: \n{result.stderr}".Trim());
			javascript = string.Empty;
			return false;
		}

		// Now we read the generated javascript file and return it's contents
		var buildPath = Path.Combine(_appPath, "assets", "main.js");

		if (!File.Exists(buildPath))
		{
			throw new CliException($"Could not find the built file in [{buildPath}]");
		}

		_hasChanges = false;
		javascript = File.ReadAllText(buildPath);
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

	private void OnChanged(object sender, FileSystemEventArgs e)
	{
		if (e.Name != null && e.Name.Contains("assets/main"))
		{
			return; // this case we ignore because these are the build files
		}

		Log.Information($"Changed: {e.Name}", ConsoleColor.Cyan);
		_hasChanges = true;
	}
}
