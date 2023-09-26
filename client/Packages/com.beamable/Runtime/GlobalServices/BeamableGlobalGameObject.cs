using Beamable.Common;
using Beamable.Common.Dependencies;
using UnityEngine;

namespace Beamable
{
	/// <summary>
	/// This class will create a global GameObject for the global dependency injection scope
	/// available in <see cref="Beam.GlobalScope"/>
	/// </summary>
	public class BeamableGlobalGameObject : IGameObjectContext, IBeamableDisposableOrder
	{
		private GameObject _instance;

		public GameObject GameObject
		{
			get
			{
				if (!Application.isPlaying) return null;
				
				if (_instance) return _instance.gameObject;
				var gob = new GameObject("BeamableGlobal");
				Object.DontDestroyOnLoad(gob);
				gob.hideFlags = HideFlags.DontSave;
				return gob;
			}
		}

		public Promise OnDispose()
		{
			Object.Destroy(_instance.gameObject);
			return Promise.Success;
		}

		public int DisposeOrder => 200;
	}
}
