using Beamable.UI.Buss;
using Beamable.UI.Scripts;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.Components
{
	public class AccountSlotPresenter : MonoBehaviour
	{
		private const string ONLINE_CLASS = "online";
		private const string OFFLINE_CLASS = "offline";
		
		public Image AvatarImage;
		public TextMeshProUGUI UsernameText;
		public TextMeshProUGUI DescriptionText;
		public Button ConfirmButton;
		public TextMeshProUGUI ConfirmButtonText;
		public Button MainButton;
		public GameObject AcceptCancelButtons;
		public Button AcceptButton;
		public Button CancelButton;
		public BussElement PresenceDotBussElement;
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
			SetViewData(item.ViewData);
			
			ConfirmButton.onClick.ReplaceOrAddListener(() => onConfirmPressed?.Invoke(item.ViewData.PlayerId));
			ConfirmButton.gameObject.SetActive(onConfirmPressed != null);
			ConfirmButtonText.text = buttonText;
			MainButton.onClick.ReplaceOrAddListener(() => onEntryPressed?.Invoke(item.ViewData.PlayerId));
			AcceptCancelButtons.SetActive(false);
			
			SetOnlineState(true);
		}

		public void Setup(PoolData item, Action<long> onEntryPressed, Action<long> onCancelPressed, Action<long> onAcceptPressed)
		{
			SetViewData(item.ViewData);
			
			ConfirmButton.gameObject.SetActive(false);
			MainButton.onClick.ReplaceOrAddListener(() => onEntryPressed?.Invoke(item.ViewData.PlayerId));
			AcceptCancelButtons.SetActive(true);
			AcceptButton.onClick.ReplaceOrAddListener(() => onAcceptPressed?.Invoke(item.ViewData.PlayerId));
			CancelButton.onClick.ReplaceOrAddListener(() => onCancelPressed?.Invoke(item.ViewData.PlayerId));
			
			SetOnlineState(true);
		}

		private void SetViewData(ViewData viewData)
		{
			AvatarImage.sprite = viewData.Avatar;
			AvatarImage.color = viewData.Avatar == null ? Color.clear : Color.white;
			UsernameText.text = viewData.PlayerName;
			DescriptionText.text = viewData.Description;
		}

		public void SetOnlineState(bool online)
		{
			PresenceDotBussElement.AddClass(online ? ONLINE_CLASS : OFFLINE_CLASS);
			CanvasGroup.alpha = online ? OnlineAlpha : OfflineAlpha;
		}
	}
}
