using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Serialization;

namespace Beamable.Common.Content
{
   [System.Serializable]
   [Agnostic]
   public class ClientPermissions
   {
      [Tooltip(ContentObject.WriteSelf1)]
      [FormerlySerializedAs("write_self")]
      [ContentField("write_self")]
      public bool writeSelf;
   }
   
   [System.Serializable]
   [Agnostic]
   public class OptionalClientPermissions : Optional<ClientPermissions>
   { }
   
}
