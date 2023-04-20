/**
 * This part of the class defines how we manage Beamo Services and how we deploy them remotely to Beam-O.
 * It handles synchronizing the remote and local manifests, generating a remote manifest from the locally defined data, uploading the docker images to a registry and finally pushing the manifest
 * up to Beam-O.
 */

using Beamable.Common;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server;
using ICSharpCode.SharpZipLib.Tar;
using Serilog;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace cli.Services;

public partial class BeamoLocalSystem
{
	/// <summary>
	/// Synchronizes the given <see cref="localManifest"/> instance with the given <see cref="remoteManifest"/>. This will:
	/// <list type="bullet">
	/// <item>Ensures all remote embedded databases (storage objects) are defined locally. Since these can always be built </item>
	/// <item></item>
	/// </list>
	/// </summary>
	/// <param name="remoteManifest"></param>
	/// <param name="localManifest"></param>
	public async Task SyncLocalManifestWithRemote(ServiceManifest remoteManifest)
	{
		var existingMongoServices = BeamoManifest.ServiceDefinitions.Where(sd => sd.Protocol == BeamoProtocolType.EmbeddedMongoDb).ToList();
		foreach (var storageReference
				 in remoteManifest.storageReference)
		{
			// If we don't have a service storage with that id stored locally, let's make one
			if (existingMongoServices.All(sd => sd.BeamoId != storageReference.id))
				await AddDefinition_EmbeddedMongoDb(storageReference.id, null, Array.Empty<string>(), CancellationToken.None);
		}

		var existingHttpServices = BeamoManifest.ServiceDefinitions.Where(sd => sd.Protocol == BeamoProtocolType.HttpMicroservice).ToList();
		foreach (var httpReference in remoteManifest.manifest)
		{
			var foundDefinition = existingHttpServices.FirstOrDefault(sd => sd.BeamoId == httpReference.serviceName);
			// If we don't have an http service with that id locally defined, let's make a remote only service defined locally.
			// A remote-only service is just a service that can't be built locally --- in HttpMicroservice's case, its a service with no project and dockerfile paths.
			if (foundDefinition == null)
			{
				var sd = await AddDefinition_HttpMicroservice(httpReference.serviceName,
					null,
					null,
					httpReference.dependencies.Select(d => d.id).ToArray(),
					CancellationToken.None);

				sd.ImageId = httpReference.imageId;
				sd.ShouldBeEnabledOnRemote = httpReference.enabled;
			}
			else
			{
				// If we can't build the image locally, we keep the reference to the image id that existed in the remote.
				if (!VerifyCanBeBuiltLocally(foundDefinition))
					foundDefinition.ImageId = httpReference.imageId;
				// If we can build the image locally, this image id will be replaced when we deploy either locally or remotely, so we don't need to do anything here.
			}
		}
	}

	/// <summary>
	/// Deploys services defined in the given <see cref="localManifest"/>.
	/// </summary>
	public async Task<ServiceManifest> DeployToRemote(BeamoLocalManifest localManifest, BeamoLocalRuntime localRuntime, string dockerRegistryUrl, string comments,
		Dictionary<string, string> perServiceComments, Action<string, float> buildPullImageProgress = null, Action<string> onServiceDeployCompleted = null,
		Action<string, float> onContainerUploadProgress = null, Action<string, bool> onContainerUploadCompleted = null)
	{
		// First Stop all Local Containers that are running
		await Task.WhenAll(localRuntime.ExistingLocalServiceInstances.Select(async sd => await StopContainer(sd.ContainerId)));

		// Then, let's try to deploy locally first.
		try
		{
			await DeployToLocal(localManifest, null, buildPullImageProgress, onServiceDeployCompleted);
		}
		// If we fail, log out a message and the exception that caused the failure
		catch (Exception e)
		{
			BeamableLogger.LogError($"Prevented remote deployment since local deployment failed with exception:\n{e}!");
			return null;
		}


		// Upload working containers to docker registry
		var beamoIds = localManifest.ServiceDefinitions.Select(sd => sd.BeamoId).ToArray();
		var folders = beamoIds.Select(id => $"{id}_folder").ToArray();

		await UploadContainers(beamoIds, folders, dockerRegistryUrl, CancellationToken.None, onContainerUploadProgress, onContainerUploadCompleted);


		// If all is well with the local deployment, we convert the local manifest into the remote one
		// TODO: When Beam-O gets upgraded, hopefully it'll use the same format locally. Then, we can rename this stuff to BeamoManifest and throw this x-form away.
		var remoteManifest = new ServiceManifest();
		WriteServiceManifestFromLocal(localManifest, comments, perServiceComments, remoteManifest);

		await _beamo.Deploy(remoteManifest);

		return remoteManifest;
	}

