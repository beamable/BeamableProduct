#if UNITY_6000_3_OR_NEWER
using UnityObjectId = UnityEngine.EntityId;
#else
using UnityObjectId = System.Int32;
#endif

namespace Beamable.Editor.Util
{
	internal static class UnityObjectIdentity
	{
		internal static UnityObjectId GetObjectId(UnityEngine.Object target)
		{
#if UNITY_6000_3_OR_NEWER
			return target.GetEntityId();
#else
			return target.GetInstanceID();
#endif
		}
	}
}
