using Beamable.UI.Scripts;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicParty
{
	public class PartySlotPresenter : MonoBehaviour
	{
		public RectTransform RectTransform;
		public Image AvatarImage;
		public TextMeshProUGUI PlayerNameText;
		public Button AcceptButton;
		public Button AskToLeaveButton;
		public Button PromoteButton;
		public Button AddMemberButton;
		public GameObject ExpandableButtons;
		public GameObject OccupiedSlotGroup;

		protected PlayersListPresenter ListPresenter;
		protected PoolData Item;
		protected bool IsInviteList;

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
		
		public void Setup(PoolData item,
		                  PlayersListPresenter listPresenter,
		                  bool isInviteList,
		                  Action<string> onAcceptButton,
		                  Action<string> onAskToLeaveButton,
		                  Action<string> onPromoteButton,
		                  Action onAddMemberButton)
		{
			bool isSlotOccupied = !string.IsNullOrWhiteSpace(item.ViewData.PlayerId);
			OccupiedSlotGroup.SetActive(isSlotOccupied);
			AddMemberButton.gameObject.SetActive(!isSlotOccupied);
			
			AvatarImage.sprite = item.ViewData.Avatar;
			PlayerNameText.text = item.ViewData.PlayerId;
			AcceptButton.onClick.ReplaceOrAddListener(() => onAcceptButton(item.ViewData.PlayerId));
			AskToLeaveButton.onClick.ReplaceOrAddListener(() => onAskToLeaveButton(item.ViewData.PlayerId));
			PromoteButton.onClick.ReplaceOrAddListener(() => onPromoteButton(item.ViewData.PlayerId));
			AddMemberButton.onClick.ReplaceOrAddListener(() => onAddMemberButton());

			ListPresenter = listPresenter;
			Item = item;
			IsInviteList = isInviteList;
		}

		public void ToggleExpand()
		{
			if (IsInviteList)
			{
				return;
			}
			
			ExpandableButtons.SetActive(!ExpandableButtons.activeSelf);
			StartCoroutine(UpdateItemHeight());
		}

		protected IEnumerator UpdateItemHeight()
		{
			yield return new WaitForEndOfFrame();
			Item.Height = RectTransform.sizeDelta.y;
			ListPresenter.UpdateContent();
		}
	}
}
