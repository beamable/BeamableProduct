using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;
using Beamable.Common.Content;
using Beamable.Server.Content;
using Beamable.Server.Common;
using NUnit.Framework;
using UnityEngine;

namespace microserviceTests.microservice.Content;

[TestFixture]
public class BakedContentArchiveTests
{
   private static readonly JsonSerializerOptions Fields = new() { IncludeFields = true };

   [Test]
   public async Task CompressedArchiveRoundTripsAndRetainsUnityContentShape()
   {
      var archive = new BakedContentArchive
      {
         cid = "customer",
         pid = "realm",
         manifestId = "global",
         manifestUid = "manifest-1",
         content = new List<BakedContentEntry>
         {
            new() { contentId = "items.sword", contentVersion = "version-1", data = "{\"id\":\"items.sword\",\"version\":\"version-1\",\"properties\":{}}" }
         }
      };
      var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".bytes");
      try
      {
         await File.WriteAllBytesAsync(path, BakedContentArchive.Encode(archive, true));
         var loaded = BakedContentArchive.Read(path);
         Assert.That(loaded.cid, Is.EqualTo("customer"));
         Assert.That(loaded.pid, Is.EqualTo("realm"));
         Assert.That(loaded.content[0].data, Is.EqualTo(archive.content[0].data));

         using var input = new GZipStream(File.OpenRead(path), CompressionMode.Decompress);
         using var document = await JsonDocument.ParseAsync(input);
         Assert.That(document.RootElement.GetProperty("content")[0].GetProperty("contentId").GetString(), Is.EqualTo("items.sword"));
         // Unity's loader deserializes the content list and ignores the server metadata.
         var unityData = JsonSerializer.Deserialize<ContentDataInfoWrapper>(document.RootElement.GetRawText(), Fields);
         Assert.That(unityData.content[0].contentVersion, Is.EqualTo("version-1"));
      }
      finally { File.Delete(path); }
   }

   [Test]
   public void UnknownFormatIsRejected()
   {
      var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".bytes");
      try
      {
         File.WriteAllText(path, "{\"formatVersion\":99,\"content\":[]}");
         Assert.Throws<InvalidDataException>(() => BakedContentArchive.Read(path));
      }
      finally { File.Delete(path); }
   }

   [Test]
   public void EncodedManifestCanBeReadByUnityJsonContract()
   {
      JsonUtilityConverter.Init();
      var manifest = new ClientManifest
      {
         uid = "manifest-1",
         entries = new List<ClientContentInfo>
         {
            new() { contentId = "items.sword", version = "version-1", uri = "https://example.invalid/sword" }
         }
      };
      var bytes = BakedContentArchive.Encode(manifest, true);
      using var input = new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress);
      using var reader = new StreamReader(input);
      var restored = JsonUtility.FromJson<ClientManifest>(reader.ReadToEnd());
      Assert.That(restored.uid.GetOrElse(""), Is.EqualTo("manifest-1"));
      Assert.That(restored.entries[0].contentId, Is.EqualTo("items.sword"));
   }

   [Test]
   public void UnityFormatContentCanBeReadByUnityJsonContract()
   {
      JsonUtilityConverter.Init();
      var entries = new List<BakedContentEntry>
      {
         new() { contentId = "items.sword", contentVersion = "version-1", data = "{\"id\":\"items.sword\",\"version\":\"version-1\",\"properties\":{}}" }
      };
      var bytes = BakedContentArchive.Encode(new { content = entries }, true);
      using var input = new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress);
      using var reader = new StreamReader(input);
      var restored = JsonUtility.FromJson<ContentDataInfoWrapper>(reader.ReadToEnd());
      Assert.That(restored.TryGetContent("items.sword", "version-1", out var found), Is.True);
      Assert.That(found.data, Is.EqualTo(entries[0].data));
   }
}
