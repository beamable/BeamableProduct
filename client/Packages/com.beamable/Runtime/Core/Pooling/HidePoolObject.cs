using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Pooling
{
	public class HidePoolObject : MonoBehaviour
	{
		[System.NonSerialized]
		public HidePool.Pool owner;

		// intrusive list for pool and user of pool
		[System.NonSerialized]
		public LinkedListNode<HidePoolObject> pool = null;
	}
}


