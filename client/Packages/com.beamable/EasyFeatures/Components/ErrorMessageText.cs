using TMPro;
using UnityEngine;

namespace Beamable.EasyFeatures.Components
{
	public class ErrorMessageText : MonoBehaviour
	{
		public TextMeshProUGUI Text;

		public void SetErrorMessage(string error)
		{
			gameObject.SetActive(!string.IsNullOrWhiteSpace(error));
			Text.text = error;
		}
	}
}
