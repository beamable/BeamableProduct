﻿using Beamable.Api.Connectivity;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Dependencies;
using Beamable.Common.Runtime.Collections;
using Beamable.Coroutines;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Networking;

namespace Beamable.Api.CloudSaving
{
	/// <summary>
	/// This type defines the %Client main entry point for the %CloudSaving feature.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/cloud-save-feature">Cloud Save</a> feature documentation
	/// - See Beamable.API script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public class CloudSavingService : PlatformSubscribable<ManifestResponse, ManifestResponse>
	{
		private const string ServiceName = "cloudsaving";
		private ManifestResponse _localManifest;
		private IPlatformService _platform;
		private PlatformRequester _requester;
		private WaitForSecondsRealtime _delay;
		private CoroutineService _coroutineService;
		private ConcurrentDictionary<string, string> _pendingUploads = new ConcurrentDictionary<string, string>();
		private ConcurrentDictionary<string, string> _previouslyDownloaded = new ConcurrentDictionary<string, string>();
		private IEnumerator _fileWatchingRoutine;
		private IEnumerator _fileWebRequestRoutine;
		private ConnectivityService _connectivityService;
		private List<Func<Promise<Unit>>> _ProcessFilesPromiseList = new List<Func<Promise<Unit>>>();
		private string _manifestPath => Path.Combine(localCloudDataPath.manifestPath, "cloudDataManifest.json");
		private LocalCloudDataPath localCloudDataPath => new LocalCloudDataPath(_platform);
		private Promise<Unit> _initDone;
		public Action<ManifestResponse> UpdateReceived;
		public Action<CloudSavingError> OnError;
		public string LocalCloudDataFullPath => localCloudDataPath.dataPath;

		public bool isInitializing = false;
		public CloudSavingService(IPlatformService platform, PlatformRequester requester,
		   CoroutineService coroutineService, IDependencyProvider provider) : base(provider, ServiceName)
		{
			_platform = platform;
			_requester = requester;
			_coroutineService = coroutineService;
			_connectivityService = new ConnectivityService(_coroutineService);
		}

		public Promise<Unit> Init(int pollingIntervalSecs = 10)
		{
			if (isInitializing) { return _initDone; }

			_initDone = new Promise<Unit>();

			isInitializing = true;

			_delay = new WaitForSecondsRealtime(pollingIntervalSecs);

			var _initSteps = new List<Func<Promise<Unit>>>();

			_initSteps.Add(new Func<Promise<Unit>>(() => StopFileWatcherRoutine()));
			_initSteps.Add(new Func<Promise<Unit>>(() => StopWebRequestRoutine()));
			_initSteps.Add(new Func<Promise<Unit>>(() => ClearQueuesAndLocalManifest()));
			_initSteps.Add(new Func<Promise<Unit>>(() => LoadLocalManifest()));

			return Promise.ExecuteSerially(_initSteps).FlatMap(_ =>
			{
				Directory.CreateDirectory(LocalCloudDataFullPath);

				return EnsureRemoteManifest()
				.FlatMap(SyncRemoteContent)
				.FlatMap(startRoutines =>
				{
					  Subscribe(cb =>
				   {
					  //DO NOT REMOVE THIS, WE NEED THIS TO GET DEFAULT NOTIFICATIONS
				  });

					  _fileWatchingRoutine = StartFileSystemWatchingCoroutine();
					  _platform.Notification.Subscribe(
					  "cloudsaving.datareplaced",
					  OnReplacedNtf
				   );
					  _initDone.CompleteSuccess(PromiseBase.Unit);
					  isInitializing = false;
					  return _initDone;
				  });
			});
		}
		private Promise<Unit> ClearQueuesAndLocalManifest()
		{
			_pendingUploads.Clear();
			_localManifest = null;
			_previouslyDownloaded.Clear();
			return Promise<Unit>.Successful(PromiseBase.Unit);
		}

		private Promise<Unit> SyncRemoteContent(ManifestResponse manifestResponse)
		{
			// Replacement is essentially a "force sync". Therefore we want to ensure that we
			// blow away everything.
			if (manifestResponse.replacement)
			{
				DeleteLocalUserData().Then(_ =>
				   Directory.CreateDirectory(LocalCloudDataFullPath)
				);
			}
			// This method will also commit a new remote manifest AND store the manifest locally.
			return DownloadUserData(manifestResponse);
		}

		private IEnumerator StartFileSystemWatchingCoroutine()
		{
			var routine = WatchDirectoryForChanges();
			_coroutineService.StartCoroutine(routine);
			return routine;
		}

		private IEnumerator WatchDirectoryForChanges()
		{
			while (true)
			{
				yield return _delay;

				if (!_connectivityService.HasConnectivity)
				{
					continue;
				}
				_pendingUploads.Clear();
				if (Directory.Exists(LocalCloudDataFullPath))
				{
					foreach (var filepath in Directory.GetFiles(LocalCloudDataFullPath, "*.*", SearchOption.AllDirectories))
					{
						yield return SetPendingUploads(filepath);
					}
				}

				if (_pendingUploads.Count > 0)
				{
					yield return UploadUserData();
				}
			}
		}

		private void InvokeError(string reason, Exception inner)
		{
			OnError?.Invoke(new CloudSavingError(reason, inner));
		}

		private Action<Exception> ProvideErrorCallback(string methodName)
		{
			return (ex) => { InvokeError($"{methodName} Failed: {ex?.Message ?? "unknown reason"}", ex); };
		}

		public Promise<Unit> ReinitializeUserData(int pollingIntervalSecs = 10)
		{
			if (isInitializing) { return _initDone; }

			var _initSteps = new List<Func<Promise<Unit>>>();
			_initSteps.Add(() => StopFileWatcherRoutine());
			_initSteps.Add(() => StopWebRequestRoutine());
			_initSteps.Add(() => ClearQueuesAndLocalManifest());
			_initSteps.Add(() => DeleteLocalUserData());
			_initSteps.Add(() => Init(pollingIntervalSecs));

			return Promise.ExecuteSerially(_initSteps);

		}

		public Promise<ManifestResponse> EnsureRemoteManifest()
		{
			return FetchUserManifest().RecoverWith(error =>
			{
				if (error is PlatformRequesterException notFound && notFound.Status == 404)
				{
					return CreateEmptyManifest();
				}
				throw error;
			});
		}

		private Promise<Unit> StopFileWatcherRoutine()
		{
			if (_fileWatchingRoutine != null)
			{
				_coroutineService.StopCoroutine(_fileWatchingRoutine);
			}
			return PromiseBase.SuccessfulUnit;
		}
		private Promise<Unit> StopWebRequestRoutine()
		{
			if (_fileWebRequestRoutine != null)
			{
				_coroutineService.StopCoroutine(_fileWebRequestRoutine);
			}
			return PromiseBase.SuccessfulUnit;
		}
		private Promise<Unit> LoadLocalManifest()
		{
			if (File.Exists(_manifestPath))
			{
				_localManifest = _localManifest == null
				   ? JsonUtility.FromJson<ManifestResponse>(File.ReadAllText(_manifestPath))
				   : _localManifest;

				foreach (var entry in _localManifest.manifest)
				{
					var localFilePath = Path.Combine(LocalCloudDataFullPath, entry.key);
					_previouslyDownloaded[localFilePath] = entry.eTag;
				}
			}
			return Promise<Unit>.Successful(PromiseBase.Unit);
		}

		private Promise<ManifestResponse> UploadUserData()
		{
			return GenerateUploadObjectRequestWithMapping().FlatMap(response =>
			{
				if (response.Item1.request.Count <= 0)
				{
					return Promise<ManifestResponse>.Failed(new Exception("Upload is empty"));
				}

				return HandleRequest(response.Item1,
				   response.Item2,
				   Method.PUT,
				   "/data/uploadURL"
				).FlatMap(_ => CommitManifest(response.Item1))
				.RecoverWith(_ => UploadUserData())
				.Error(ProvideErrorCallback(nameof(UploadUserData)));
			});
		}

		private Promise<(UploadManifestRequest, ConcurrentDictionary<string, string>)> GenerateUploadObjectRequestWithMapping()
		{
			Promise<(UploadManifestRequest, ConcurrentDictionary<string, string>)> response = new Promise<(UploadManifestRequest, ConcurrentDictionary<string, string>)>();
			var uploadRequest = new List<ManifestEntry>();
			var uploadMap = new ConcurrentDictionary<string, string>();
			var allFilesAndDirectories = new List<string>();

			if (_pendingUploads != null && _pendingUploads.Count > 0)
			{
				foreach (var item in _pendingUploads)
				{
					allFilesAndDirectories.Add(Path.Combine(LocalCloudDataFullPath, item.Key));
				}
			}

			foreach (var fullPathToFile in allFilesAndDirectories)
			{
				var objectKey = NormalizeS3Path(fullPathToFile, LocalCloudDataFullPath);
				uploadMap.TryAdd(fullPathToFile, objectKey);
				var contentInfo = new FileInfo(fullPathToFile);
				var contentLength = contentInfo.Length;
				var lastModified = long.Parse(contentInfo.LastWriteTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture));
				GenerateChecksum(fullPathToFile).Then(checksum =>
				{
					var uploadObjectRequest = new ManifestEntry(objectKey,
				   (int)contentLength,
				   checksum,
				   null,
				   _platform.User.id,
				   lastModified);
					_previouslyDownloaded[fullPathToFile] = checksum;
					uploadRequest.Add(uploadObjectRequest);
				});
			}
			response.CompleteSuccess((new UploadManifestRequest(uploadRequest), uploadMap));
			return response;
		}
		private Promise<Unit> WriteManifestToDisk(ManifestResponse response)
		{
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(_manifestPath));
				File.WriteAllText(_manifestPath, JsonUtility.ToJson(response, true));
				return LoadLocalManifest();
			}
			catch (Exception ex)
			{
				return Promise<Unit>.Failed(ex).Error(ProvideErrorCallback(nameof(WriteManifestToDisk)));
			}
		}

		private Promise<Unit> DownloadUserData(ManifestResponse manifestResponse)
		{
			var missingLocalFiles = DiffManifest(manifestResponse);
			return GenerateDownloadRequest(missingLocalFiles).FlatMap(downloadRequest =>
			{

				return HandleRequest(new GetS3DownloadURLsRequest(downloadRequest),
			 missingLocalFiles,
			 Method.GET,
			 "/data/downloadURL"
		  )
		  .Error(ProvideErrorCallback(nameof(DownloadUserData)))
		  .FlatMap(__ =>
		  {
				if (manifestResponse.replacement)
				{
					var upload = new UploadManifestRequest(new List<ManifestEntry>());
					foreach (var r in manifestResponse.manifest)
					{
						upload.request.Add(new ManifestEntry(r.key,
						r.size,
						r.eTag,
						null,
						_platform.User.id,
						r.lastModified)
					 );
					}

					return CommitManifest(upload).FlatMap(_ =>
				 {
				   // We want to ensure that we explicitly invoke the event with the ORIGINAL manifest.
				   UpdateReceived?.Invoke(manifestResponse);
					  return PromiseBase.SuccessfulUnit;
				  });
				}
				else
				{
					if (downloadRequest.Count > 0)
					{
						return Promise.ExecuteRolling(10, _ProcessFilesPromiseList).Map(_ => PromiseBase.Unit).FlatMap(downloads =>
					 {
						 _ProcessFilesPromiseList.Clear();
						 return WriteManifestToDisk(manifestResponse).FlatMap(manifest =>
					  {
							UpdateReceived?.Invoke(manifestResponse);
							return PromiseBase.SuccessfulUnit;
						});
					 });
					}
				}
				return PromiseBase.SuccessfulUnit;
			});
			});
		}

		private Promise<List<GetS3ObjectRequest>> GenerateDownloadRequest(ConcurrentDictionary<string, string> missingFiles)
		{
			var downloadRequest = new List<GetS3ObjectRequest>();

			foreach (var s3ObjectKey in missingFiles)
			{
				downloadRequest.Add(new GetS3ObjectRequest(s3ObjectKey.Value));
			}

			return Promise<List<GetS3ObjectRequest>>.Successful(downloadRequest);
		}

		private Promise<ManifestResponse> CreateEmptyManifest()
		{
			var emptyManifest = new UploadManifestRequest(new List<ManifestEntry>());
			return CommitManifest(emptyManifest);
		}

		private ConcurrentDictionary<string, string> DiffManifest(ManifestResponse response)
		{
			var newManifestDict = new ConcurrentDictionary<string, string>();

			if (_localManifest != null && !response.replacement)
			{
				foreach (var s3Object in _localManifest.manifest)
				{
					var fullPathToFile = Path.Combine(LocalCloudDataFullPath, s3Object.key);
					_previouslyDownloaded[fullPathToFile] = s3Object.eTag;
				}

				foreach (var s3Object in response.manifest)
				{
					var fullPathToFile = Path.Combine(LocalCloudDataFullPath, s3Object.key);

					string hash;
					var hasBeenDownloaded = _previouslyDownloaded.TryGetValue(fullPathToFile, out hash);
					var localContentMatchesServer = s3Object.eTag.Equals(hash);

					if (!hasBeenDownloaded || !localContentMatchesServer)
					{
						newManifestDict[fullPathToFile] = s3Object.eTag;
					}
				}
			}
			else
			{
				foreach (var s3Object in response.manifest)
				{
					var fullPathToFile = Path.Combine(LocalCloudDataFullPath, s3Object.key);
					newManifestDict[fullPathToFile] = s3Object.eTag;
				}
			}

			_localManifest = response;
			return newManifestDict;
		}

		private Promise<Unit> HandleRequest<T>(T request, ConcurrentDictionary<string, string> fileNameToKey, Method method, string endpoint)
		{
			var promiseList = new List<Func<Promise<Unit>>>();
			Dictionary<string, PreSignedURL> s3Response = new Dictionary<string, PreSignedURL>();
			return GetPresignedURL(request, endpoint).FlatMap(presignedURLS =>
			{
				if (presignedURLS.response.Count > 0)
				{
					presignedURLS.response.ForEach(resp =>
				 {
				   //MD5_Checksum : PresignedURL
				   s3Response[resp.objectKey] = resp;
				  });
					foreach (var kv in fileNameToKey)
					{
						var fullPathToFile = kv.Key;
						PreSignedURL s3PresignedURL;
						if (s3Response.TryGetValue(kv.Value, out s3PresignedURL))
						{
							promiseList.Add(new Func<Promise<Unit>>(() => GetOrPostObjectInS3(fullPathToFile, s3PresignedURL, method)));
						}
						else
						{
							Debug.LogWarning($"Key in manifest does not match a value on the server, Key {kv.Value}");
						}
					}
				}
				return Promise.ExecuteInBatch(10, promiseList).Map(_ => PromiseBase.Unit);
			}).Error(delete =>
			   {
				   Directory.Delete(localCloudDataPath.temp, true);
			   });
		}

		private Promise<Unit> GetOrPostObjectInS3(string fullPathToDestinationFile, PreSignedURL url, Method method)
		{
			var tempFile = GetTempFileName();
			var s3Request = BuildS3Request(method, url.url, tempFile, fullPathToDestinationFile);
			return MakeRequestToS3(fullPathToDestinationFile,
								   tempFile,
								   s3Request);
		}
		private string GetTempFileName()
		{
			//Create a temp file ID for downloading CloudSaving data before we try and replace the local data
			var guid = Guid.NewGuid().ToString();
			var fullPathToTempFile = Path.Combine(localCloudDataPath.temp, guid);
			return fullPathToTempFile;
		}
		private Promise<Unit> MakeRequestToS3(string filename, string fullPathToTempFile, UnityWebRequest request)
		{
			var result = new Promise<Unit>();
			_fileWebRequestRoutine = HandleResponse(filename, fullPathToTempFile, result, request);
			_coroutineService.StartCoroutine(_fileWebRequestRoutine);
			return result;
		}

		private IEnumerator HandleResponse(string filename, string fullPathToTempFile, Promise<Unit> promise, UnityWebRequest request)
		{
			UnityWebRequest currentRequest = request;
			var isGet = currentRequest.method == Method.GET.ToString();
			var isError = false;

			for (var retryCount = 0; retryCount < 4; retryCount++)
			{
				var activeWebRequest = currentRequest.SendWebRequest();
				isError = false;

				while (!activeWebRequest.isDone)
				{
					yield return new WaitForSecondsRealtime(1f);
				}

				if (activeWebRequest.webRequest.IsHttpError())
				{
					isError = true;
					yield return new WaitForSecondsRealtime(retryCount);
					//The presignedURL did not find an object that matches
					Debug.LogWarning($"Retrying {currentRequest.method} for {filename}. Encountered: {currentRequest.error}");
					//The previous request was made, Unity destroys it, so we need to create a new one with the same parameters.
					currentRequest = new UnityWebRequest(currentRequest.url, currentRequest.method);
				}
				else
				{
					//Not an error, lets get out of the loop
					break;
				}
			}
			if (isError)
			{
				_ProcessFilesPromiseList.Clear();
				promise.Error(ProvideErrorCallback(nameof(HandleResponse)));
				promise.CompleteError(new Exception($"Failed to process {filename}, stopping service."));
				yield return promise;
				yield break;
			}

			if (isGet)
			{
				var isDownloadValid = ValidateTempFile(fullPathToTempFile, filename);
				if (!isDownloadValid)
				{
					_ProcessFilesPromiseList.Clear();
					promise.Error(ProvideErrorCallback(nameof(HandleResponse)));
					promise.CompleteError(new Exception($"Failed to process {filename}, stopping service"));
					yield return promise;
					yield break;
				}
				else
				{
					_ProcessFilesPromiseList.Add(() => ProcessTempFiles(fullPathToTempFile, filename, isDownloadValid));
					promise.CompleteSuccess(PromiseBase.Unit);
					yield return promise;
				}
			}

			else
			{
				promise.CompleteSuccess(PromiseBase.Unit);
				yield return promise;
			}

			request.Dispose();
		}



		private bool ValidateTempFile(string fullPathToTempFile, string fullPathToDestinationFile)
		{
			if (!File.Exists(fullPathToTempFile)) { return false; }

			bool isValid = false;

			GenerateChecksum(fullPathToTempFile).Then(checksum =>
			{
				var objectKey = fullPathToDestinationFile.Substring(0, LocalCloudDataFullPath.Length + 1);
				var manifestRecord = _localManifest.manifest.Find(x => x.key.Equals(objectKey));

				if (manifestRecord == null)
				{
					isValid = true;
				}
				else
				{
					isValid = checksum == manifestRecord.eTag;
				}
			}

			);

			return isValid;
		}



		private Promise<Unit> ProcessTempFiles(string fullPathToTempFile, string fullPathToDestinationFile, bool isValid)
		{
			Promise<Unit> promise = new Promise<Unit>();
			if (isValid)
			{
				if (File.Exists(fullPathToDestinationFile))
				{
					File.Delete(fullPathToDestinationFile);
				}
				if (File.Exists(fullPathToTempFile))
				{
					File.Move(fullPathToTempFile, fullPathToDestinationFile);
					promise.CompleteSuccess(PromiseBase.Unit);
				}
				else
				{
					promise.CompleteError(new Exception("Temp file no longer exists."));
				}

			}
			else
			{
				File.Delete(fullPathToTempFile);
				promise.CompleteError(new Exception("File integrity check failed."));
			}

			return promise;
		}

		private UnityWebRequest BuildS3Request(Method method, string uri, string fullPathToTempFile, string fileName)
		{
			UnityWebRequest request;

			var downloadLocationIsNotNull = fullPathToTempFile != null;
			var isGet = method == Method.GET;

			if (downloadLocationIsNotNull && isGet)
			{
				request = new UnityWebRequest(uri)
				{
					downloadHandler = new DownloadHandlerFile(fullPathToTempFile),
					method = method.ToString()
				};
			}
			else
			{
				var upload = new UploadHandlerRaw(ReadFileWithoutLock(fileName))
				{
					contentType = "application/octet-stream"
				};
				request = new UnityWebRequest(uri)
				{
					uploadHandler = upload,
					method = method.ToString()
				};
			}

			return request;
		}

		private byte[] ReadFileWithoutLock(string filename)
		{
			byte[] oFileBytes = null;
			using (FileStream fs = File.Open(
								 filename,
								 FileMode.Open,
								 FileAccess.Read,
								 FileShare.ReadWrite))
			{
				int numBytesToRead = Convert.ToInt32(fs.Length);
				oFileBytes = new byte[(numBytesToRead)];
				fs.Read(oFileBytes, 0, numBytesToRead);
			}

			return oFileBytes;

		}

		private Promise<ManifestResponse> FetchUserManifest()
		{
			return _requester.Request<ManifestResponse>(
				  Method.GET,
				  string.Format($"/basic/cloudsaving?playerId={_platform.User.id}"),
				  null)
			   .Error(ProvideErrorCallback(nameof(FetchUserManifest)));
		}

		private Promise<URLResponse> GetPresignedURL<T>(T request, string endpoint)
		{
			return _requester.Request<URLResponse>(
				  Method.POST,
				  string.Format($"/basic/cloudsaving{endpoint}"),
				  request)
			   .Error(ProvideErrorCallback(nameof(GetPresignedURL)));
		}

		private Promise<ManifestResponse> CommitManifest(UploadManifestRequest request)
		{
			return _requester.Request<ManifestResponse>(
			   Method.PUT,
			   string.Format($"/basic/cloudsaving/data/commitManifest"),
			   request
			).FlatMap(res =>
			   {
				   res.replacement = false;
				   _localManifest = res;
				   return WriteManifestToDisk(res).Map(_ =>
				{
					  return res;
				  });
			   }
			).Error(ProvideErrorCallback(nameof(CommitManifest)));
		}

		private string NormalizeS3Path(string key, string path)
		{
			return key.Remove(0, path.Length + 1).Replace(@"\", "/");
		}
		private Promise<Unit> SetPendingUploads(string filePath)
		{
			return GenerateChecksum(filePath).FlatMap(checksum =>
			{

				var checksumEqual = _previouslyDownloaded.ContainsKey(filePath) && _previouslyDownloaded[filePath].Equals(checksum);
				var missingKey = !_previouslyDownloaded.ContainsKey(filePath);
				var fileLengthNotZero = new FileInfo(filePath).Length > 0;
				if ((!checksumEqual || missingKey) && fileLengthNotZero)
				{
					_pendingUploads[filePath] = checksum;
				}
				return Promise<Unit>.Successful(PromiseBase.Unit);
			});
		}
		private Promise<Unit> DeleteLocalUserData()
		{
			var _initSteps = new List<Func<Promise<Unit>>>();
			_initSteps.Add(new Func<Promise<Unit>>(() => StopFileWatcherRoutine()));
			_initSteps.Add(new Func<Promise<Unit>>(() => ClearQueuesAndLocalManifest()));

			return Promise.ExecuteSerially(_initSteps).FlatMap(initDone =>
			{


				if (File.Exists(_manifestPath))
				{
					File.Delete(_manifestPath);
				}

				if (Directory.Exists(localCloudDataPath.manifestPath))
				{
					try
					{
						Directory.Delete(localCloudDataPath.manifestPath, true);
					}
					catch (IOException deleteFailed)
					{
						Debug.Log($"Deletion failed because the files were open, trying again: {deleteFailed}");
						throw deleteFailed;
					}

				}
				return Promise<Unit>.Successful(initDone);
			}).Error(ProvideErrorCallback(nameof(GetPresignedURL)));
		}

		protected override string CreateRefreshUrl(string scope = null)
		{
			return "/basic/cloudsaving";
		}

		private static Promise<string> GenerateChecksum(string content)
		{
			using (var md5 = MD5.Create())
			{
				using (var stream = new FileStream(content, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					return Promise<string>.Successful(BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty));
				}
			}
		}

		private void OnReplacedNtf(object _)
		{
			EnsureRemoteManifest()
				.FlatMap(SyncRemoteContent);
		}

		protected override Promise<ManifestResponse> ExecuteRequest(IBeamableRequester requester, string url)
		{
			return base.ExecuteRequest(requester, url).RecoverWith(err =>
			{
				if (err is PlatformRequesterException notFound && notFound.Status == 404)
				{
					return CreateEmptyManifest();
				}

				return Promise<ManifestResponse>.Failed(err);
			});
		}

		protected override void OnRefresh(ManifestResponse data)
		{
			if (JsonUtility.ToJson(_localManifest) != JsonUtility.ToJson(data))
			{
				SyncRemoteContent(data);
			}
		}
	}


	[Serializable]
	public class S3Object
	{
		public string bucketName;
		public string key;
		public long size;
		public DateTime lastModified;
		public Owner owner;
		public string storageClass;
		public string eTag;
	}

	[Serializable]
	public class Owner
	{
		public string displayName;
		public string id;
	}

	[Serializable]
	public class PreSignedURL
	{
		public string objectKey;
		public string url;
	}

	[Serializable]
	public class URLResponse
	{
		public List<PreSignedURL> response;

		public URLResponse(List<PreSignedURL> response)
		{
			this.response = response;
		}
	}

	[Serializable]
	public class UploadManifestRequest
	{
		public List<ManifestEntry> request;

		public UploadManifestRequest(List<ManifestEntry> request)
		{
			this.request = request;
		}
	}

	[Serializable]
	public class ManifestEntry
	{
		public string objectKey;
		public int sizeInBytes;
		public string checksum;
		public List<MetadataPair> metadata;
		public long playerId;
		public long lastModified;

		public ManifestEntry(string objectKey, int sizeInBytes, string checksum, List<MetadataPair> metadata,
		   long playerId, long lastModified)
		{
			this.objectKey = objectKey;
			this.sizeInBytes = sizeInBytes;
			this.checksum = checksum;
			this.metadata = metadata;
			this.playerId = playerId;
			this.lastModified = lastModified;
		}
	}

	[Serializable]
	public class MetadataPair
	{
		public string key;
		public string value;

		public MetadataPair(string key, string value)
		{
			this.key = key;
			this.value = value;
		}
	}

	[Serializable]
	public class GetS3ObjectRequest
	{
		public string objectKey;

		public GetS3ObjectRequest(string objectKey)
		{
			this.objectKey = objectKey;
		}
	}

	[Serializable]
	public class GetS3DownloadURLsRequest
	{
		public List<GetS3ObjectRequest> request;

		public GetS3DownloadURLsRequest(List<GetS3ObjectRequest> request)
		{
			this.request = request;
		}
	}

	[Serializable]
	public class ManifestResponse
	{
		public string id;
		public List<CloudSavingManifestEntry> manifest;
		public bool replacement = false;
	}

	[Serializable]
	public class CloudSavingManifestEntry
	{
		public string bucketName;
		public string key;
		public int size;
		public long lastModified;
		public string eTag;

		public CloudSavingManifestEntry(string bucketName, string key, int size, long lastModified, string eTag)
		{
			this.bucketName = bucketName;
			this.key = key;
			this.size = size;
			this.lastModified = lastModified;
			this.eTag = eTag;
		}
	}

	[Serializable]
	public class LocalCloudDataPath
	{
		public string root;
		public string prefix;
		public string dataPath;
		public string manifestPath;
		public string temp;

		private const string _root = "beamable";
		private const string _cloudSavingDir = "cloudsaving";
		private const string _temp = "tmp";

		public LocalCloudDataPath(IPlatformService platformService)
		{
			platformService.OnReady.Then(plat =>
			{
				root = Path.GetFullPath(Application.persistentDataPath);
				prefix = Path.Combine(
				_root,
				_cloudSavingDir,
				platformService.Cid,
				platformService.Pid,
				platformService.User.id.ToString()
			 );
				dataPath = Path.Combine(root, prefix, "data");
				manifestPath = Path.Combine(root, prefix);
				temp = Path.Combine(manifestPath, _temp);
			});
		}
	}
}
