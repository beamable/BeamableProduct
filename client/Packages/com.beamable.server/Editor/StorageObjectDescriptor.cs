using System;
using UnityEngine;

namespace Beamable.Server.Editor
{
   [System.Serializable]
   public class StorageObjectDescriptor : IDescriptor
   {
      [SerializeField]
      private string _name;
      public string Name
      {
         get => _name;
         set => _name = value;
      }
      public string AttributePath { get; set; }
      public Type Type { get; set; }
      public string ContainerName => $"{Name}_storage";
      public string ImageName => "mongo:latest";

      public string LocalToolContainerName => $"{Name}_storage_tool";
      public string ToolImageName => $"mongo-express:latest";
   }
}