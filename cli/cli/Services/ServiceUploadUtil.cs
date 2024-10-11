using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Dependencies;
using Beamable.Serialization.SmallerJSON;
using cli.Utils;
using CliWrap;
using Docker.DotNet;
using Serilog;
using SharpCompress.Archives;
using SharpCompress.Archives.Tar;
using SharpCompress.Common;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace cli.Services;

public static class ServiceUploadUtil
{
	private const string MEDIA_MANIFEST = "application/vnd.docker.distribution.manifest.v2+json";
	private const string MEDIA_CONFIG = "application/vnd.docker.container.image.v1+json";

	private const string MEDIA_LAYER = "application/vnd.docker.image.rootfs.diff.tar.gzip";

	public struct FileBlobResult
	{
		public string Digest;
		public long Size;
	}

	static async Task<MemoryStream> SaveDockerImage(string dockerPath, string imageId)
	{
		if (!DockerPathOption.TryValidateDockerExec(dockerPath, out var dockerPathError))
		{
			throw new CliException(dockerPathError);
		}

		var stream = new MemoryStream();
		var argString = $"image save {imageId}";
		var command = Cli
			.Wrap(dockerPath)
			.WithArguments(argString)
			.WithValidation(CommandResultValidation.None)
			.WithStandardOutputPipe(PipeTarget.ToStream(stream, true))
			.WithStandardErrorPipe(PipeTarget.ToDelegate(line =>
			{
				Log.Error($"Failed to save docker image. imageId=[{imageId}] msg=[{line}]");
			}));
		await command.ExecuteAsync();
		
		return stream;
	}
	
