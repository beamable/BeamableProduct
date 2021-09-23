using System;
using System.IO;
using UnityEngine;

namespace Beamable.Server.Editor
{
   [Serializable]
   public class MicroserviceDescriptor : IDescriptor
   {
      public const string ASSEMBLY_FOLDER_NAME = "_assemblyReferences";

      [SerializeField]
      private string _name;
      public string Name
      {
         get => _name;
         set => _name = value;
      }
      public string AttributePath { get; set; }
      public Type Type { get; set; }

      public string SourcePath => Path.GetDirectoryName(AttributePath);
      public string HidePath => $"./Assets/~/beamservicehide/{Name}";

      public string BuildPath => $"./Assets/../Temp/beamservicebuild/{Name}";
      public string ContainerName => $"{Name}_container";
      public string ImageName => Name.ToLower();
   }
}