	/// <summary>
	/// Modifies the given <paramref name="remoteManifest"/> to match the data in the given <paramref name="localManifest"/> as well as the given <paramref name="comments"/> and
	/// <paramref name="perServiceComments"/>.
	/// </summary>
	public static void WriteServiceManifestFromLocal(BeamoLocalManifest localManifest, string comments, Dictionary<string, string> perServiceComments,
		ServiceManifest remoteManifest)
	{
		// Setup comments
		remoteManifest.comments = comments;

		// Build list of service storage references
		{
			var allMongoServices = localManifest.ServiceDefinitions.Where(sd => sd.Protocol == BeamoProtocolType.EmbeddedMongoDb).ToList();
			var locals = allMongoServices.Select(mongoSd => new ServiceStorageReference()
			{
				id = mongoSd.BeamoId,
				// Tied to Embedded Mongo DB --- if we add other embedded db types, they each get their const.
				storageType = "mongov1",
				templateId = "small",
				enabled = mongoSd.ShouldBeEnabledOnRemote,
			}).ToList();
			foreach (var local in locals)
			{
				var index = remoteManifest.storageReference.FindIndex(reference => reference.id == local.id);
				if (index < 0)
				{
					remoteManifest.storageReference.Add(local);
					continue;
				}

				remoteManifest.storageReference[index] = local;
			}
		}

		// Build list of service references
		{
			var allHttpMicroservices = localManifest.ServiceDefinitions.Where(sd => sd.Protocol == BeamoProtocolType.HttpMicroservice).ToList();
			var locals = allHttpMicroservices.Select(httpSd =>
			{
				var remoteProtocol = localManifest.HttpMicroserviceRemoteProtocols[httpSd.BeamoId];
				if (!perServiceComments.TryGetValue(httpSd.BeamoId, out var httpSdComments))
					httpSdComments = string.Empty;

				return new ServiceReference()
				{
					serviceName = httpSd.BeamoId,
					enabled = httpSd.ShouldBeEnabledOnRemote,
					templateId = "small",
					containerHealthCheckPort = long.Parse(remoteProtocol.HealthCheckPort),
					imageId = httpSd.TruncImageId,
					dependencies = httpSd.DependsOnBeamoIds
						// For now, Beam-O only supports dependencies on Storage Objects (ie.:Embedded Mongo DBs).
						// TODO: change this when Beam-O supports real dependency resolution across services.
						.Where(beamoId => localManifest.ServiceDefinitions.First(sd => sd.BeamoId == beamoId).Protocol == BeamoProtocolType.EmbeddedMongoDb)
						.Select(beamoId => new ServiceDependency() { id = beamoId, storageType = "mongov1" }).ToList(),
					comments = httpSdComments,
				};
			}).ToList();

			foreach (var local in locals)
			{
				var index = remoteManifest.manifest.FindIndex(reference => reference.serviceName == local.serviceName);
				if (index < 0)
				{
					remoteManifest.manifest.Add(local);
					continue;
				}

				remoteManifest.manifest[index] = local;
			}
		}
	}

	#region Image Uploading --- This is some old code adapted from our Unity SDK that could use a review at some point

