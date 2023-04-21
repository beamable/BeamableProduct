using Beamable.Common;
using Beamable.Common.Spew;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Beamable.Serialization.SmallerJSON;
using System.Net;
using System.Threading;
using UnityEngine;

namespace Beamable.Server
{
	public class ECRUploaderService
	{

		/// <summary>
		/// Tag to apply when uploading images.
		/// </summary>
		private const string DockerTagReference = "latest";

		private const string s3ECRStorage = "beamable-ecr-images";
		private readonly HttpClient _client;
		private readonly string _uploadBaseUri;
		private readonly MD5 _md5 = MD5.Create();
		private string serviceUniqueName;
		private readonly string _serviceName;
		private readonly string _imageId;

		public event Action<string, float> OnProgress; // serviceName, progress[0-1]

		public ECRUploaderService(string cid, 
									string pid,
									string productionPid,
									string token,
									string serviceName,
		                            string imageId,
									string registryUrl="https://dev-ecr.beamable.com")
		{
			_client = new HttpClient();
			_client.DefaultRequestHeaders.Add("x-ks-clientid", cid);
			_client.DefaultRequestHeaders.Add("x-ks-projectid", pid);
			_client.DefaultRequestHeaders.Add("x-ks-token", token);
			serviceUniqueName = GetHash($"{cid}_{productionPid}_{serviceName}").Substring(0, 30);
			_uploadBaseUri = $"{registryUrl}/{serviceUniqueName}";
			_serviceName = serviceName;
			_imageId = imageId;
		}


		protected string GetHash(string input)
		{
			byte[] data = _md5.ComputeHash(Encoding.UTF8.GetBytes(input));
			var sBuilder = new StringBuilder();
			for (int i = 0; i < data.Length; i++)
			{
				sBuilder.Append(data[i].ToString("x2"));
			}

			return sBuilder.ToString();
		}

		private static byte[] ReadFileWithoutLock(string filename)
		{
			byte[] fileBytes = null;
			using (FileStream fs = File.Open(
				       filename,
				       FileMode.Open,
				       FileAccess.Read,
				       FileShare.ReadWrite))
			{
				int numBytesToRead = Convert.ToInt32(fs.Length);
				fileBytes = new byte[(numBytesToRead)];
				fs.Read(fileBytes, 0, numBytesToRead);
			}

			return fileBytes;

		}

		private async Task<HttpResponseMessage> SendFileToS3(Uri uri, ByteArrayContent content, CancellationToken cancellationToken)
		{
			var request = new HttpClient();
			var requestMessage = new HttpRequestMessage(HttpMethod.Put, uri);
			requestMessage.Content = content;
			
			var registryResponse = await SendRequestWithRetries($"Upload {uri} to s3", () => request.SendAsync(requestMessage, cancellationToken), cancellationToken);
			try
			{
				registryResponse.EnsureSuccessStatusCode();
			}
			catch (HttpRequestException httpError)
			{
				throw new HttpRequestException($"Uploading image to registry failed with: {httpError}");
			}

			return registryResponse;
		}

		private ByteArrayContent GenerateContentForUpload(string fileName)
		{
			var file = ReadFileWithoutLock(fileName);
			return new ByteArrayContent(file);
		}

		private async Task<Uri> GetPresignedURLForObject(string key, CancellationToken cancellationToken)
		{
			var uri = $"{_uploadBaseUri}/getURL/{s3ECRStorage}/{serviceUniqueName}/{key}";
			var response = await SendRequestWithRetries($"Get Presigned Url for {key}", () => _client.GetAsync(uri, cancellationToken), cancellationToken);
			// var response = await _client.GetAsync(uri);
			try
			{
				response.EnsureSuccessStatusCode();
			}
			catch (Exception ex)
			{
				Debug.LogError($"Failed to get Presigned url for uri=[{uri}]");
				Debug.LogException(ex);
				throw;
			}

			var url = response.Headers.Location;
			return url;
		}

		private async Task<HttpResponseMessage> CommitImageToRegistry(byte[] localManifestBytes, CancellationToken cancellationToken)
		{
			var manifestJson = Encoding.UTF8.GetString(localManifestBytes);

			var commitToRegistryResponse = await SendRequestWithRetries($"Submitting Registry", () =>
				                                                            _client.PostAsync(
					                                                            requestUri:
					                                                            $"{_uploadBaseUri}/submitLayersToECR?imageTag={_imageId}",
					                                                            content: new StringContent(
						                                                            manifestJson), cancellationToken),
			                                                            cancellationToken);
			
			try
			{
				commitToRegistryResponse.EnsureSuccessStatusCode();
			}
			catch (HttpRequestException httpError)
			{
				throw new HttpRequestException($"Commiting image to registry failed with: {httpError}");
			}

			return commitToRegistryResponse;
		}

		private void EmitProgress(float amount)
		{
			OnProgress?.Invoke(_serviceName, amount);
		}