	public static async Task Upload(IDependencyProvider provider, 
		string beamoId,
		string imageId,
		string gamePid,
		string dockerRegistryUrl,
		CancellationToken ct,
		Action<float> onProgressCallback)
	{
		var sw = new Stopwatch();
		sw.Start();
		var lastLogTime = sw.ElapsedMilliseconds;

		void LogTime(string msg)
		{
			var elapsed = sw.ElapsedMilliseconds;
			var diff = elapsed - lastLogTime;
			Log.Verbose($"upload perf timer -- {msg} -- {elapsed} - {diff}");
			lastLogTime = sw.ElapsedMilliseconds;
		}
		
		onProgressCallback?.Invoke(.02f);
		// TODO: handle image deletion with a nicer exception, "plan references image that no longer exists locally", "replan"
		using var memoryImageStream = await SaveDockerImage(provider.GetService<IAppContext>().DockerPath, imageId);
		memoryImageStream.Position = 0;
		
		LogTime("copied image to local memory");

		onProgressCallback?.Invoke(.04f);

		memoryImageStream.Position = 0;
		
		using var imageArchive = TarArchive.Open(memoryImageStream);

		var ctx = provider.GetService<IAppContext>();
		
		onProgressCallback?.Invoke(.05f);
		
		var serviceUniqueName = GetHash($"{ctx.Cid}_{gamePid}_{beamoId}")
			// This substring exists because there is a char-length limit on the remote beamo registry :(
			.Substring(0, 30);
		var baseUrl = dockerRegistryUrl + serviceUniqueName + "/";

		var client = new HttpClient
		{
			Timeout = Timeout.InfiniteTimeSpan,
			BaseAddress = new Uri(baseUrl),
			DefaultRequestVersion = HttpVersion.Version20,
			DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
		};
		client.DefaultRequestHeaders.Add("x-ks-clientid", ctx.Cid);
		client.DefaultRequestHeaders.Add("x-ks-projectid", ctx.Pid);
		client.DefaultRequestHeaders.Add("x-ks-token", provider.GetService<IBeamableRequester>().AccessToken.Token);


		var (hasManifest, manifestBytes) = await TryGetBytesForEntry(imageArchive, "manifest.json");
		if (!hasManifest)
			throw new CliException($"unable to find manifest.json entry in archive. service=[{beamoId}] image=[{imageId}]");
		
		var manifest = BeamoLocalSystem.DockerManifest.FromBytes(manifestBytes);

		LogTime("got manifest");

		var uploadManifest = new Dictionary<string, object>
		{
			{ "schemaVersion", 2 }, 
			{ "mediaType", MEDIA_MANIFEST }, 
			{ "config", new Dictionary<string, object>
			{
				{ "mediaType", MEDIA_CONFIG }
			} }, 
			{ "layers", new List<object>() },
		};
		var config = (Dictionary<string, object>)uploadManifest["config"];
		var layers = (List<object>)uploadManifest["layers"];
		
		var manifestStream = await imageArchive.OpenEntryAsMemoryStream($"{manifest.config}");
		var configResult = (await UploadFileBlob(client, manifestStream, ct, (bytes, progress) =>
		{
			onProgressCallback?.Invoke(.05f + .05f * progress);
		}));
		ct.ThrowIfCancellationRequested();
		
		config["digest"] = configResult.Digest;
		config["size"] = configResult.Size;

		// upload the blobs
		var uploadIndexToJob = new SortedDictionary<int, Task<Dictionary<string, object>>>();
		var uploadTasks = new List<Task>();
		
		var progressBytes = new long[manifest.layers.Length];
		var layerStreams = new MemoryStream[manifest.layers.Length];
		var totalUploadSize = 0L;
		for (var i = 0; i < manifest.layers.Length; i++)
		{
			var index = i;
			var layerStream = await imageArchive.OpenEntryAsMemoryStream(manifest.layers[index]);
			totalUploadSize += layerStream.Length;
			layerStreams[index] = layerStream;
		}
		
		for (var i = 0; i < manifest.layers.Length; i++)
		{
			var index = i;
			var layer = layerStreams[i];
			var uploadTask = UploadLayer(client, layer, (bytes, _) =>
			{
				progressBytes[index] = bytes;
				var totalProgress = progressBytes.Sum() / (float)totalUploadSize;
				onProgressCallback?.Invoke(.1f + totalProgress * .85f );
			}, ct);
			uploadIndexToJob.Add(i, uploadTask);
			uploadTasks.Add(uploadTask);
		}
		
		await Task.WhenAll(uploadTasks.ToArray());
		onProgressCallback?.Invoke(.95f);
		

		foreach (var kvp in uploadIndexToJob)
		{
			layers.Add(kvp.Value.Result);
		}
		onProgressCallback?.Invoke(.96f);

		await UploadManifestJson(client, uploadManifest, imageId, progress =>
		{
			onProgressCallback?.Invoke(.96f + .03f * progress);
		});
		
		onProgressCallback?.Invoke(1);
	}
	
	
	/// <summary>
	/// Upload the manifest JSON to complete the Docker image push.
	/// </summary>
	/// <param name="uploadManifest">Data structure containing image data.</param>
	private static async Task UploadManifestJson(HttpClient http, Dictionary<string, object> uploadManifest, string imageId, Action<float> onProgressCallback)
	{
		var manifestJson = Json.Serialize(uploadManifest, new StringBuilder());
		var manifestBytes = Encoding.UTF8.GetBytes(manifestJson);
		var streamContent = new ObservableByteArrayContent(manifestBytes);
		streamContent.OnProgress += (_, ratio) =>
		{
			onProgressCallback?.Invoke(ratio);
		};
		streamContent.Headers.ContentType =  new MediaTypeHeaderValue(MEDIA_MANIFEST);

		var response = await http.PutAsync($"manifests/{imageId}", streamContent);
		response.EnsureSuccessStatusCode();
	}
	
	/// <summary>
	/// Upload one layer of a Docker image, adding its digest to the upload
	/// manifest when complete.
	/// </summary>
	/// <param name="layerPath">Filesystem path to the layer archive.</param>
	private static async Task<Dictionary<string, object>> UploadLayer(HttpClient http, MemoryStream layerStream, Action<int, float> onContainerUploadProgress, CancellationToken token)
	{
		var layerDigest = await UploadFileBlob(http, layerStream, token, onContainerUploadProgress, chunkSize: 2<<13); // 2 << 13
		return new Dictionary<string, object> { { "digest", layerDigest.Digest }, { "size", layerDigest.Size }, { "mediaType", MEDIA_LAYER } };
	}
	
	
	
