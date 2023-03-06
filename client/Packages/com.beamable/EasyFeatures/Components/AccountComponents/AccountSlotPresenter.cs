using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Presence;
using Beamable.UI.Buss;
using Beamable.UI.Scripts;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.Components
{
	public class AccountSlotPresenter : MonoBehaviour
	{
		private const string SELECTED_CLASS = "selected";

		public float AuthIconSize = 45;

		[Space]
		public Image BackgroundImage;
		public BussElement BackgroundBussElement;
		public Image AvatarImage;
		public Image DefaultAvatarImage;
		public TextMeshProUGUI UsernameText;
		public TextMeshProUGUI DescriptionText;
		public TextMeshProUGUI StatusText;
		public Button ConfirmButton;
		public TextMeshProUGUI ConfirmButtonText;
		public Image MainButton;
		public GameObject AcceptCancelButtons;
		public Button AcceptButton;
		public Button CancelButton;
		public Button DeleteButton;
		public SlidingPanel SlidingPanel;
		public BussElement PresenceDotBussElement;
		public Transform LinkedAuthsRoot;
		public AuthMethodButton AuthMethodButtonPrefab;
		
		[Range(0, 1)]
		public float OnlineAlpha = 1;
		[Range(0, 1)]
		public float OfflineAlpha = 0.5f;
		public CanvasGroup CanvasGroup;
		
		private Button _button;
		private Toggle _toggle;
		
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

		public void Setup(PoolData item, Action<long> onEntryPressed, Action<long> onConfirmPressed, string buttonText = "Confirm", Action<long> deleteAction = null)
		{
			SetViewData(item.ViewData);

			ConfirmButton.onClick.ReplaceOrAddListener(() => onConfirmPressed?.Invoke(item.ViewData.PlayerId));
			ConfirmButton.gameObject.SetActive(onConfirmPressed != null);
			ConfirmButtonText.text = buttonText;
			AcceptCancelButtons.SetActive(false);
			DeleteButton.onClick.ReplaceOrAddListener(() => deleteAction?.Invoke(item.ViewData.PlayerId));
			SlidingPanel.enabled = deleteAction != null;

			InitButton(() => onEntryPressed?.Invoke(item.ViewData.PlayerId));
		}

		public void Setup(PoolData item, Action<long> onEntryPressed, Action<long> onCancelPressed, Action<long> onAcceptPressed, Action<long> deleteAction = null)
		{
			SetViewData(item.ViewData);
			
			ConfirmButton.gameObject.SetActive(false);
			AcceptCancelButtons.SetActive(true);
			AcceptButton.onClick.ReplaceOrAddListener(() => onAcceptPressed?.Invoke(item.ViewData.PlayerId));
			CancelButton.onClick.ReplaceOrAddListener(() => onCancelPressed?.Invoke(item.ViewData.PlayerId));
			DeleteButton.onClick.ReplaceOrAddListener(() => deleteAction?.Invoke(item.ViewData.PlayerId));
			SlidingPanel.enabled = deleteAction != null;
			
			InitButton(() => onEntryPressed?.Invoke(item.ViewData.PlayerId));
		}
		
		public void SetupAsToggle(PoolData item, ToggleGroup group, Action<bool, long> onToggleSwitched, Action<long> deleteAction = null)
		{
			SetViewData(item.ViewData);
			
			ConfirmButton.gameObject.SetActive(false);
			AcceptCancelButtons.SetActive(false);
			DeleteButton.onClick.ReplaceOrAddListener(() => deleteAction?.Invoke(item.ViewData.PlayerId));
			SlidingPanel.enabled = deleteAction != null;
			
			InitToggle(group, selected =>
			{
				BackgroundBussElement.SetClass(SELECTED_CLASS, selected);
				onToggleSwitched?.Invoke(selected, item.ViewData.PlayerId);
			});
		}
		
		private void InitButton(UnityAction onPressedAction)
		{
			_button = MainButton.gameObject.GetComponent<Button>();
			if (_button == null)
			{
				_button = MainButton.gameObject.AddComponent<Button>();				
			}
			_button.targetGraphic = BackgroundImage;
			_button.onClick.ReplaceOrAddListener(onPressedAction);
		}
		
		private void InitToggle(ToggleGroup group, UnityAction<bool> onPressedAction)
		{
			_toggle = MainButton.gameObject.GetComponent<Toggle>();
			if (_toggle == null)
			{
				_toggle = MainButton.gameObject.AddComponent<Toggle>();	
			}
			_toggle.targetGraphic = BackgroundImage;
			_toggle.group = group;
			_toggle.onValueChanged.ReplaceOrAddListener(onPressedAction);
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
