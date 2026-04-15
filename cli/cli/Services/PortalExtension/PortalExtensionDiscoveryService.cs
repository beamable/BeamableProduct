using Beamable.Server;
using Beamable.Server.Api.Notifications;
using cli.Portal;
using cli.Utils;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace cli.Services.PortalExtension;


[Serializable]
public class ExtensionBuildMetaData
{
	public string Name;
	public string ToolkitVersion;
	public PortalExtensionPackageProperties Properties;
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
			DiffInstructionsMetadata = buildData.DiffInstructionsMetadata,
			CurrentHash = buildData.CurrentHash
		};
	}
}

[Serializable]
public class ExtensionBuildData
{
	public bool IsFullBuild;
	public string FullData;

	public bool IsError;
	public string ErrorMessage;
	public string ErrorStackTrace;

	public DiffInstructions DiffInstructionsJs;
	public DiffInstructions DiffInstructionsCss;
	public DiffInstructions DiffInstructionsMetadata;

	public string CurrentHash;
}

public class PortalExtensionObserver
{
	private static readonly string[] _defaultFilesExtensionsToObserve = new string[] { "css", "svelte", "js", "html" };
	private bool _alreadyStarted;

	private PortalExtensionDef _metaData;

	private PortalExtensionBuildHistory _buildHistory;

	private CancellationTokenSource _cancelToken;
	private IMicroserviceNotificationsApi _notificationsApi;
	private IMicroserviceAttributes _attributes;
	private BeamoLocalManifest _manifest;

	public string AppFilesPath => _metaData.AbsolutePath;

	public PortalExtensionDef ExtensionMetaData
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

	public List<string> FileExtensions = new List<string>();

	public void CancelDiscovery()
	{
		_cancelToken.Cancel();
	}

	public void ConfigureServiceData(IMicroserviceNotificationsApi notificationApi, IMicroserviceAttributes attributes, BeamoLocalManifest manifest)
	{
		_notificationsApi = notificationApi;
		_attributes = attributes;
		_manifest = manifest;
	}

	public virtual void BuildExtension()
	{
		if (_buildHistory == null)
		{
			_buildHistory = new PortalExtensionBuildHistory(10);
		}

		StartProcessResult result = StartProcessUtil.Run("npm", "run beam-build", useShell: true, workingDirectoryPath: AppFilesPath).WaitForResult();

		if (result.exit != 0)
		{
			_buildHistory.Add(new PortalExtensionBuild()
			{
				 IsError = true,
				 ErrorMessage = result.stderr,
				 Checksum = ""
			});
			return;
		}

		try
		{
			var metadataContent = new ExtensionBuildMetaData
			{
				Name = ExtensionMetaData.Name,
				ToolkitVersion = ExtensionMetaData.GetToolkitVersion(),
				Properties = ExtensionMetaData.Properties
			};

			var metadataPath = Path.Combine(AppFilesPath, "assets", "metadata.json");

			string metaDataDir = Path.GetDirectoryName(metadataPath);

			if (!Directory.Exists(metaDataDir))
			{
				Directory.CreateDirectory(metadataPath);
			}

			var metadataContentJson = JsonConvert.SerializeObject(metadataContent, Formatting.Indented);

			File.WriteAllText(metadataPath, metadataContentJson);

			var build = CreateAppBuildData();
			_buildHistory.Add(build);
		}
		catch (Exception e)
		{
			throw new CliException($"Failed to generate portal extension metadata file. \nCheck exception: [\n{e.Message}] \nStackTrace: [{e.StackTrace}]"
				.Trim());
		}

	}

	public void InstallDeps()
	{
		StartProcessResult result = StartProcessUtil.Run("npm", "install", useShell: true, workingDirectoryPath: AppFilesPath).WaitForResult();
		if (result.exit != 0)
		{
			throw new CliException($"Failed to generate portal extension dependencies. \nCheck errors: \n{result.stderr} \nAll logs: {result.stdout}"
				.Trim());
		}
	}

	public PortalExtensionBuild CreateAppBuildData()
	{
		var mainJsPath = Path.Combine(AppFilesPath, "assets", "index.js");
		var mainCssPath = Path.Combine(AppFilesPath, "assets", "style.css");
		var metadataPath = Path.Combine(AppFilesPath, "assets", "metadata.json");

		if (!File.Exists(mainJsPath) || !File.Exists(mainCssPath) || !File.Exists(metadataPath))
		{
			throw new CliException($"Could not find the portal extension built files. These should exist: [\"{mainJsPath}\", \"{mainCssPath}\", \"{metadataPath}\"]");
		}

		string[] currentJsLines = File.ReadLines(mainJsPath).ToArray();
		string[] currentCssLines = File.ReadLines(mainCssPath).ToArray();
		string[] currentMetadataLines = File.ReadLines(metadataPath).ToArray();

		var computedHash = GetBuildHash(currentJsLines, currentCssLines, currentMetadataLines);
		var bundle = ConvertBuiltFiles(new []{mainJsPath, mainCssPath, metadataPath});

		return new PortalExtensionBuild()
		{
			javascriptLines = currentJsLines,
			cssLines = currentCssLines,
			metadataLines = currentMetadataLines,
			FullBuild =  bundle,
			Checksum = computedHash,
		};
	}

