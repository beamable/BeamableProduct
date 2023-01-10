using UnityEngine;

namespace Beamable.EasyFeatures.Components
{
	public class CustomOverlay : MonoBehaviour, IOverlayComponent
	{
		public virtual void Show()
		{
			gameObject.SetActive(true);
		}

		public virtual void Hide()
		{
			Destroy(gameObject);
		}
	}
}
