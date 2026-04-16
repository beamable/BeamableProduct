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

		return buildData;
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
	private BeamActivity _rootActivity;
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

	public BeamActivity RootActivity
	{
		get => _rootActivity;
		set => _rootActivity = value;
	}

	public string MetadataPath => Path.Combine(AppFilesPath, "assets", "metadata.json");

	public List<string> FileExtensions = new List<string>();

	public void CancelDiscovery()
	{
		_cancelToken.Cancel();
	}

	public void ConfigureServiceData(IMicroserviceNotificationsApi notificationApi, IMicroserviceAttributes attributes, BeamActivity beamActivity, BeamoLocalManifest manifest)
	{
		_notificationsApi = notificationApi;
		_attributes = attributes;
		_rootActivity = beamActivity;
		_manifest = manifest;
	}

	public void ConfigureServiceData(PortalExtensionDef extensionMetaData, BeamActivity beamActivity)
	{
		_metaData = extensionMetaData;
		_rootActivity = beamActivity;
	}

	public void BuildExtension()
	{
		using var childActivity = _rootActivity.CreateChild("Build extension");

		if (_buildHistory == null)
		{
			_buildHistory = new PortalExtensionBuildHistory(10);
		}

		StartProcessResult result = StartProcessUtil.Run("npm", "run beam-build", useShell: true, workingDirectoryPath: AppFilesPath).WaitForResult();
		CreateMetaDataFile();

		if (result.exit != 0)
		{
			_buildHistory.Add(new PortalExtensionBuild()
			{
				 IsError = true,
				 ErrorMessage = result.stderr,
				 Checksum = Guid.NewGuid().ToString() // Just put a random guid here, this is just so it's not confused with an empty string, that means that no build was found
			});
			return;
		}

		try
		{
			var build = CreateAppBuildData();
			_buildHistory.Add(build);

			var mainJsPath = Path.Combine(AppFilesPath, "assets", "index.js");
			var mainCssPath = Path.Combine(AppFilesPath, "assets", "style.css");

			long metadataBytes = File.Exists(MetadataPath) ? new FileInfo(MetadataPath).Length : 0;
			long jsSizeBytes = File.Exists(mainJsPath) ? new FileInfo(mainJsPath).Length : 0;
			long cssSizeBytes = File.Exists(mainCssPath) ? new FileInfo(mainCssPath).Length : 0;


			childActivity.SetTags(new TelemetryAttributeCollection()
				.With(TelemetryAttributes.PortalExtensionMetadataSize(metadataBytes))
				.With(TelemetryAttributes.PortalExtensionJsSize(jsSizeBytes))
				.With(TelemetryAttributes.PortalExtensionCssSize(cssSizeBytes))
				.With(TelemetryAttributes.PortalExtensionTotalSize(metadataBytes + jsSizeBytes + cssSizeBytes))
				.With(TelemetryAttributes.PortalExtensionName(_metaData.Name)));
		}
		catch (Exception e)
		{
			throw new CliException($"Failed to generate portal extension metadata file. \nCheck exception: [\n{e.Message}] \nStackTrace: [{e.StackTrace}]"
				.Trim());
		}
	}

	public void InstallDeps()
	{
		using var childActivity = _rootActivity.CreateChild("Install Dependencies");

		StartProcessResult result = StartProcessUtil.Run("npm", "install", useShell: true, workingDirectoryPath: AppFilesPath).WaitForResult();
		if (result.exit != 0)
		{
			throw new CliException($"Failed to generate portal extension dependencies. \nCheck errors: \n{result.stderr} \nAll logs: {result.stdout}"
				.Trim());
		}

		childActivity.SetTag(TelemetryAttributes.PortalExtensionName(_metaData.Name));
		// Don't need to track for Duration for install as Activity already does it
	}

	private void CreateMetaDataFile()
	{
		var metadataContent = new ExtensionBuildMetaData
		{
			Name = ExtensionMetaData.Name,
			ToolkitVersion = ExtensionMetaData.GetToolkitVersion(),
			Properties = ExtensionMetaData.Properties
		};

		string metaDataDir = Path.GetDirectoryName(MetadataPath);

		if (!Directory.Exists(metaDataDir))
		{
			Directory.CreateDirectory(MetadataPath);
		}

		var metadataContentJson = JsonConvert.SerializeObject(metadataContent, Formatting.Indented);

		File.WriteAllText(MetadataPath, metadataContentJson);
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

	public (string hash, string bundle) GetFullBundleWithOnlyMetadata()
	{
		string[] currentMetadataLines = File.ReadLines(MetadataPath).ToArray();
		var computedHash = GetBuildHash(Array.Empty<string>(), Array.Empty<string>(), currentMetadataLines);
		var bundle = ConvertBuiltFiles(new []{MetadataPath});

		return (computedHash, bundle);
	}

	public ExtensionBuildData GetAppBuild(string clientHash)
	{
		var recentBuild = _buildHistory.GetFirst();

		if (recentBuild.IsError)
		{
			(string hash, string bundle) = GetFullBundleWithOnlyMetadata();
			return new ExtensionBuildData() { IsError = true, ErrorMessage = recentBuild.ErrorMessage, ErrorStackTrace = "", FullData = bundle, CurrentHash = hash };
		}

		if (_buildHistory.Get(clientHash, out var oldBuild))
		{
			// calculate diff
			var diffJs = PortalExtensionDiff.GetDiffInstructions(oldBuild.javascriptLines, recentBuild.javascriptLines);
			var diffCss = PortalExtensionDiff.GetDiffInstructions(oldBuild.cssLines, recentBuild.cssLines);
			var diffMetadata = PortalExtensionDiff.GetDiffInstructions(oldBuild.metadataLines, recentBuild.metadataLines);

			return new ExtensionBuildData()
			{
				CurrentHash = recentBuild.Checksum,
				IsFullBuild = false,
				DiffInstructionsJs = diffJs,
				DiffInstructionsCss = diffCss,
				DiffInstructionsMetadata = diffMetadata,
			};
		}

		return new ExtensionBuildData()
		{
			IsFullBuild = true,
			CurrentHash = recentBuild.Checksum,
			FullData = recentBuild.FullBuild,
		};
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
