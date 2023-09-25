using Beamable.Common;
using Beamable.Common.Dependencies;
using UnityEngine;

namespace Beamable
{
	public class BeamableGlobalGameObject : IGameObjectContext, IBeamableDisposableOrder
	{
		private BeamableGlobalBehaviour _instance;

		public GameObject GameObject
		{
			get
			{
				if (!Application.isPlaying) return null;
				
				if (_instance) return _instance.gameObject;
				var gob = new GameObject("GlobalBeamable");
				Object.DontDestroyOnLoad(gob);
				gob.hideFlags = HideFlags.HideAndDontSave;
				
				_instance = gob.AddComponent<BeamableGlobalBehaviour>();
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
