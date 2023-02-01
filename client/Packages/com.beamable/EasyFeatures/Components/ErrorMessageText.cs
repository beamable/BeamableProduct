using TMPro;
using UnityEngine;

namespace Beamable.EasyFeatures.Components
{
	public class ErrorMessageText : MonoBehaviour
	{
		[SerializeField]
		protected TextMeshProUGUI Text;

		/// <summary>
		/// Sets an error message and displays based on the content. Passing an empty string is an equivalent of <see cref="HideMessage"/>
		/// </summary>
		public void SetErrorMessage(string error)
		{
			gameObject.SetActive(!string.IsNullOrWhiteSpace(error));
			Text.text = error;
		}

		public void HideMessage()
		{
			SetErrorMessage(string.Empty);
		}
	}
}
