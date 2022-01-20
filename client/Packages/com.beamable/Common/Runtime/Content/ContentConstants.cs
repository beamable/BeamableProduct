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

      public static readonly string BeamableResourcesPath = Path.Combine(Application.dataPath, "Beamable/Resources");
      public static readonly string BakedFileResourcePath = "bakedContent";
      public static readonly string BakedContentFilePath = Path.Combine(BeamableResourcesPath, BakedFileResourcePath);
   }
}
