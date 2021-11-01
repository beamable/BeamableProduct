using System;
using Beamable.Common;
using UnityEngine;

namespace Beamable.Server.Editor
{

   [Serializable]
   public class IntDataWrapper : DataWrapper<int> { /* unused but needed for generation */}
   [Serializable]
   public class VersionDataWrapper : DataWrapper<PackageVersion> { /* unused but needed for generation */}

   [Serializable]
   public class DataWrapper<T> : ScriptableObject
   {
      public T InnerData;

      public static DataWrapper<T> Create()
      {
         return CreateInstance<DataWrapper<T>>();
      }
   }
}