	public ExtensionBuildData GetAppBuild(string clientHash)
	{
		if (_buildHistory.Get(clientHash, out var oldBuild))
		{
			var recentBuild = _buildHistory.GetFirst();

			if (recentBuild.IsError)
			{
				return new ExtensionBuildData() { IsFullBuild = true, ErrorMessage = recentBuild.ErrorMessage, };
			}


		}
		//
		//
		// // no sender hash, no stored previous hash or different hash than expected, means that we need to send the full build and sync the hashes
		// if (string.IsNullOrEmpty(clientHash) || _currentExtensionData == null || !clientHash.Equals(_currentExtensionData.previousBuildHash))
		// {
		// 	_currentExtensionData = new ExtensionCurrentData
		// 	{
		// 		previousLinesJs = currentJsLines,
		// 		previousLinesCss = currentCssLines,
		// 		previousLinesMetadata = currentMetadataLines,
		// 		previousBuildHash = computedHash,
		// 	};
		//
		// 	var bundle = ConvertBuiltFiles(new []{mainJsPath, mainCssPath, metadataPath});
		//
		// 	return new ExtensionBuildData()
		// 	{
		// 		IsFullBuild = true,
		// 		CurrentHash = computedHash,
		// 		FullData = bundle
		// 	};
		// }
		//
		// var diffJs = PortalExtensionDiff.GetDiffInstructions(_currentExtensionData.previousLinesJs, currentJsLines);
		// var diffCss = PortalExtensionDiff.GetDiffInstructions(_currentExtensionData.previousLinesCss, currentCssLines);
		// var diffMetadata = PortalExtensionDiff.GetDiffInstructions(_currentExtensionData.previousLinesMetadata, currentMetadataLines);
		//
		// var result = new ExtensionBuildData()
		// {
		// 	CurrentHash = computedHash,
		// 	IsFullBuild = false,
		// 	DiffInstructionsJs = diffJs,
		// 	DiffInstructionsCss = diffCss,
		// 	DiffInstructionsMetadata = diffMetadata,
		// };
		//
		// _currentExtensionData.previousLinesJs = currentJsLines;
		// _currentExtensionData.previousLinesCss = currentCssLines;
		// _currentExtensionData.previousLinesMetadata = currentMetadataLines;
		// _currentExtensionData.previousBuildHash = computedHash;

		return new ExtensionBuildData();
	}

	public static string GetBuildHash(string[] fileA, string[] fileB, string[] fileC)
	{
		var sequenceA = fileA.Select((val, index) => new KeyValuePair<string, string>($"A:{index}", val));

		var sequenceB = fileB.Select((val, index) => new KeyValuePair<string, string>($"B:{index}", val));

		var sequenceC = fileC.Select((val, index) => new KeyValuePair<string, string>($"C:{index}", val));

		var combined = sequenceA.Concat(sequenceB).Concat(sequenceC);

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

		using var watcher = new FileSystemWatcher(AppFilesPath);

		watcher.Filters.Clear();

		var fileExtensions = _defaultFilesExtensionsToObserve.ToList();
		fileExtensions.AddRange(FileExtensions);
		fileExtensions = fileExtensions.Distinct().ToList();

		foreach (var ext in fileExtensions)
		{
			watcher.Filters.Add($"*.{ext}");
		}

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
		string assetsFolder = $"assets{Path.DirectorySeparatorChar}"; 
		string nodeModuleFolder = $"node_modules{Path.DirectorySeparatorChar}";
		string beamableClients = Path.Combine("beamable", "clients");
		
		if (e.Name != null && (e.Name.Contains(assetsFolder) || e.Name.Contains(nodeModuleFolder) || e.Name.Contains(beamableClients)))
		{
			return; // this case we ignore because these are the build files
		}

		// generate microservice client files, in case a manually change happened to the package.json adding a new dep
		PortalExtensionAddDependencyCommand.GenerateDependenciesClients(AppFilesPath, _manifest);

		// build the app since there are new changes in the src files
		BuildExtension();

		//TODO: check this back once event subscriptions change
		_notificationsApi.NotifyServer(true, "notify-portalextension",
			new PortalExtensionNotifyPayload()
			{
				serviceName = _attributes.MicroserviceName ,
				extensionName = _metaData.Name,
				extensionProperties = _metaData.Properties
			});
	}

	[Serializable]
	public class PortalExtensionNotifyPayload
	{
		public string serviceName;
		public string extensionName;
		public PortalExtensionPackageProperties extensionProperties;
	}
}
