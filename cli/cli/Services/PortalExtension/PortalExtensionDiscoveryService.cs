using Beamable.Server;
using Beamable.Server.Api.Notifications;
using cli.Utils;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace cli.Services.PortalExtension;

[Serializable]
public class ExtensionBuildData //TODO make this have a diff version number so we know if the diff algorithm changed
{
	public bool IsFullBuild;
	public string FullData;

	public DiffInstructions DiffInstructionsJs;
	public DiffInstructions DiffInstructionsCss;

	public string CurrentHash;
}

[Serializable]
public class ExtensionBuildMetaData
{
	public string ExtensionName;
	public string ExtensionType;
}

public enum PortalExtensionType
{
	TestPage,
	PlayerPage
}

public class PortalExtensionDiscoveryService : Microservice
{

	[ClientCallable]
	public ExtensionBuildData RequestPortalExtensionData(string currentHash = "")
	{
		var observer = Provider.GetService<PortalExtensionObserver>();

		ExtensionBuildData buildData = observer.GetAppBuild(currentHash);

		return new ExtensionBuildData
		{
			IsFullBuild = buildData.IsFullBuild,
			FullData = buildData.FullData,
			DiffInstructionsJs = buildData.DiffInstructionsJs,
			DiffInstructionsCss = buildData.DiffInstructionsCss,
			CurrentHash = buildData.CurrentHash
		};
	}

	[ClientCallable]
	public ExtensionBuildMetaData RequestMetaData()
	{
		var observer = Provider.GetService<PortalExtensionObserver>();
		return observer.ExtensionMetaData;
	}
}

public class PortalExtensionObserver
{
	public class ExtensionCurrentData
	{
		public string[] previousLinesJs;
		public string[] previousLinesCss;

		public string previousBuildHash;
	}


	private bool _alreadyStarted;
	private string _appPath;

	private ExtensionBuildMetaData _metaData;

	//TODO have this have a sorted list of data+hash (with a max size of 10 maybe), and try to find the one that matches and calculate the
	// diff between that one and the latest build, on each build we add the to the list and remove the oldest one
	private ExtensionCurrentData _currentExtensionData;

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

	public ExtensionBuildMetaData ExtensionMetaData
	{
		get
		{
			if (_metaData == null)
			{
				throw new Exception("The property ExtensionMetaData needs a valid path");
			}

			return _metaData;
		}
		set
		{
			if (value is null)
			{
				throw new Exception("Value for this property cannot be null");
			}

			_metaData = value;
		}
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

	public void BuildExtension() //TODO: improve this with more error handling
	{
		var result = StartProcessUtil.Run("npm", "run build", workingDirectoryPath: _appPath);
		if (result.exit != 0)
		{
			Log.Error($"Failed to generate portal extension build. Check errors: \n{result.stderr}".Trim());
		}
	}

	public void InstallDeps() //TODO: improve this with more error handling
	{
		var result = StartProcessUtil.Run("npm", "install", workingDirectoryPath: _appPath);
		if (result.exit != 0)
		{
			Log.Error($"Failed to generate portal extension dependencies. Check errors: \n{result.stderr}".Trim());
		}
	}

	public ExtensionBuildData GetAppBuild(string clientHash)
	{
		var mainJsPath = Path.Combine(_appPath, "assets", "main.js");
		var mainCssPath = Path.Combine(_appPath, "assets", "main.css");

		if (!File.Exists(mainJsPath) || !File.Exists(mainCssPath))
		{
			throw new CliException($"Could not find the portal extension built files. These should exist: [\"{mainJsPath}\", \"{mainCssPath}\"]");
		}

		string[] currentJsLines = File.ReadLines(mainJsPath).ToArray();
		string[] currentCssLines = File.ReadLines(mainCssPath).ToArray();

		var computedHash = GetBuildHash(currentJsLines, currentCssLines);


		// no sender hash, no stored previous hash or different hash than expected, means that we need to send the full build and sync the hashes
		if (string.IsNullOrEmpty(clientHash) || _currentExtensionData == null || !clientHash.Equals(_currentExtensionData.previousBuildHash))
		{
			_currentExtensionData = new ExtensionCurrentData
			{
				previousLinesJs = currentJsLines,
				previousLinesCss = currentCssLines,
				previousBuildHash = computedHash,
			};

			var bundle = ConvertBuiltFiles(new []{mainJsPath, mainCssPath});

			return new ExtensionBuildData()
			{
				IsFullBuild = true,
				CurrentHash = computedHash,
				FullData = bundle
			};
		}

		var diffJs = PortalExtensionDiff.GetDiffInstructions(_currentExtensionData.previousLinesJs, currentJsLines);
		var diffCss = PortalExtensionDiff.GetDiffInstructions(_currentExtensionData.previousLinesCss, currentCssLines);

		var result = new ExtensionBuildData()
		{
			CurrentHash = computedHash,
			IsFullBuild = false,
			DiffInstructionsJs = diffJs,
			DiffInstructionsCss = diffCss
		};

		_currentExtensionData.previousLinesJs = currentJsLines;
		_currentExtensionData.previousLinesCss = currentCssLines;
		_currentExtensionData.previousBuildHash = computedHash;

		return result;
	}

	private static string GetBuildHash(string[] fileA, string[] fileB)
	{
		var sequenceA = fileA.Select((val, index) => new KeyValuePair<string, string>($"A:{index}", val));

		var sequenceB = fileB.Select((val, index) => new KeyValuePair<string, string>($"B:{index}", val));

		var combined = sequenceA.Concat(sequenceB);

		StringBuilder sb = new StringBuilder();
		foreach (var item in combined)
		{
			sb.Append($"[{item.Key},{item.Value}]");
		}

		using (SHA256 sha256 = SHA256.Create())
		{
			byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
			return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
		}
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
		if (e.Name != null && (e.Name.Contains("assets/main") || e.Name.Contains("node_modules/")))
		{
			return; // this case we ignore because these are the build files
		}

		// build the app since there are new changes in the src files
		BuildExtension();

		Log.Information($"Change detected in file: {e.Name}");

		_notificationsApi.NotifyServer(true, "notify-portalextension",
			new PortalExtensionNotifyPayload()
			{
				serviceName = _attributes.MicroserviceName ,
				extensionName = _metaData.ExtensionName,
				extensionType = _metaData.ExtensionType
			});
	}

	[Serializable]
	public class PortalExtensionNotifyPayload
	{
		public string serviceName;
		public string extensionName;
		public string extensionType;
	}
}
