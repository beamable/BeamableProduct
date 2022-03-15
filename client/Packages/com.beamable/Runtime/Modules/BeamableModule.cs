using Beamable.InputManagerIntegration;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Beamable
{
	public class BeamableModule : MonoBehaviour
	{
		void Start()
		{
			if (EventSystem.current == null && Application.isPlaying)
			{
				BeamableInput.AddInputSystem();
			}
		}
	}
}