	/// <summary>
	/// Upload a file blob, which may be config JSON or an image layer.
	/// </summary>
	/// <param name="filename">File to upload.</param>
	/// <returns>Hash digest of the blob.</returns>
	private static async Task<FileBlobResult> UploadFileBlob(
		HttpClient http,
		MemoryStream stream,
		CancellationToken token,
		Action<int, float> onProgressCallback,
		int chunkSize=4069)
	{
		token.ThrowIfCancellationRequested();
		onProgressCallback?.Invoke(0, 0);
		var digest = HashDigest(stream);
		if (await CheckBlobExistence(http, digest))
		{
			onProgressCallback?.Invoke((int)stream.Length, 1);
			return new FileBlobResult { Digest = digest, Size = stream.Length};
		}

		stream.Position = 0;
		var location = NormalizeWithDigest(await PrepareUploadLocation(http), digest);
		var response = await UploadStream(http, stream, location, digest, onProgressCallback, token, chunkSize);

		response.EnsureSuccessStatusCode();
		
		return new FileBlobResult { Digest = digest, Size = stream.Length };
	}
	
	
	/// <summary>
	/// Upload a chunk of a file, using PATCH for intermediate chunks or PUT
	/// for the final chunk.
	/// </summary>
	/// <param name="chunk">File chunk including range information.</param>
	/// <param name="location">URI for upload.</param>
	/// <returns>HTTP response.</returns>
	private static async Task<HttpResponseMessage> UploadStream(HttpClient http, MemoryStream dataStream, Uri location, string digest, Action<int, float> progressCallback, CancellationToken token, int chunkSize=4096)
	{
		var uri = location;
		var method = HttpMethod.Put;

		dataStream.Position = 0;
		var content = new ObservableByteArrayContent(dataStream.ToArray(), chunkSize);
		content.OnProgress = progressCallback;
		
		var request = new HttpRequestMessage(method, uri) { Content = content, Version = HttpVersion.Version20 };
		request.Content.Headers.ContentLength = dataStream.Length;
		request.Content.Headers.ContentRange = new ContentRangeHeaderValue(0, dataStream.Length, dataStream.Length);
		
		var response = await http.SendAsync(request, token);
		try
		{
			response.EnsureSuccessStatusCode();
		}
		catch (HttpRequestException ex)
		{
			var body = await response.Content.ReadAsStringAsync(token);
			Log.Error($"Failed to upload image chunk. message=[{ex.Message}] body=[{body}]");
			throw;
		}
		finally
		{
			progressCallback?.Invoke((int)dataStream.Length, 1f);
		}

		return response;
	}
	
	private static async Task<HttpResponseMessage> UploadStreamMulti(HttpClient http, MemoryStream dataStream, Uri location, string digest, Action<int, float> progressCallback, CancellationToken token, int chunkSize=4096)
	{
		// split the dataStream out into chunks...
		var segments = new List<ArraySegment<byte>>();
		// var requestChunkSize = 1048576 * 10;
		var requestChunkSize = 2 << 15;
		var bytes = dataStream.ToArray();
		for (var i = 0; i < bytes.Length; i += requestChunkSize)
		{
			var start = i;
			var size = Math.Min(requestChunkSize, bytes.Length - i);
			segments.Add(new ArraySegment<byte>(bytes, start, size));
		}

		var pendingTasks = new List<Task>();
		var writtenAmounts = new int[segments.Count];
		
		for (var i = 0; i < segments.Count; i++)
		{
			var index = i;
			var isLast = i == segments.Count - 1;
			var method = isLast 
				? HttpMethod.Put 
				: new HttpMethod("PATCH");

			var segment = segments[i];
			var content = new ObservableByteArrayContent(segment.ToArray(), chunkSize);
			writtenAmounts[index] = 0;
			content.OnProgress += (writtenBytes, ratio) =>
			{
				writtenAmounts[index] = writtenBytes;
				var total = writtenAmounts.Sum();
				var wholeRatio = (total / (float)dataStream.Length);
				progressCallback?.Invoke(total, wholeRatio);
			};
			// var content = new StreamContent(new MemoryStream(segment.ToArray()));
			var request = new HttpRequestMessage(method, location) { Content = content};
			request.Content.Headers.ContentLength = segment.Count;
			request.Content.Headers.ContentRange = new ContentRangeHeaderValue(segment.Offset, segment.Offset + segment.Count - 1, dataStream.Length);

			var requestTask = http.SendAsync(request, token);
			pendingTasks.Add(requestTask);
			var response = await requestTask;
			try
			{
				response.EnsureSuccessStatusCode();
				location = NormalizeWithDigest(response.Headers.Location, digest);
			}
			catch (HttpRequestException ex)
			{
				// TODO: in an error case, we need to cancel all other requests...
				var body = await response.Content.ReadAsStringAsync(token);
				Log.Error($"Failed to upload image chunk. message=[{ex.Message}] body=[{body}] segment-start=[{segment.Offset}] segment-length=[{segment.Count}]");
				throw;
			}
		}

		try
		{
			await Task.WhenAll(pendingTasks);
		}
		finally
		{
			progressCallback?.Invoke((int)dataStream.Length, 1f);
		}

		return default;
	}
	