		public async Task Upload(string folder, CancellationToken token)
		{
			token.ThrowIfCancellationRequested();

			//Local docker manifest, used to identify the config file.
			var localManifestBytes = File.ReadAllBytes($"{folder}/manifest.json");
			var manifest = DockerManifest.FromBytes(localManifestBytes);
			EmitProgress(.05f);
			//Get Presigned URL and ByteArrayContent to push to S3

			// Manifest is JSON that contains the relative <layer_hash>/layer.tar and Config file name
			//This manifest differs from the Image manifest, which will be generated by our service
			var manifestFilePresignedURL = await GetPresignedURLForObject($"manifest.json", token);
			token.ThrowIfCancellationRequested();
			EmitProgress(.1f);

			var manifestFilePayload = GenerateContentForUpload($"{folder}/manifest.json");


			// Config is JSON that is similar to docker inspect on an image
			var configFilePresignedURL = await GetPresignedURLForObject($"{manifest.config}", token);
			token.ThrowIfCancellationRequested();
			EmitProgress(.15f);

			var configFilePayload = GenerateContentForUpload($"{folder}/{manifest.config}");

			//Push the config to S3
			await SendFileToS3(configFilePresignedURL, configFilePayload, token);
			token.ThrowIfCancellationRequested();
			EmitProgress(.2f);


			//Push the manifest to S3
			await SendFileToS3(manifestFilePresignedURL, manifestFilePayload, token);
			token.ThrowIfCancellationRequested();
			EmitProgress(.25f);

			// Sorted dict of upload tasks
			var uploadIndexToJob = new SortedDictionary<int, Task<HttpResponseMessage>>();
			for (var i = 0; i < manifest.layers.Length; i++)
			{
				var layer = manifest.layers[i];
				var url = await GetPresignedURLForObject(layer, token);
				var work = SendFileToS3(url, GenerateContentForUpload($"{folder}/{layer}"), token);
				uploadIndexToJob.Add(i, work);
				token.ThrowIfCancellationRequested();
			}

			foreach (var job in uploadIndexToJob)
			{
				var _ = job.Value.ContinueWith((_, state) =>
				{
					var doneTasks = uploadIndexToJob.Count(j => j.Value.IsCompleted);
					var progress = doneTasks / (float)(manifest.layers.Length);
					EmitProgress(.25f + (progress * .7f));
				}, job.Key, token);
			}

			//Process all upload tasks, in order
			await Task.WhenAll(uploadIndexToJob.Values);
			token.ThrowIfCancellationRequested();
			EmitProgress(.97f);

			//Inform service that we're done uploading and to move the data to ECR
			await CommitImageToRegistry(localManifestBytes, token);
			token.ThrowIfCancellationRequested();
			EmitProgress(1);

		}

		private async Task<HttpResponseMessage> SendRequestWithRetries(string name,
		                                                               Func<Task<HttpResponseMessage>> requestGenerator,
		                                                               CancellationToken cancellationToken,
		                                                               int maxAttempts = 500)
		{
			var attemptCount = 0;
			var timeoutStatusCodes = new HttpStatusCode[]
			{
				HttpStatusCode.GatewayTimeout,
			};

			async Task<HttpResponseMessage> Attempt()
			{
				if (attemptCount++ >= maxAttempts)
				{
					throw new HttpRequestException("Request timed out, and exhausted all retries.");
				}

				cancellationToken.ThrowIfCancellationRequested();
				try
				{
					var result = await requestGenerator();
					if (timeoutStatusCodes.Contains(result.StatusCode))
					{
						// failed, try again :( 
						Debug.LogWarning(
							$"Request failed with bad status code, trying again... name=[{name}] attempt=[{attemptCount}] status=[{result.StatusCode}]");
						return await Attempt();
					}

					return result;
				}

				catch (IOException io)
				{
					Debug.LogWarning(
						$"Request failed out due to io, trying again... name=[{name}] attempt=[{attemptCount}] message=[{io.Message}]");
					return await Attempt();
				}
				catch (HttpRequestException ex) when (ex.InnerException is IOException inner)
				{
					Debug.LogWarning(
						$"Request failed out due to inner io, trying again... name=[{name}] attempt=[{attemptCount}] message=[{ex.Message}] inner=[{inner.Message}]");
					return await Attempt();
				}
				catch (TaskCanceledException)
				{
					Debug.LogWarning($"Request timed out, trying again... name=[{name}] attempt=[{attemptCount}]");
					return await Attempt();
				}
				catch (Exception ex)
				{
					Debug.Log("Unknown upload exception!!");
					Debug.LogException(ex);
					throw;
				}
			}

			return await Attempt();
		}
		

		/// <summary>
		/// Docker manifest data structure, from JSON like:
		///   [{"Config":"...","RepoTags":["..."],"Layers":["...","..."]}]
		/// But the uploader does not need RepoTags so we omit it.
		/// </summary>
		[Serializable]
		private class DockerManifest
		{
			public string config;
			public string[] layers;

			/// <summary>
			/// Given JSON bytes that fit the expected Docker manifest
			/// schema, create a manifest data structure for the first
			/// manifest in the JSON.
			/// </summary>
			/// <param name="bytes">JSON data bytes.</param>
			/// <returns>Manifest data structure.</returns>
			public static DockerManifest FromBytes(byte[] bytes)
			{
				var result = new DockerManifest();
				var manifests = (List<object>)Json.Deserialize(bytes);
				var firstManifest = (IDictionary<string, object>)manifests[0];
				result.config = firstManifest["Config"].ToString();
				var layers = (List<object>)firstManifest["Layers"];
				result.layers = layers?.Select(x => x.ToString()).ToArray();
				return result;
			}
		}
	}
}
