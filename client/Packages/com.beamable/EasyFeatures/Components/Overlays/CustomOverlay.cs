using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.Components
{
	public class CustomOverlay : MonoBehaviour, IOverlayComponent
	{
		public Button CloseButton;

		private GameObject _instantiated;
		
		public T Show<T>(T objectToInstantiate) where T : MonoBehaviour
		{
			CloseButton.onClick.RemoveAllListeners();
			CloseButton.onClick.AddListener(Hide);

			T instance = Instantiate(objectToInstantiate, transform, true);
			_instantiated = instance.gameObject;
			
			gameObject.SetActive(true);

			return instance;
		}

		public void Hide()
		{
			Destroy(_instantiated.gameObject);
			gameObject.SetActive(false);
		}
	}
}