	/// <summary>
	/// Check whether a blob exists using a HEAD request.
	/// </summary>
	/// <param name="digest"></param>
	/// <returns></returns>
	private static async Task<bool> CheckBlobExistence(HttpClient http, string digest)
	{
		var request = new HttpRequestMessage(HttpMethod.Head, $"blobs/{digest}");
		var response = await http.SendAsync(request);

		// TODO: retest this.
		var maxIter = 10;
		while (maxIter-- > 0 && response.StatusCode == HttpStatusCode.TemporaryRedirect)
		{
			var nextLocation = response.Headers.Location;
			response = await http.SendAsync(new HttpRequestMessage(HttpMethod.Head, nextLocation));
		}
		
		return response.StatusCode == HttpStatusCode.OK;
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
	/// Request an upload location for a blob by making POST request to the
	/// upload path.
	/// </summary>
	/// <returns>The upload location URI.</returns>
	private static async Task<Uri> PrepareUploadLocation(HttpClient http)
	{
		var response = await http.PostAsync("blobs/uploads/", new StringContent(""));
		try
		{
			response.EnsureSuccessStatusCode();
		}
		catch (HttpRequestException ex)
		{
			var body = await response.Content.ReadAsStringAsync();
			Log.Error($"Failed to prepare image upload location. message=[{ex.Message}] body=[{body}] url=[\"blobs/uploads/\"]");
			throw;
		}

		return response.Headers.Location;
	}
	

	/// <summary>
	/// Gets an MD5 hash as a string.
	/// </summary>
	public static string GetHash(string input)
	{
		byte[] data = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(input));

		var builder = new StringBuilder();
		for (int i = 0; i < data.Length; i++)
			builder.Append(data[i].ToString("x2"));
		return builder.ToString();
	}
	
	
	/// <summary>
	/// Compute the SHA256 hash digest of the content stream.
	/// </summary>
	/// <param name="stream">Stream containing full content to be hashed.</param>
	/// <returns>Hash digest as a hexadecimal string with algorithm prefix.</returns>
	private static string HashDigest(Stream stream)
	{
		// TODO: Can hash computation be async? ~ACM 2019-12-16
		// TODO: This seems CPU heavy; let's just trust the hashes from JSON. ~ACM 2019-12-18
		var sb = new StringBuilder("sha256:");

		foreach (var b in (SHA256.Create()).ComputeHash(stream))
			sb.Append($"{b:x2}");

		return sb.ToString();
	}

	static bool TryGetEntry(TarArchive archive, string entryName, out IArchiveEntry entry)
	{
		entry = archive.Entries.FirstOrDefault(e => e.Key == entryName);
		return entry != null;
	}

	
	
	static async Task<byte[]> GetBytesFromStream(Stream stream)
	{
		var mem = new MemoryStream();
		await stream.CopyToAsync(mem);
		mem.Position = 0;
		return mem.ToArray();
	}
	
	static async Task<(bool, byte[])> TryGetBytesForEntry(TarArchive archive, string entryName)
	{
		if (!TryGetEntry(archive, entryName, out var entry))
		{
			return (false, Array.Empty<byte>());
		}

		using var entryStream = entry.OpenEntryStream();
		var bytes = await GetBytesFromStream(entryStream);
		return (true, bytes);
	}

	public static Task<MemoryStream> OpenEntryAsMemoryStream(this TarArchive archive, string entryName)
	{
		if (!TryGetEntry(archive, entryName, out var entry))
		{
			throw new CliException("tar does not contain an archive " + entryName);
		}

		return entry.OpenEntryAsMemoryStream();
	}
	
	public static async Task<MemoryStream> OpenEntryAsMemoryStream(this IArchiveEntry entry)
	{
		using var entryStream = entry.OpenEntryStream();
		var mem = new MemoryStream();
		await entryStream.CopyToAsync(mem);
		mem.Position = 0;
		return mem;
	}
}
