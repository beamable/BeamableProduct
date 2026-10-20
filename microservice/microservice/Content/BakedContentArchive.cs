using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace Beamable.Server.Content
{
   /// <summary>
   /// The content field has the same shape as Unity's ContentDataInfoWrapper.
   /// Unity ignores the additional fields when it loads bakedContent.bytes.
   /// </summary>
   public sealed class BakedContentArchive
   {
      public const int CurrentFormatVersion = 1;
      public const string ContentFileName = "bakedContent.bytes";
      public const string ManifestFileName = "bakedManifest.bytes";

      public int formatVersion = CurrentFormatVersion;
      public string cid;
      public string pid;
      public string manifestId;
      public string manifestUid;
      public List<BakedContentEntry> content = new List<BakedContentEntry>();

      private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { IncludeFields = true };

      public static byte[] Encode(object value, bool compress)
      {
         var json = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
         if (!compress) return json;
         using var output = new MemoryStream();
         using (var gzip = new GZipStream(output, CompressionLevel.Optimal, true))
         {
            gzip.Write(json, 0, json.Length);
         }
         return output.ToArray();
      }

      public static BakedContentArchive Read(string path)
      {
         var bytes = File.ReadAllBytes(path);
         if (bytes.Length >= 2 && bytes[0] == 0x1f && bytes[1] == 0x8b)
         {
            using var input = new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress);
            using var output = new MemoryStream();
            input.CopyTo(output);
            bytes = output.ToArray();
         }
         var archive = JsonSerializer.Deserialize<BakedContentArchive>(bytes, JsonOptions);
         if (archive == null || archive.formatVersion != CurrentFormatVersion || archive.content == null)
            throw new InvalidDataException("Unsupported or empty baked content archive.");
         return archive;
      }
   }

   public sealed class BakedContentEntry
   {
      public string contentId;
      public string contentVersion;
      public string data;
   }
}
