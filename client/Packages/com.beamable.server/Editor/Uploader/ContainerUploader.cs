using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Beamable.Serialization.SmallerJSON;
using Beamable.Editor;
using Beamable.Editor.Environment;
using UnityEngine;

namespace Beamable.Server.Editor.Uploader
{
public class ContainerUploader
   {
      // See https://docs.docker.com/registry/spec/manifest-v2-2/#image-manifest-field-descriptions
      private const string MediaManifest = "application/vnd.docker.distribution.manifest.v2+json";
      private const string MediaConfig = "application/vnd.docker.container.image.v1+json";
      private const string MediaLayer = "application/vnd.docker.image.rootfs.diff.tar.gzip";
      //private const int ChunkSize = 1048576; // TODO: Measure performance of different chunk sizes. ~ACM 2019-12-18
      private const int ChunkSize = 1048576 * 10;

      /// <summary>
      /// Tag to apply when uploading images.
      /// </summary>
      private const string DockerTagReference = "latest";

      private readonly HttpClient _client;
      private readonly string _uploadBaseUri;
      private readonly HashAlgorithm _sha256;
      private readonly ContainerUploadHarness _harness;
      private readonly MicroserviceDescriptor _descriptor;
      private readonly string _imageId;
      private readonly MD5 _md5 = MD5.Create();

