using Beamable.Server;
using Beamable.Server.Api.Notifications;
using cli.Portal;
using cli.Services.Web;
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

	/// <summary>
	/// The selector names this extension exposes via <c>&lt;BeamExtensionSite selector="..." /&gt;</c>
	/// (e.g. <c>top</c>, <c>bottom</c>, <c>B-top</c>), scanned once at build time. Combined with
	/// <see cref="Properties"/>'s <c>mount.page</c>, this tells the CLI/Portal which selectors live at
	/// which URL. May be null/empty (or absent entirely in metadata built before this field existed).
	/// </summary>
	public List<string> ExtensionSites;
}

public class PortalExtensionDiscoveryService : Microservice
{

	// Note: this creates source code leaking, even tho the service has an ever changing Guid
	[Callable]
	public ExtensionBuildData RequestPortalExtensionData(string currentHash = "")
	{
		var observer = Provider.GetService<PortalExtensionObserver>();

		ExtensionBuildData buildData = observer.GetAppBuild(currentHash);

		return buildData;
	}
}

/// <summary>
/// Zone (cid.zid) variant of <see cref="PortalExtensionDiscoveryService"/>. Used as the route source when a
/// portal extension declares <c>beamable.serviceScope: "zone"</c>, so its backing service boots as a
/// <see cref="ZoneMicroservice"/> (no realm SDK). The route body is identical — it only needs
/// <see cref="ZoneMicroservice.Provider"/> to reach the neutral <see cref="PortalExtensionObserver"/>.
/// </summary>
public class PortalExtensionDiscoveryZoneService : ZoneMicroservice
{
	[Callable]
	public ExtensionBuildData RequestPortalExtensionData(string currentHash = "")
	{
		var observer = Provider.GetService<PortalExtensionObserver>();
		return observer.GetAppBuild(currentHash);
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

	public int DiffAlgorithmVersion;
	public DiffInstructions DiffInstructionsJs;
	public DiffInstructions DiffInstructionsCss;
	public DiffInstructions DiffInstructionsMetadata;

	public string CurrentHash;
}

public class PortalExtensionObserver
{
	private static readonly string[] _defaultFilesExtensionsToObserve = new string[] { "css", "js", "html", "tsx", "jsx", "ts" };
	private bool _alreadyStarted;

	private PortalExtensionDef _metaData;

	private PortalExtensionBuildHistory _buildHistory;

	private CancellationTokenSource _cancelToken;

	// The watcher raises OnChanged on thread-pool threads with no serialisation, so two events can
	// reach BuildExtension at once and collide writing metadata.json. Rebuilds are serialised here.
	private readonly object _buildLock = new object();
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
		lock (_buildLock)
		{
			BuildExtensionUnsafe();
		}
	}

	private void BuildExtensionUnsafe()
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
			// Bundlers report the failing import/syntax error on stdout as often as on stderr, so keep
			// whichever one actually carries the message.
			var buildError = string.IsNullOrWhiteSpace(result.stderr) ? result.stdout : result.stderr;

			// Without this the reason only ever reaches the portal payload, never the terminal the user is
			// watching, so a broken extension looks like it silently did nothing.
			Log.Error("Portal extension [{name}] failed to build. npm exited with {exit}.\n{buildError}",
				_metaData.Name, result.exit, buildError);

