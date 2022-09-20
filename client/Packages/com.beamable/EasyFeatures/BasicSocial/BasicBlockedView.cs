using UnityEngine;

namespace Beamable.EasyFeatures.BasicSocial
{
	public class BasicBlockedView : MonoBehaviour, IBeamableView
	{
		public bool IsVisible
		{
			get => gameObject.activeSelf;
			set => gameObject.SetActive(value);
		}

		public int GetEnrichOrder()
		{
			return 0;
		}
	}
}