      public ContainerUploader(EditorAPI api, ContainerUploadHarness harness, MicroserviceDescriptor descriptor, string imageId)
      {
         _client = new HttpClient();
         _client.DefaultRequestHeaders.Add("x-ks-clientid", api.CustomerView.Cid);
         _client.DefaultRequestHeaders.Add("x-ks-projectid", api.Pid);
         _client.DefaultRequestHeaders.Add("x-ks-token", api.Token.Token);
         var serviceUniqueName = GetHash($"{api.CustomerView.Cid}_{api.ProductionRealm.Pid}_{descriptor.Name}").Substring(0, 30);
         _uploadBaseUri = $"{BeamableEnvironment.DockerRegistryUrl}{serviceUniqueName}";
         _sha256 = SHA256.Create();
         _harness = harness;
         _descriptor = descriptor;
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

      /// <summary>
      /// Upload a Docker image that has been expanded into a folder.
      /// </summary>
      /// <param name="folder">The filesystem path to the expanded image.</param>
      public async Task Upload(string folder)
      {
         var manifest = DockerManifest.FromBytes(File.ReadAllBytes($"{folder}/manifest.json"));
         var uploadManifest = new Dictionary<string, object>
         {
            {"schemaVersion", 2},
            {"mediaType", MediaManifest},
            {"config", new Dictionary<string, object> {{"mediaType", MediaConfig}}},
            {"layers", new List<object>()},
//            {"tag", _imageId}
         };
         var config = (Dictionary<string, object>) uploadManifest["config"];
         var layers = (List<object>) uploadManifest["layers"];

         // Upload the config JSON as a blob.
         var configResult = (await UploadFileBlob($"{folder}/{manifest.config}"));
         config["digest"] = configResult.Digest;
         config["size"] = configResult.Size;

         // Upload all layer blobs.
         var uploadIndexToJob = new SortedDictionary<int, Task<Dictionary<string, object>>>();
         for (var i = 0; i < manifest.layers.Length; i++)
         {
            var layer = manifest.layers[i];
            uploadIndexToJob.Add(i, UploadLayer($"{folder}/{layer}"));
         }

         await Task.WhenAll(uploadIndexToJob.Values);
         foreach (var kvp in uploadIndexToJob)
         {
            layers.Add(kvp.Value.Result);
         }

         // Upload manifest JSON.
         await UploadManifestJson(uploadManifest, _imageId);
      }

      /// <summary>
      /// Upload the manifest JSON to complete the Docker image push.
      /// </summary>
      /// <param name="uploadManifest">Data structure containing image data.</param>
      private async Task UploadManifestJson(Dictionary<string, object> uploadManifest, string imageId)
      {
         var manifestJson = Json.Serialize(uploadManifest, new StringBuilder());
         var uri = new Uri($"{_uploadBaseUri}/manifests/{imageId}");
         var content = new StringContent(manifestJson, Encoding.Default, MediaManifest);
         var response = await _client.PutAsync(uri, content);
         response.EnsureSuccessStatusCode();
         _harness.Log("image manifest uploaded");
      }

      /// <summary>
      /// Upload one layer of a Docker image, adding its digest to the upload
      /// manifest when complete.
      /// </summary>
      /// <param name="layerPath">Filesystem path to the layer archive.</param>
      private async Task<Dictionary<string, object>> UploadLayer(string layerPath)
      {
         var layerDigest = await UploadFileBlob(layerPath);
         return new Dictionary<string, object>
         {
            {"digest", layerDigest.Digest},
            {"size", layerDigest.Size},
            {"mediaType", MediaLayer}
         };
      }

      /// <summary>
      /// Upload a file blob, which may be config JSON or an image layer.
      /// </summary>
      /// <param name="filename">File to upload.</param>
      /// <returns>Hash digest of the blob.</returns>
      private async Task<FileBlobResult> UploadFileBlob(string filename)
      {
         using (var fileStream = File.OpenRead(filename))
         {
            var digest = HashDigest(fileStream);
            if (await CheckBlobExistence(digest))
            {
               _harness.ReportUploadProgress(digest, fileStream.Length, fileStream.Length);
               return new FileBlobResult
               {
                  Digest = digest,
                  Size = fileStream.Length
               };
            }
            fileStream.Position = 0;
            var location = NormalizeWithDigest(await PrepareUploadLocation(), digest);
            while (fileStream.Position < fileStream.Length)
            {
               var chunk = await FileChunk.FromParent(fileStream, ChunkSize);
               var response = await UploadChunk(chunk, location);
               response.EnsureSuccessStatusCode();
               _harness.ReportUploadProgress(digest, chunk.End, chunk.FullLength);
               location = NormalizeWithDigest(response.Headers.Location, digest);
            }
            return new FileBlobResult
            {
               Digest = digest,
               Size = fileStream.Length
            };
         }
      }

      struct FileBlobResult
      {
         public string Digest;
         public long Size;
      }

      /// <summary>
      /// Upload a chunk of a file, using PATCH for intermediate chunks or PUT
      /// for the final chunk.
      /// </summary>
      /// <param name="chunk">File chunk including range information.</param>
      /// <param name="location">URI for upload.</param>
      /// <returns>HTTP response.</returns>
      private async Task<HttpResponseMessage> UploadChunk(FileChunk chunk, Uri location)
      {
         var uri = location;
         var method = chunk.IsLast ? HttpMethod.Put : new HttpMethod("PATCH");
         var request = new HttpRequestMessage(method, uri) {Content = new StreamContent(chunk.Stream)};
         request.Content.Headers.ContentLength = chunk.Length;
         request.Content.Headers.ContentRange = new ContentRangeHeaderValue(chunk.Start, chunk.End, chunk.FullLength);
         var response = await _client.SendAsync(request);
         try
         {
            response.EnsureSuccessStatusCode();
         }
         catch (HttpRequestException ex)
         {
            var body = await response.Content.ReadAsStringAsync();
            Debug.LogError($"Failed to upload image chunk. message=[{ex.Message}] body=[{body}]");
            throw ex;
         }
         return response;
      }

      /// <summary>
      /// Check whether a blob exists using a HEAD request.
      /// </summary>
      /// <param name="digest"></param>
      /// <returns></returns>
      private async Task<bool> CheckBlobExistence(string digest)
      {
         var uri = new Uri($"{_uploadBaseUri}/blobs/{digest}");
         var request = new HttpRequestMessage(HttpMethod.Head, uri);
         var response = await _client.SendAsync(request);
         return response.StatusCode == HttpStatusCode.OK;
      }

      /// <summary>
      /// Request an upload location for a blob by making POST request to the
      /// upload path.
      /// </summary>
      /// <returns>The upload location URI.</returns>
      private async Task<Uri> PrepareUploadLocation()
      {
         var uri = new Uri($"{_uploadBaseUri}/blobs/uploads/");
         var response = await _client.PostAsync(uri, new StringContent(""));
         try
         {
            response.EnsureSuccessStatusCode();
         }
         catch (HttpRequestException ex)
         {
            var body = await response.Content.ReadAsStringAsync();
            Debug.LogError($"Failed to prepare image upload location. message=[{ex.Message}] body=[{body}] url=[{uri}]");
            throw ex;
         }

         return response.Headers.Location;
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
         var builder = new UriBuilder(uri) {Scheme = Uri.UriSchemeHttps, Port = -1};
         builder.Query += $"&digest={digest}";
         return builder.Uri;
      }

      /// <summary>
      /// Compute the SHA256 hash digest of the content stream.
      /// </summary>
      /// <param name="stream">Stream containing full content to be hashed.</param>
      /// <returns>Hash digest as a hexadecimal string with algorithm prefix.</returns>
      private string HashDigest(Stream stream)
      {
         // TODO: Can hash computation be async? ~ACM 2019-12-16
         // TODO: This seems CPU heavy; let's just trust the hashes from JSON. ~ACM 2019-12-18
         var sb = new StringBuilder("sha256:");
         foreach (var b in _sha256.ComputeHash(stream))
         {
            sb.Append($"{b:x2}");
         }
         return sb.ToString();
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
            var manifests = (List<object>) Json.Deserialize(bytes);
            var firstManifest = (IDictionary<string, object>) manifests[0];
            result.config = firstManifest["Config"].ToString();
            var layers = (List<object>) firstManifest["Layers"];
            result.layers = layers?.Select(x => x.ToString()).ToArray();
            return result;
         }
      }
   }
}