using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.Components
{
	public class OverlayedModalWindow : MonoBehaviour, IOverlayComponent
	{
		public enum Mode
		{
			Default,
			Confirm,
		}

		[Header("Components")]
		public TextMeshProUGUI Label;
		public TextMeshProUGUI Content;
		public Button CancelButton;
		public Button ConfirmButton;
		
		public void Show(string label, string content, Action confirmAction, Mode mode = Mode.Default)
		{
			Label.text = label;
			Content.text = content;

			CancelButton.onClick.ReplaceOrAddListener(Hide);
			ConfirmButton.onClick.ReplaceOrAddListener(confirmAction.Invoke);

			if (mode == Mode.Default)
			{
				CancelButton.gameObject.SetActive(false);
			}

			gameObject.SetActive(true);
		}

		public void Hide()
		{
			gameObject.SetActive(false);
			CancelButton.onClick.RemoveAllListeners();
			ConfirmButton.onClick.RemoveAllListeners();
		}
	}
}