	// See https://docs.docker.com/registry/spec/manifest-v2-2/#image-manifest-field-descriptions
	private const string MEDIA_MANIFEST = "application/vnd.docker.distribution.manifest.v2+json";
	private const string MEDIA_CONFIG = "application/vnd.docker.container.image.v1+json";

	private const string MEDIA_LAYER = "application/vnd.docker.image.rootfs.diff.tar.gzip";

	//private const int ChunkSize = 1048576; // TODO: Measure performance of different chunk sizes. ~ACM 2019-12-18
	private const int CHUNK_SIZE = 1048576 * 10;

	/// <summary>
	/// This contains everything we need to upload a single container to the registry.
	/// We make one of these per image that we need to upload.
	/// <see cref="BeamoLocalSystem.PrepareContainerUploader"/> and <see cref="BeamoLocalSystem.UploadContainers"/>
	/// </summary>
	public class ContainerUploadData
	{
		public HttpClient Client;
		public string UploadBaseUri;
		public HashAlgorithm Sha256;
		public BeamoServiceDefinition ServiceDefinition;
		public MD5 Md5;
		public string Hash;
		public long PartsCompleted;
		public long PartsAmount;
	}

	/// <summary>
	/// Upload the specified container to the private Docker registry.
	/// </summary>
	public async Task UploadContainers(string[] beamoIds, string[] folders, string dockerRegistryUrl, CancellationToken cancellationToken,
		Action<string, float> onContainerUploadProgress = null,
		Action<string, bool> onContainerUploadCompleted = null)
	{
		var serviceDefinition = beamoIds.Select(id => BeamoManifest.ServiceDefinitions.First(sd => sd.BeamoId == id)).ToList();

		// Prepare the data we need to correctly upload the containers.
		var cid = _ctx.Cid;
		var realmPid = _ctx.Pid;
		var gamePid = (await _realmApi.GetRealm()).FindRoot().Pid; // TODO I really think we should move this to _ctx/ConfigService and grab it during init...
		var accessToken = _ctx.Token.Token;

		var uploadTasks = serviceDefinition
			.Where(sd => sd.Protocol == BeamoProtocolType.HttpMicroservice)
			.Where(VerifyCanBeBuiltLocally)
			.Select(async (sd, i) =>
			{
				var folder = folders[i];
				try
				{
					var stream = await SaveImage(sd.BeamoId);
					var tar = TarArchive.CreateInputTarArchive(stream, Encoding.Default);
					tar.ExtractContents(folder);

					var service = new ECRUploaderService(cid, realmPid, gamePid, accessToken, sd.BeamoId, sd.TruncImageId, dockerRegistryUrl);
					service.OnProgress += onContainerUploadProgress;
					await service.Upload(folder, cancellationToken);
					
					onContainerUploadCompleted?.Invoke(sd.BeamoId, true);
				}
				catch (Exception ex)
				{
					onContainerUploadCompleted?.Invoke(sd.BeamoId, false);
					BeamableLogger.LogError(ex);
					throw;
				}
				finally
				{
					Directory.Delete(folder, true);
				}
			});

		await Task.WhenAll(uploadTasks);
	}
	
	/// <summary>
	/// Upload one layer of a Docker image, adding its digest to the upload
	/// manifest when complete.
	/// </summary>
	/// <param name="layerPath">Filesystem path to the layer archive.</param>
	private static async Task<Dictionary<string, object>> UploadLayer(ContainerUploadData data, Action<string, float> onContainerUploadProgress, string layerPath, CancellationToken token)
	{
		var layerDigest = await UploadFileBlob(data, layerPath, token);
		Interlocked.Increment(ref data.PartsCompleted);
		onContainerUploadProgress?.Invoke(data.ServiceDefinition.BeamoId, (float)Interlocked.Read(ref data.PartsCompleted) / data.PartsAmount);
		return new Dictionary<string, object> { { "digest", layerDigest.Digest }, { "size", layerDigest.Size }, { "mediaType", MEDIA_LAYER } };
	}

