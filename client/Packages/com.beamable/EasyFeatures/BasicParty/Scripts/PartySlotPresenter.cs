using Beamable.UI.Scripts;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicParty
{
	public class PartySlotPresenter : MonoBehaviour
	{
		public Image AvatarImage;
		public TextMeshProUGUI PlayerNameText;
		public Button AcceptButton;
		public Button AskToLeaveButton;
		public Button PromoteButton;
		public Button AddMemberButton;
		public GameObject ExpandableButtons;
		public GameObject OccupiedSlotGroup;

		public struct ViewData
		{
			public string PlayerId;
			public bool IsReady;
			public Sprite Avatar;
		}
		
		public class PoolData : PoolableScrollView.IItem
		{
			public ViewData ViewData { get; set; }
			public int Index { get; set; }
			public float Height { get; set; }
		}
		
		public void Setup(ViewData viewData,
		                  Action<string> onAcceptButton,
		                  Action<string> onAskToLeaveButton,
		                  Action<string> onPromoteButton,
		                  Action onAddMemberButton)
		{
			bool isSlotOccupied = !string.IsNullOrWhiteSpace(viewData.PlayerId);
			OccupiedSlotGroup.SetActive(isSlotOccupied);
			AddMemberButton.gameObject.SetActive(!isSlotOccupied);
			
			AvatarImage.sprite = viewData.Avatar;
			PlayerNameText.text = viewData.PlayerId;
			AcceptButton.onClick.ReplaceOrAddListener(() => onAcceptButton(viewData.PlayerId));
			AskToLeaveButton.onClick.ReplaceOrAddListener(() => onAskToLeaveButton(viewData.PlayerId));
			PromoteButton.onClick.ReplaceOrAddListener(() => onPromoteButton(viewData.PlayerId));
			AddMemberButton.onClick.ReplaceOrAddListener(() => onAddMemberButton());
		}

		public void ToggleExpand()
		{
			ExpandableButtons.SetActive(!ExpandableButtons.activeSelf);
		}
	}
}
