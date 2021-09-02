using System;
using System.IO;

namespace Beamable.Server.Editor
{
   [System.Serializable]
   public class MicroserviceDescriptor
   {
      public const string ASSEMBLY_FOLDER_NAME = "_assemblyReferences";

      public string Name { get; set; }
      public string AttributePath { get; set; }
      public Type Type { get; set; }

      public string SourcePath => Path.GetDirectoryName(AttributePath);
      public string HidePath => $"./Assets/~/beamservicehide/{Name}";

      public string BuildPath => $"./Assets/../Temp/beamservicebuild/{Name}";
      public string ContainerName => $"{Name}_container";
      public string ImageName => Name.ToLower();
   }
}