	/// <summary>
	/// Upload the manifest JSON to complete the Docker image push.
	/// </summary>
	/// <param name="uploadManifest">Data structure containing image data.</param>
	private static async Task UploadManifestJson(ContainerUploadData data, Dictionary<string, object> uploadManifest, string imageId)
	{
		var manifestJson = Json.Serialize(uploadManifest, new StringBuilder());
		var uri = new Uri($"{data.UploadBaseUri}/manifests/{imageId}");
		var content = new StringContent(manifestJson, Encoding.Default, MEDIA_MANIFEST);
		var response = await data.Client.PutAsync(uri, content);
		response.EnsureSuccessStatusCode();
	}

	/// <summary>
	/// Upload a file blob, which may be config JSON or an image layer.
	/// </summary>
	/// <param name="filename">File to upload.</param>
	/// <returns>Hash digest of the blob.</returns>
	private static async Task<FileBlobResult> UploadFileBlob(ContainerUploadData data, string filename, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		using (var fileStream = File.OpenRead(filename))
		{
			var digest = HashDigest(data.Sha256, fileStream);
			if (await CheckBlobExistence(data, digest))
			{
				return new FileBlobResult { Digest = digest, Size = fileStream.Length };
			}

			fileStream.Position = 0;
			var location = NormalizeWithDigest(await PrepareUploadLocation(data), digest);
			while (fileStream.Position < fileStream.Length)
			{
				token.ThrowIfCancellationRequested();
				var chunk = await FileChunk.FromParent(fileStream, CHUNK_SIZE);
				var response = await UploadChunk(data, chunk, location, token);
				response.EnsureSuccessStatusCode();
				location = NormalizeWithDigest(response.Headers.Location, digest);
			}

			return new FileBlobResult { Digest = digest, Size = fileStream.Length };
		}
	}

	/// <summary>
	/// Check whether a blob exists using a HEAD request.
	/// </summary>
	/// <param name="digest"></param>
	/// <returns></returns>
	private static async Task<bool> CheckBlobExistence(ContainerUploadData data, string digest)
	{
		var uri = new Uri($"{data.UploadBaseUri}/blobs/{digest}");
		var request = new HttpRequestMessage(HttpMethod.Head, uri);
		var response = await data.Client.SendAsync(request);
		return response.StatusCode == HttpStatusCode.OK;
	}

	/// <summary>
	/// Request an upload location for a blob by making POST request to the
	/// upload path.
	/// </summary>
	/// <returns>The upload location URI.</returns>
	private static async Task<Uri> PrepareUploadLocation(ContainerUploadData data)
	{
		var uri = new Uri($"{data.UploadBaseUri}/blobs/uploads/");
		var response = await data.Client.PostAsync(uri, new StringContent(""));
		try
		{
			response.EnsureSuccessStatusCode();
		}
		catch (HttpRequestException ex)
		{
			var body = await response.Content.ReadAsStringAsync();
			BeamableLogger.LogError($"Failed to prepare image upload location. message=[{ex.Message}] body=[{body}] url=[{uri}]");
			throw;
		}

		return response.Headers.Location;
	}

	/// <summary>
	/// Upload a chunk of a file, using PATCH for intermediate chunks or PUT
	/// for the final chunk.
	/// </summary>
	/// <param name="chunk">File chunk including range information.</param>
	/// <param name="location">URI for upload.</param>
	/// <returns>HTTP response.</returns>
	private static async Task<HttpResponseMessage> UploadChunk(ContainerUploadData data, FileChunk chunk, Uri location, CancellationToken token)
	{
		var uri = location;
		var method = chunk.IsLast ? HttpMethod.Put : new HttpMethod("PATCH");
		var request = new HttpRequestMessage(method, uri) { Content = new StreamContent(chunk.Stream) };
		request.Content.Headers.ContentLength = chunk.Length;
		request.Content.Headers.ContentRange = new ContentRangeHeaderValue(chunk.Start, chunk.End, chunk.FullLength);
		var response = await data.Client.SendAsync(request, token);
		try
		{
			response.EnsureSuccessStatusCode();
		}
		catch (HttpRequestException ex)
		{
			var body = await response.Content.ReadAsStringAsync(token);
			BeamableLogger.LogError($"Failed to upload image chunk. message=[{ex.Message}] body=[{body}]");
			throw;
		}

		return response;
	}

