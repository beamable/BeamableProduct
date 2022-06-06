using Beamable.EasyFeatures.Components;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class OverlayedLobbySettingsWindow : MonoBehaviour, IOverlayComponent
	{
		public TMP_InputField _nameField;
		public TMP_InputField _descriptionField;
		
		public Button CancelButton;
		public Button ConfirmButton;

		public void Show(string name, string description, Action<string, string> confirmAction, Action closeAction)
		{
			_nameField.SetTextWithoutNotify(name);
			_descriptionField.SetTextWithoutNotify(description);
			
			CancelButton.onClick.ReplaceOrAddListener(closeAction.Invoke);
			ConfirmButton.onClick.ReplaceOrAddListener(() =>
			{
				confirmAction?.Invoke(_nameField.text, _descriptionField.text);
			});

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
