using Beamable.Api;
using Beamable.Api.CloudSaving;
using Beamable.Common;
using Beamable.Coroutines;
using Beamable.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

namespace Beamable.Player.CloudSaving
{

	public enum CloudSaveStatus
	{
		Inactive,
		Initialized,
		Initializing,
		ConflictedData,
	}

	public struct DataConflictDetail
	{
		public string FileName;
		public DateTime LocalTimestamp;
		public DateTime CloudTimestamp;
		public int LocalFileSize;
		public int CloudFileSize;

		/// <summary>
		/// The path for the temporarily download Remote File.
		/// </summary>
		public string RemoteFilePath;
	}

	public enum ConflictUsage
	{
		UseLocal,
		UseCloud,
	}

	[Serializable]
	public class CloudSavingManifest
	{
		public List<CloudSaveEntry> manifest = new();
		public bool replacement;
	}

	[Serializable]
	public class CloudSaveEntry
	{
		public string key;
		public int size;
		public long lastModified;
		public string eTag;
		[NonSerialized]
		public bool isModified;

		public CloudSaveEntry(string key, int size, long lastModified, string eTag, bool isModified)
		{
			this.key = key;
			this.size = size;
			this.lastModified = lastModified;
			this.eTag = eTag;
		}
	}

	public class PlayerCloudSavingConfiguration
	{
		/// <summary>
		/// When this option is enabled, the AutoConflict will use the latest save for data conflict.
		/// When disabled you will need to use the <see cref="GetDataConflictDetails"/> and <see cref="ResolveDataConflict"/> or
		/// the <see cref="OnDataConflict"/> action.
		/// </summary>
		public bool UseAutoConflictResolver = true;
		/// <summary>
		/// This option enables or disabled the AutoCloud.
		/// AutoCloud is an automatic process that automatically save all files inside the <see cref="LocalDataFullPath"/>
		/// It only saves files from this folder, all sub-folder will be ignored.
		/// When disabled, you will need to save files using the SaveData functions
		/// </summary>
		public bool UseAutoCloud = false;
		/// <summary>
		/// This Function allows you to override the DataConflictResolver.
		/// This will automatically be called in the Init process if there is a Conflict to be resolved.
		/// </summary>
		public Func<ConflictUsage, DataConflictDetail> OnDataConflict;
		/// <summary>
		/// This Function allows you to override the JsonSerializer for <see cref="ICloudSavingService.SaveData{T}"/>
		/// </summary>
		public Func<object, string> CustomJsonSerializer;
		/// <summary>
		/// This Function allows you to override the JsonDeserializer for <see cref="ICloudSavingService.LoadData{T}"/>
		/// </summary>
		public Func<string, object> CustomJsonDeserializer;
	}

	public class PlayerCloudSaving : ICloudSavingService
	{
		private readonly LocalSavePathDetail _localSaveInformation;
		private readonly PlayerCloudSavingConfiguration _configuration;
		private readonly CoroutineService _coroutineService;
		
		private CloudSavingManifest _localManifest;
		private WaitForSecondsRealtime _routineWaitInterval;
		private Coroutine _watcherRoutine;
		

		public string LocalDataFullPath => _localSaveInformation.DataPath;
		
		public Action<ManifestResponse> OnManifestUpdated { get; set; }
		public Action<CloudSavingError> OnCloudSavingError { get; set; }
		public CloudSaveStatus ServiceStatus { get; private set; }

		public PlayerCloudSaving(IPlatformService platformService, PlayerCloudSavingConfiguration configuration, CoroutineService coroutineService)
		{
			_localSaveInformation =  new LocalSavePathDetail(platformService);
			_configuration = configuration;
			_coroutineService = coroutineService;
			ServiceStatus = CloudSaveStatus.Inactive;
		}
		
		public Promise<CloudSaveStatus> Init(int pollingIntervalSeconds = 10)
		{
			if (ServiceStatus != CloudSaveStatus.Inactive)
			{
				Debug.LogWarning("PlayerCloudSaving is already initialized. To ReInitialize use the ReInit method.");
				return Promise<CloudSaveStatus>.Successful(ServiceStatus); 
			}

			_routineWaitInterval = new WaitForSecondsRealtime(pollingIntervalSeconds);
			
			SetupFolders();

			LoadLocalManifest();
			
			// Request Cloud Manifest
			
			// Check and Resolve Conflicts (if possible)
			
			// Download missing cloud Data/Upload local data

			StopWatcherRoutine();
			_watcherRoutine = _coroutineService.StartNew("FileWatcher", _configuration.UseAutoCloud ? AutoCloudFileWatcherRoutine() : ManifestWatcherRoutine());
			

			ServiceStatus = CloudSaveStatus.Initialized;
			return Promise<CloudSaveStatus>.Successful(ServiceStatus);
		}

