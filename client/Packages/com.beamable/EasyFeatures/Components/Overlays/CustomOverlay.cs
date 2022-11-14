using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.Components
{
	public class CustomOverlay : MonoBehaviour, IOverlayComponent
	{
		public Button CloseButton;

		private GameObject _instantiated;
		
		public void Show(GameObject objectToInstantiate)
		{
			CloseButton.onClick.RemoveAllListeners();
			CloseButton.onClick.AddListener(Hide);

			_instantiated = Instantiate(objectToInstantiate, transform, true);
			
			gameObject.SetActive(true);
		}

		public void Hide()
		{
			Destroy(_instantiated.gameObject);
			gameObject.SetActive(false);
		}
	}
}
