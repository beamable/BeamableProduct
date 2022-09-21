using Beamable.UI.Scripts;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicSocial
{
	public class FriendSlotPresenter : MonoBehaviour
	{
		public Image AvatarImage;
		public TextMeshProUGUI UsernameText;
		public TextMeshProUGUI DescriptionText;
		public Button ConfirmButton;
		public Button MainButton;
		public TextMeshProUGUI ConfirmButtonText;
		public Image PresenceDot;
		public Color OnlineColor = Color.green;
		public Color OfflineColor = Color.grey;
		[Range(0, 1)]
		public float OnlineAlpha = 1;
		[Range(0, 1)]
		public float OfflineAlpha = 0.5f;
		public CanvasGroup CanvasGroup;
		
		public struct ViewData
		{
			public long PlayerId;
			public string PlayerName;
			public string Description;
			public Sprite Avatar;
		}
		
		public class PoolData : PoolableScrollView.IItem
		{
			public ViewData ViewData { get; set; }
			public int Index { get; set; }
			public float Height { get; set; }
		}

		public void Setup(PoolData item, Action<long> onEntryPressed, Action<long> onConfirmPressed, string buttonText = "Confirm")
		{
			AvatarImage.sprite = item.ViewData.Avatar;
			UsernameText.text = item.ViewData.PlayerName;
			DescriptionText.text = item.ViewData.Description;
			
			ConfirmButton.onClick.ReplaceOrAddListener(() => onConfirmPressed?.Invoke(item.ViewData.PlayerId));
			ConfirmButton.gameObject.SetActive(onConfirmPressed != null);
			ConfirmButtonText.text = buttonText;
			MainButton.onClick.ReplaceOrAddListener(() => onEntryPressed?.Invoke(item.ViewData.PlayerId));
		}

		public void SetOnlineState(bool online)
		{
			PresenceDot.color = online ? OnlineColor : OfflineColor;
			CanvasGroup.alpha = online ? OnlineAlpha : OfflineAlpha;
		}
	}
}
