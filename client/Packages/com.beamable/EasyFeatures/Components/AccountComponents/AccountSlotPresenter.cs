using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Presence;
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
		private const string SELECTED_CLASS = "selected";

		public float AuthIconSize = 45;
		
		[Space]
		public Image AvatarImage;
		public Image DefaultAvatarImage;
		public TextMeshProUGUI UsernameText;
		public TextMeshProUGUI DescriptionText;
		public TextMeshProUGUI StatusText;
		public Button ConfirmButton;
		public TextMeshProUGUI ConfirmButtonText;
		public Button MainButton;
		public Toggle MainToggle;
		public BussElement ToggleBussElement;
		public GameObject AcceptCancelButtons;
		public Button AcceptButton;
		public Button CancelButton;
		public BussElement PresenceDotBussElement;
		public Transform LinkedAuthsRoot;
		public AuthMethodButton AuthMethodButtonPrefab;
		
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
			public bool IsCurrentPlayer;
			public Sprite Avatar;
			public bool HasEmail;
			public AuthThirdParty[] ThirdParties;
			public PlayerPresence Presence;
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
			MainButton.gameObject.SetActive(true);
			MainButton.onClick.ReplaceOrAddListener(() => onEntryPressed?.Invoke(item.ViewData.PlayerId));
			MainToggle.gameObject.SetActive(false);
			AcceptCancelButtons.SetActive(false);
		}

		public void Setup(PoolData item, Action<long> onEntryPressed, Action<long> onCancelPressed, Action<long> onAcceptPressed)
		{
			SetViewData(item.ViewData);
			
			ConfirmButton.gameObject.SetActive(false);
			MainButton.gameObject.SetActive(true);
			MainButton.onClick.ReplaceOrAddListener(() => onEntryPressed?.Invoke(item.ViewData.PlayerId));
			MainToggle.gameObject.SetActive(false);
			AcceptCancelButtons.SetActive(true);
			AcceptButton.onClick.ReplaceOrAddListener(() => onAcceptPressed?.Invoke(item.ViewData.PlayerId));
			CancelButton.onClick.ReplaceOrAddListener(() => onCancelPressed?.Invoke(item.ViewData.PlayerId));
		}
		
		public void SetupAsToggle(PoolData item, ToggleGroup group, Action<long> onEntrySelected)
		{
			SetViewData(item.ViewData);
			
			ConfirmButton.gameObject.SetActive(false);
			MainButton.gameObject.SetActive(false);
			MainToggle.gameObject.SetActive(true);
			MainToggle.group = group;
			MainToggle.onValueChanged.ReplaceOrAddListener(selected =>
			{
				ToggleBussElement.SetClass(SELECTED_CLASS, selected);
				
				if (selected)
					onEntrySelected?.Invoke(item.ViewData.PlayerId);
			});
			AcceptCancelButtons.SetActive(false);
		}

		private void SetViewData(ViewData viewData)
		{
			AvatarImage.sprite = viewData.Avatar;
			AvatarImage.color = viewData.Avatar == null ? Color.clear : Color.white;
			DefaultAvatarImage.enabled = viewData.Avatar == null;
			UsernameText.text = viewData.PlayerName;
			DescriptionText.text = viewData.Description;
			StatusText.gameObject.SetActive(viewData.Presence != null);

			// clear old icon instances
			foreach (Transform child in LinkedAuthsRoot)
			{
				Destroy(child.gameObject);
			}
			
			// show linked auths icons
			if (viewData.HasEmail)
				Instantiate(AuthMethodButtonPrefab, LinkedAuthsRoot).SetupEmail(true, false, AuthIconSize);

			if (viewData.ThirdParties != null)
			{
				foreach (var thirdParty in viewData.ThirdParties)
				{
					Instantiate(AuthMethodButtonPrefab, LinkedAuthsRoot).SetupThirdParty(thirdParty, true, false, AuthIconSize);
				}
			}
			
			// update presence status
			if (viewData.Presence != null)
			{
				CanvasGroup.alpha = viewData.Presence.online ? OnlineAlpha : OfflineAlpha;

				StatusText.text = viewData.Presence.GetName(viewData.IsCurrentPlayer);
				viewData.Presence.SetPresenceDotClasses(PresenceDotBussElement, viewData.IsCurrentPlayer);
			}
		}
	}
}