		private async void LoadLocalManifest()
		{
			_localManifest = File.Exists(_localSaveInformation.ManifestPath)
				? await BeamUnityFileUtils.ReadJsonContent<CloudSavingManifest>(_localSaveInformation.ManifestPath)
				: new CloudSavingManifest();
		}

		public Promise<CloudSaveStatus> ReInit(int pollingIntervalSeconds = 10)
		{
			StopWatcherRoutine();
			// Stop all webRequests
			ServiceStatus = CloudSaveStatus.Inactive;
			return Init(pollingIntervalSeconds);
		}
		public Promise<bool> ForceUploadLocalData()
		{
			throw new NotImplementedException();
		}
		public Promise<bool> ForceDownloadCloudData()
		{
			throw new NotImplementedException();
		}
		public Promise<IReadOnlyList<DataConflictDetail>> GetDataConflictDetails()
		{
			throw new NotImplementedException();
		}
		public Promise<Unit> ResolveDataConflict(string fileName, bool useLocalData)
		{
			throw new NotImplementedException();
		}
		public async Promise<Unit> SaveStringData(string fileName, string content)
		{
			string filePath = Path.Combine(_localSaveInformation.DataPath, fileName);
			var fileInfo = await BeamUnityFileUtils.WriteStringFile(filePath, content);
			AddOrUpdateLocalManifestEntry(fileInfo);
			return PromiseBase.Unit;
		}

		public async Promise<Unit> SaveByteData(string fileName, byte[] content)
		{
			string filePath = Path.Combine(_localSaveInformation.DataPath, fileName);
			var fileInfo = await BeamUnityFileUtils.WriteBytesFile(filePath, content);
			AddOrUpdateLocalManifestEntry(fileInfo);
			return PromiseBase.Unit;
		}
		public async Promise<Unit> SaveData<T>(string fileName, T contentData)
		{
			string filePath = Path.Combine(_localSaveInformation.DataPath, fileName);
			var fileInfo = await BeamUnityFileUtils.WriteJsonContent(filePath, contentData, _configuration.CustomJsonSerializer);
			AddOrUpdateLocalManifestEntry(fileInfo);
			return PromiseBase.Unit;
		}
		
		public Promise<string> LoadStringData(string fileName)
		{
			string filePath = Path.Combine(_localSaveInformation.DataPath, fileName);
			return BeamUnityFileUtils.ReadStringFile(filePath);
		}
		public Promise<byte[]> LoadByteData(string fileName)
		{
			string filePath = Path.Combine(_localSaveInformation.DataPath, fileName);
			return BeamUnityFileUtils.ReadBytesFile(filePath);
		}
		public Promise<T> LoadData<T>(string fileName)
		{
			string filePath = Path.Combine(_localSaveInformation.DataPath, fileName);
			return BeamUnityFileUtils.ReadJsonContent<T>(filePath, _configuration.CustomJsonDeserializer);
		}
		public Promise<Unit> ArchiveSavedData(string fileName)
		{
			int findIndex = FindEntryFindInManifest(fileName);
			if (findIndex != -1)
			{
				_localManifest.manifest.RemoveAt(findIndex);
				// Ask Backend to Delete File on Cloud
			}
			string filePath = Path.Combine(_localSaveInformation.DataPath, fileName);
			return BeamUnityFileUtils.ArchiveFile(filePath, _localSaveInformation.ArchivePath).Then(_ =>
			{
				CommitLocalManifest();
			});
			
		}
		public Promise<bool> RenameSavedData(string fileName, string newFileName)
		{
			int findIndex = FindEntryFindInManifest(fileName);
			if (findIndex != -1)
			{
				var entry = _localManifest.manifest[findIndex];
				entry.isModified = true;
				entry.key = newFileName;
				// Ask Backend to Rename/Delete File on Cloud
			}
			string filePath = Path.Combine(_localSaveInformation.DataPath, fileName);
			return BeamUnityFileUtils.RenameFile(filePath, newFileName).Then(_ =>
			{
				CommitLocalManifest();
			});
		}

