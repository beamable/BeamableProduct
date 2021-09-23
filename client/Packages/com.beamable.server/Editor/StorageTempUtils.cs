using System.Linq;
using Beamable.Server.Editor;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Editor
{
   public class StorageTempUtils
   {
      [MenuItem("TESTING/ListStorages")]
      public static void ListStorage()
      {
         foreach (var storage in Microservices.StorageDescriptors)
         {
            Debug.Log($"Storage {storage.Name} at {storage.AttributePath}");

         }
      }

      [MenuItem("TESTING/ListDeps")]
      public static void ListDeps()
      {
         foreach (var service in Microservices.Descriptors)
         {
            foreach (var storage in service.GetStorageReferences())
            {
               Debug.Log($"{service.Name} requires {storage.Name}");
            }
         }
      }
   }
}