	/// <summary>
	/// Given an upload URI, make sure it uses secured HTTP and append the
	/// </summary>
	/// <param name="uri">Original URI.</param>
	/// <param name="digest">Content digest to add.</param>
	/// <returns>Revised URI.</returns>
	private static Uri NormalizeWithDigest(Uri uri, string digest)
	{
		// TODO: Figure out whether http->https redirect is possible without "buffering is needed" error. ~ACM 2019-12-18
		var builder = new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = -1 };
		builder.Query += $"&digest={digest}";
		return builder.Uri;
	}

	/// <summary>
	/// Compute the SHA256 hash digest of the content stream.
	/// </summary>
	/// <param name="stream">Stream containing full content to be hashed.</param>
	/// <returns>Hash digest as a hexadecimal string with algorithm prefix.</returns>
	private static string HashDigest(HashAlgorithm _sha256, Stream stream)
	{
		// TODO: Can hash computation be async? ~ACM 2019-12-16
		// TODO: This seems CPU heavy; let's just trust the hashes from JSON. ~ACM 2019-12-18
		var sb = new StringBuilder("sha256:");

		foreach (var b in _sha256.ComputeHash(stream))
			sb.Append($"{b:x2}");

		return sb.ToString();
	}

	/// <summary>
	/// Gets an MD5 hash as a string.
	/// </summary>
	private static string GetHash(string input, MD5 md5)
	{
		byte[] data = md5.ComputeHash(Encoding.UTF8.GetBytes(input));

		var builder = new StringBuilder();
		for (int i = 0; i < data.Length; i++)
			builder.Append(data[i].ToString("x2"));
		return builder.ToString();
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

	/// <summary>
	/// One chunk of an upload.
	/// </summary>
	private class FileChunk
	{
		public readonly bool IsLast;
		public readonly long Start;
		public long End => Start + Length - 1;
		public readonly long Length;
		public readonly Stream Stream;
		public readonly long FullLength;

		// TODO: How can we make this more efficient? ~ACM 2019-12-18
		private FileChunk(Stream stream, long position, long length, bool isLast, long fullLength)
		{
			Stream = stream;
			Start = position;
			Length = length;
			IsLast = isLast;
			FullLength = fullLength;
		}

		/// <summary>
		/// Create a FileChunk from a parent stream, using the stream's position
		/// and the supplied length as range parameters.
		/// </summary>
		/// <param name="parent">Stream to build from.</param>
		/// <param name="length">Maximum chunk length.</param>
		/// <returns>Resulting chunk with range information.</returns>
		public static async Task<FileChunk> FromParent(Stream parent, long length)
		{
			if (length >= parent.Length && parent.Position == 0)
			{
				return WholeStream(parent);
			}

			var start = parent.Position;
			var isLast = false;
			var chunkLength = length;
			if (parent.Position + chunkLength > parent.Length)
			{
				chunkLength = parent.Length - parent.Position;
				isLast = true;
			}

			var buffer = new byte[chunkLength];
			var _ = await parent.ReadAsync(buffer, 0, (int)chunkLength);
			var stream = new MemoryStream(buffer);
			return new FileChunk(stream, start, chunkLength, isLast, parent.Length);
		}

		/// <summary>
		/// Make a chunk that encompasses the entirety of the supplied stream.
		/// </summary>
		/// <param name="parent">Stream to build from.</param>
		/// <returns>Chunk with range information.</returns>
		private static FileChunk WholeStream(Stream parent)
		{
			return new FileChunk(parent, 0, parent.Length, true, parent.Length);
		}
	}

	private struct FileBlobResult
	{
		public string Digest;
		public long Size;
	}

	#endregion
}
