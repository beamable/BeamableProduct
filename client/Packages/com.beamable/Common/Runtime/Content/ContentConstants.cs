using System.IO;
using UnityEngine;

namespace Beamable.Common.Content
{
   public static class ContentConstants
   {
      public const string PUBLIC = "public";
      public const string PRIVATE = "private";


      //Editor Property Drawer Utils
      public const string MISSING_SUFFIX = " (missing)";
      
      public static readonly string CompressedContentPath = Path.Combine(Application.streamingAssetsPath, "Beamable/bakedContent");
      public static readonly string DecompressedContentPath = Path.Combine(Application.streamingAssetsPath, "Beamable/Baked/Content");
   }
}
