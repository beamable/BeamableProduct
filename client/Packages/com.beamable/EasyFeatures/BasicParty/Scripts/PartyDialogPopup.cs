using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicParty
{
	public class PartyDialogPopup : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI _messageText;
		[SerializeField] private Button _confirmButton;
		[SerializeField] private Button _cancelButton;

		public void Setup(string message, UnityAction onConfirm, UnityAction onCancel)
		{
			_messageText.text = message;
			_confirmButton.onClick.AddListener(onConfirm);
			_cancelButton.onClick.AddListener(onCancel);
		}
	}
}
