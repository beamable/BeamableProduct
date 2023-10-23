using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Notifications;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Common.Player;
using Beamable.Common.Runtime.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Beamable.Api.CloudSaving
{
	[Serializable]
	public class PlayerSaves : AbsObservableReadonlyDictionary<string, SerializableDictionaryStringToString>, IStorageHandler<PlayerSaves>
	{
		/// <summary>
		/// The <see cref="CloudSavingService"/> will keep the files located at this file path backed up on Beamable servers.
		/// You should use this path as the base path for all file read and write operations.
		/// </summary>
		public string LocalCloudDataFullPath => localCloudDataPath.dataPath;

		/// <summary>
		/// An event with a <see cref="CloudSavingError"/> parameter.
		/// This event triggers anytime there is an error in the <see cref="CloudSavingService"/>
		/// </summary>
		public Action<CloudSavingError> OnError;

		private readonly IPlatformService _platform;
		private readonly INotificationService _notificationService;
		private readonly PlatformRequester _requester;
		private string _manifestPath => Path.Combine(localCloudDataPath.manifestPath, "cloudDataManifest.json");
		private LocalCloudDataPath localCloudDataPath => new LocalCloudDataPath(_platform);
		private StorageHandle<PlayerSaves> _saveHandle;
		private ManifestResponse _localManifest;
		private ConcurrentDictionary<string, string> _pendingUploads = new ConcurrentDictionary<string, string>();

		public PlayerSaves(IPlatformService platform,
		                   INotificationService notificationService,
		                   PlatformRequester requester,
		                   IDependencyProvider provider)
		{
			_platform = platform;
			_notificationService = notificationService;
			_requester = requester;

			_notificationService.Subscribe(
				"cloudsaving.datareplaced",
				OnReplacedNtf
			);
			_notificationService.Subscribe(
				"cloudsaving.refresh",
				OnReplacedNtf
			);
		}

		/// <summary>
		/// <b> You shouldn't need to call this method, because this will be called by the constructor. </b>
		/// This method will be marked as Obsolete in the future.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
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

		private Promise<ManifestResponse> CreateEmptyManifest()
		{
			var emptyManifest = new UploadManifestRequest(new List<ManifestEntry>());
			return CommitManifest(emptyManifest);
		}

		private void OnReplacedNtf(object _)
		{
			EnsureRemoteManifest()
				.FlatMap(SyncRemoteContent);
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

		private static Promise<List<GetS3ObjectRequest>> GenerateDownloadRequest(ConcurrentDictionary<string, string> missingFiles)
		{
			var downloadRequest = new List<GetS3ObjectRequest>();

			foreach (var s3ObjectKey in missingFiles)
			{
				downloadRequest.Add(new GetS3ObjectRequest(s3ObjectKey.Value));
			}

			return Promise<List<GetS3ObjectRequest>>.Successful(downloadRequest);
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
							       // UpdateReceived?.Invoke(manifestResponse);
							       return PromiseBase.SuccessfulUnit;
						       });
					       }
					       else
					       {
						       if (downloadRequest.Count > 0)
						       {
								   
									       return PromiseBase.SuccessfulUnit;
							       /*(return Promise.ExecuteInBatchSequence(10, _ProcessFilesPromiseList).Map(_ => PromiseBase.Unit).FlatMap(downloads =>
							       {
								       //_ProcessFilesPromiseList.Clear();
								       return WriteManifestToDisk(manifestResponse).FlatMap(manifest =>
								       {
									       // UpdateReceived?.Invoke(manifestResponse);
									       return PromiseBase.SuccessfulUnit;
								       });
							       });*/
						       }
					       }
					       return PromiseBase.SuccessfulUnit;
				       });
			});
		}
		

		private ConcurrentDictionary<string, string> DiffManifest(ManifestResponse response)
		{
			var newManifestDict = new ConcurrentDictionary<string, string>();

			if (_localManifest == null || response.replacement)
			{
				foreach (var s3Object in response.manifest)
				{
					var fullPathToFile = Path.Combine(LocalCloudDataFullPath, s3Object.key);
					newManifestDict[fullPathToFile] = s3Object.eTag;
				}
			}
			else
			{
				foreach (var s3Object in _localManifest.manifest)
				{
					var fullPathToFile = Path.Combine(LocalCloudDataFullPath, s3Object.key);
					//_previouslyProcessedFiles[fullPathToFile] = s3Object.eTag;
				}

				foreach (var s3Object in response.manifest)
				{
					var fullPathToFile = Path.Combine(LocalCloudDataFullPath, s3Object.key);

					//var hasBeenDownloaded = _previouslyProcessedFiles.TryGetValue(fullPathToFile, out string hash);
					//var localContentMatchesServer = !string.IsNullOrWhiteSpace(hash) && s3Object.eTag.Equals(hash);

					//if (!hasBeenDownloaded || !localContentMatchesServer)
					{
					//	newManifestDict[fullPathToFile] = s3Object.eTag;
					}
				}
			}

			_localManifest = response;
			return newManifestDict;
		}

		private Promise<Unit> HandleRequest<T>(T request,
											   ConcurrentDictionary<string, string> fileNameToKey,
											   Method method,
											   string endpoint)
		{
			var promiseList = new ConcurrentDictionary<string, Func<Promise<Unit>>>();
			return GetPresignedURL(request, endpoint).FlatMap(presignedURLS =>
			{
				int responseAmount = presignedURLS.response.Count;
				if (responseAmount == 0)
				{
					return PromiseBase.SuccessfulUnit;
				}
				var s3Response = new Dictionary<string, PreSignedURL>(responseAmount);
				for (int i = 0; i < responseAmount; i++)
				{
					//MD5_Checksum : PresignedURL
					s3Response[presignedURLS.response[i].objectKey] = presignedURLS.response[i];
				}

				foreach (var kv in fileNameToKey)
				{
					PreSignedURL s3PresignedURL;
					bool isSuccessful = s3Response.TryGetValue(kv.Value, out s3PresignedURL);

					if (isSuccessful)
					{
						var fullPathToFile = kv.Key;
						isSuccessful = promiseList.TryAdd(s3PresignedURL.url,
														  new Func<Promise<Unit>>(
															  () => GetOrPostObjectInS3(
																  fullPathToFile, s3PresignedURL, method)));
					}

					if (!isSuccessful)
					{
						Debug.LogWarning($"Key in manifest does not match a value on the server, Key {kv.Value}");
					}
				}

				if (promiseList.Count != responseAmount)
				{
					return Promise<Unit>.Failed(new Exception("Some of the keys in manifest does not match a values on the server"));
				}

				return Promise.ExecuteInBatch(10, promiseList.Values.ToList()).Map(_ => PromiseBase.Unit);
			}).Error(delete =>
			{
				Directory.Delete(localCloudDataPath.temp, true);
			});
		}

		private Promise<Unit> GetOrPostObjectInS3(string fullPathToDestinationFile, PreSignedURL url, Method method)
		{
			var tempFile = GetTempFileName();
			//var request = _requester.Request(method,url.url,)
			var s3Request = BuildS3Request(method, url.url, tempFile, fullPathToDestinationFile);
			return MakeRequestToS3(fullPathToDestinationFile,
			                       tempFile,
			                       s3Request);
		}

		private Promise<Unit> MakeRequestToS3(string filename, string fullPathToTempFile, UnityWebRequest request)
		{
			var result = new Promise<Unit>();
			//_fileWebRequestRoutine = HandleResponse(filename, fullPathToTempFile, result, request);
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
				//_ProcessFilesPromiseList.Clear();
				promise.Error(ProvideErrorCallback(nameof(HandleResponse)));
				promise.CompleteError(new Exception($"Failed to process {filename}, stopping service."));
				yield return promise;
				yield break;
			}

			if (isGet)
			{
				//var isDownloadValid = ValidateTempFile(fullPathToTempFile, filename);
				//if (!isDownloadValid)
				{
					//_ProcessFilesPromiseList.Clear();
					promise.Error(ProvideErrorCallback(nameof(HandleResponse)));
					promise.CompleteError(new Exception($"Failed to process {filename}, stopping service"));
					//yield return promise;
					//yield break;
				}

				//_ProcessFilesPromiseList.Add(() => ProcessTempFiles(fullPathToTempFile, filename, isDownloadValid));
				promise.CompleteSuccess(PromiseBase.Unit);
				yield return promise;
			}

			else
			{
				promise.CompleteSuccess(PromiseBase.Unit);
				yield return promise;
			}

			request.Dispose();
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
				int _ = fs.Read(oFileBytes, 0, numBytesToRead);
			}

			return oFileBytes;
		}

		private string GetTempFileName()
		{
			//Create a temp file ID for downloading CloudSaving data before we try and replace the local data
			var guid = Guid.NewGuid().ToString();
			var fullPathToTempFile = Path.Combine(localCloudDataPath.temp, guid);
			return fullPathToTempFile;
		}

		private Promise<Unit> ClearQueuesAndLocalManifest()
		{
			_pendingUploads.Clear();
			_localManifest = null;
			//_previouslyProcessedFiles.Clear();
			return Promise<Unit>.Successful(PromiseBase.Unit);
		}

		private Promise<Unit> DeleteLocalUserData()
		{
			var initSteps = new List<Func<Promise<Unit>>>();
			initSteps.Add(new Func<Promise<Unit>>(() => ClearQueuesAndLocalManifest()));

			return Promise.ExecuteSerially(initSteps).FlatMap(initDone =>
			{
				if (File.Exists(_manifestPath))
				{
					File.Delete(_manifestPath);
				}

				if (!Directory.Exists(localCloudDataPath.manifestPath))
				{
					return Promise<Unit>.Successful(initDone);
				}

				try
				{
					Directory.Delete(localCloudDataPath.manifestPath, true);
				}
				catch (IOException deleteFailed)
				{
					Debug.LogError($"Deletion failed because the files were open, trying again: {deleteFailed}");
					throw;
				}

				return Promise<Unit>.Successful(initDone);
			}).Error(ProvideErrorCallback(nameof(GetPresignedURL)));
		}

		public void OnBeforeSaveState()
		{
			throw new NotImplementedException();
		}

		public void OnAfterLoadState()
		{
			throw new NotImplementedException();
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
					//_previouslyProcessedFiles[localFilePath] = entry.eTag;
				}
			}

			return Promise<Unit>.Successful(PromiseBase.Unit);
		}

		private string NormalizeS3Path(string key, string path)
		{
			return key.Remove(0, path.Length + 1).Replace(@"\", "/");
		}

		private void InvokeError(string reason, Exception inner)
		{
			OnError?.Invoke(new CloudSavingError(reason, inner));
		}

		private Action<Exception> ProvideErrorCallback(string methodName)
		{
			return (ex) => { InvokeError($"{methodName} Failed: {ex?.Message ?? "unknown reason"}", ex); };
		}

		public void ReceiveStorageHandle(StorageHandle<PlayerSaves> handle)
		{
			_saveHandle = handle;
		}

		protected override async Promise PerformRefresh()
		{
			await LoadLocalManifest();
			var response = await EnsureRemoteManifest();
			await SyncRemoteContent(response);
			_notificationService.Subscribe(
				"cloudsaving.datareplaced",
				OnReplacedNtf
			);
		}
	}
}
