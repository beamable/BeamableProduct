using System.Collections.Generic;
using Beamable.Serialization;
using JetBrains.Annotations;

namespace Beamable.Editor.Content.SaveRequest
{
   public class ManifestSaveRequest : JsonSerializable.ISerializable
   {
      public string Id;
      public List<ManifestReferenceSuperset> References;
      public void Serialize(JsonSerializable.IStreamSerializer s)
      {
         s.Serialize("id", ref Id);
         s.SerializeList("references", ref References);
      }
   }

   public class ManifestReferenceSuperset : JsonSerializable.ISerializable
   {
      public string Type;
      public string Id;
      public string Version;
      public string Uri;
      public string TypeName => Id.Substring(0, Id.LastIndexOf('.'));
      [CanBeNull] public string[] Tags;
      [CanBeNull] public string Checksum;
      [CanBeNull] public string Visibility;
      public void Serialize(JsonSerializable.IStreamSerializer s)
      {
         s.Serialize("type", ref Type);
         s.Serialize("id", ref Id);
         s.Serialize("version", ref Version);
         s.Serialize("uri", ref Uri);


         if (Tags != null)
         {
            s.SerializeArray("tags", ref Tags);
         }

         s.Serialize("checksum", ref Checksum);
         s.Serialize("visibility", ref Visibility);
      }

      public string Key => MakeKey(Id, Visibility);

      public static string MakeKey(string id, string visibility)
      {
         return $"{id}/{visibility}";
      }
   }
}