			_buildHistory.Add(new PortalExtensionBuild()
			{
				 IsError = true,
				 ErrorMessage = buildError,
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

		// An extension pinning a local developer build of the toolkit (0.0.123-*) can only resolve it from
		// the local registry — npmjs has never heard of that version — so the install has to be routed
		// there. Contributes nothing for a normal, published pin.
		var installArgs = "install" + WebLocalRegistryService.InstallArgsFor(AppFilesPath);

		StartProcessResult result = StartProcessUtil.Run("npm", installArgs, useShell: true, workingDirectoryPath: AppFilesPath).WaitForResult();
		if (result.exit != 0)
		{
			throw new CliException($"Failed to generate portal extension dependencies. \nCheck errors: \n{result.stderr} \nAll logs: {result.stdout}"
				.Trim());
		}

		childActivity.SetTag(TelemetryAttributes.PortalExtensionName(_metaData.Name));
		// Don't need to track for Duration for install as Activity already does it
	}

	/// <summary>
	/// Writes <c>assets/metadata.json</c>, creating the assets folder if the build has not produced it yet.
	/// Public so the folder/stale-directory handling can be covered without shelling out to npm.
	/// </summary>
	public void CreateMetaDataFile()
	{
		var metadataContent = new ExtensionBuildMetaData
		{
			Name = ExtensionMetaData.Name,
			ToolkitVersion = ExtensionMetaData.GetToolkitVersion(),
			Properties = ExtensionMetaData.Properties,
			// Scan the source once, at build, so listing/creating extensions later never has to.
			ExtensionSites = RemotePortalConfigService.ScanExtensionSiteSelectors(ExtensionMetaData.AbsolutePath)
		};

		var metadataContentJson = JsonConvert.SerializeObject(metadataContent, Formatting.Indented);

		try
		{
			string metaDataDir = Path.GetDirectoryName(MetadataPath);

			// Create the *assets* folder, not the metadata file's own path. Creating MetadataPath here left a
			// directory named "metadata.json" behind, and every later run then failed the write below with
			// "Access to the path ... is denied" no matter what the build did.
			if (!string.IsNullOrEmpty(metaDataDir) && !Directory.Exists(metaDataDir))
			{
				Directory.CreateDirectory(metaDataDir);
			}

			// Self-heal a workspace an earlier CLI already poisoned that way. Nothing else ever puts a
			// directory at this path, so removing it is safe and saves the user a manual delete.
			if (Directory.Exists(MetadataPath))
			{
				Directory.Delete(MetadataPath, true);
			}

			File.WriteAllText(MetadataPath, metadataContentJson);
		}
		catch (Exception e)
		{
			// A raw IO exception here escapes the CliException handler that wraps the extension startup and
			// takes the whole `beam project run` process down; surface it as a CLI error instead.
			throw new CliException(
				$"Failed to write the portal extension metadata file at [{MetadataPath}]. Message = [{e.Message}] StackTrace = [{e.StackTrace}]");
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
				DiffAlgorithmVersion = PortalExtensionDiff.AlgorithmVersion
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

		var fileExtensions = _defaultFilesExtensionsToObserve.ToList();
		fileExtensions.AddRange(FileExtensions);
		fileExtensions = fileExtensions.Distinct().ToList();

		// Watch the extension itself plus any shared libraries it depends on, so editing a linked
		// library rebuilds the extension that consumes it.
		var watchPaths = new List<string> { AppFilesPath };
		watchPaths.AddRange(GetLinkedLibraryPaths());

		var watchers = new List<FileSystemWatcher>();
		try
		{
			foreach (var watchPath in watchPaths)
			{
				var watcher = new FileSystemWatcher(watchPath);
				watcher.Filters.Clear();

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

				watchers.Add(watcher);
			}

			while (!token.IsCancellationRequested)
			{
				await Task.Delay(250, token);
			}
		}
		finally
		{
			foreach (var watcher in watchers)
			{
				watcher.Dispose();
			}
		}
	}

	/// <summary>
	/// Resolves the real source directories of any "file:" library dependencies declared in the
	/// extension's package.json, so they can be watched for live-rebuild alongside the extension.
	/// </summary>
	private List<string> GetLinkedLibraryPaths()
	{
		var paths = new List<string>();
		try
		{
			var packagePath = ExtensionMetaData.AbsolutePackageJsonPath;
			if (!File.Exists(packagePath))
			{
				return paths;
			}

			var root = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(packagePath));
			if (root["dependencies"] is not Newtonsoft.Json.Linq.JObject dependencies)
			{
				return paths;
			}

			foreach (var (_, token) in dependencies)
			{
				var value = token?.ToString();
				if (string.IsNullOrEmpty(value) || !value.StartsWith("file:"))
				{
					continue;
				}

				var relPath = value.Substring("file:".Length);
				var resolved = Path.GetFullPath(Path.Combine(AppFilesPath, relPath));
				if (Directory.Exists(resolved))
				{
					paths.Add(resolved);
				}
			}
		}
		catch
		{
			// If the package.json can't be read we simply don't add extra watchers.
		}

		return paths;
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

		// A FileSystemWatcher callback runs on a thread-pool thread, so anything that escapes here takes the
		// whole `beam project run` process down with it — killing every service and extension in the group,
		// not just this one. A failed rebuild has to stay a failed rebuild.
		try
		{
			// build the app since there are new changes in the src files
			BuildExtension();

			//TODO: check this back once event subscriptions change
			// TODO(zones): NotifyServer is realm-scoped (IMicroserviceNotificationsApi). A zone extension has no
			// realm notification channel, so _notificationsApi is null for zone today — hot-reload push is
			// skipped. Wire up a zone-appropriate notification once the zone event channel exists.
			_notificationsApi?.NotifyServer(true, "notify-portalextension",
				new PortalExtensionNotifyPayload()
				{
					serviceName = _attributes.MicroserviceName ,
					extensionName = _metaData.Name,
					extensionProperties = _metaData.Properties
				});
		}
		catch (Exception ex)
		{
			Log.Error($"Portal extension [{_metaData?.Name}] failed to rebuild after a file change: {ex.Message}");
		}
	}

	[Serializable]
	public class PortalExtensionNotifyPayload
	{
		public string serviceName;
		public string extensionName;
		public PortalExtensionPackageProperties extensionProperties;
	}
}