		public Promise<Unit> ForgetData(string fileName)
		{
			if (_configuration.UseAutoCloud)
			{
				Debug.LogWarning("This functions doesn't work well when UseAutoCloud is enabled as it will automatically " +
				                 "adds the file to the manifest again when checking for files on AutoCloud.");
			}
			else
			{
				int findIndex = FindEntryFindInManifest(fileName);
				if (findIndex != -1)
				{
					_localManifest.manifest.RemoveAt(findIndex);
					// Ask Backend to Delete File on Cloud
					CommitLocalManifest();
				}	
			}
			return Promise<Unit>.Successful(PromiseBase.Unit);
		}
		
		private void SetupFolders()
		{
			Directory.CreateDirectory(_localSaveInformation.DataPath);
			Directory.CreateDirectory(_localSaveInformation.ArchivePath);
			Directory.CreateDirectory(_localSaveInformation.TempFolderPath);
		}
		
		private void AddOrUpdateLocalManifestEntry(FileInfo fileInfo)
		{
			string fileName = fileInfo.Name;
			int contentLength = (int) fileInfo.Length;
			long lastModified = long.Parse(fileInfo.LastWriteTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture));
			string checkSum = GenerateCheckSum(fileInfo);
			
			// Find Manifest Entries ignoring case and using invariant culture.
			int findIndex = FindEntryFindInManifest(fileName);
			if (findIndex == -1)
			{
				_localManifest.manifest.Add(new CloudSaveEntry(fileName, contentLength, lastModified, checkSum, true));
			}
			else if(_localManifest.manifest[findIndex].eTag != checkSum)
			{
				CloudSaveEntry cloudSaveEntry = _localManifest.manifest[findIndex];
				cloudSaveEntry.isModified = true;
				cloudSaveEntry.eTag = checkSum;
				cloudSaveEntry.size = contentLength;
				cloudSaveEntry.lastModified = lastModified;
			}
		}

		private int FindEntryFindInManifest(string fileName)
		{
			return _localManifest.manifest.FindIndex(x => string.Equals(x.key, fileName, StringComparison.InvariantCultureIgnoreCase));
		}

		private string GenerateCheckSum(FileInfo fileInfo)
		{
			using (var md5 = MD5.Create())
			{
				using (var stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty);
				}
			}
		}

		private IEnumerator AutoCloudFileWatcherRoutine()
		{
			while (true)
			{
				yield return _routineWaitInterval;
				
				// Check files under Data Folder and add them to the manifest .
			}
		}

		private IEnumerator ManifestWatcherRoutine()
		{
			while (true)
			{
				yield return _routineWaitInterval;

				var changedEntries = _localManifest.manifest.Where(entry => entry.isModified).ToList();
				// Upload Entries
				if(changedEntries.Count == 0)
					continue;
				
				CommitLocalManifest();
				changedEntries.ForEach(entry => entry.isModified = false);

			}
		}
		
		private void StopWatcherRoutine()
		{
			if (_watcherRoutine == null)
			{
				return;
			}

			_coroutineService.StopCoroutine(_watcherRoutine);
			_watcherRoutine = null;
		}

		private async void CommitLocalManifest()
		{
			await BeamUnityFileUtils.WriteJsonContent(_localSaveInformation.ManifestPath, _localManifest,
			                                          prettyPrint: true);
		}

		private class LocalSavePathDetail
		{
			private const string BeamableFolder = "beamable";
			private const string PlayerCloudSaveFolder = "playercloudsave";
			private const string DataFolder = "data";
			private const string TempFolder = "temp";
			private const string ArchiveFolder = "archive";
			private const string ManifestName = "cloudDataManifest.json";
			
			private IPlatformService _platformService;

			public LocalSavePathDetail(IPlatformService platformService)
			{
				_platformService = platformService;
			}

			private string RootPath => Path.Combine(Application.persistentDataPath, BeamableFolder, PlayerCloudSaveFolder);
			private string BasePath =>
				Path.Combine(RootPath, _platformService.Cid, _platformService.Pid, _platformService.User.id.ToString());
		
			public string ManifestPath => Path.Combine(BasePath, ManifestName);
			public string TempFolderPath => Path.Combine(BasePath, TempFolder);
			public string ArchivePath => Path.Combine(BasePath, ArchiveFolder);
			public string DataPath => Path.Combine(BasePath, DataFolder);
		}
		
	}
}
