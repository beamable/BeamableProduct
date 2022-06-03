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
		[SerializeField] private Image _avatarImage;
		[SerializeField] private TextMeshProUGUI _playerNameText;
		[SerializeField] private Button _acceptButton;
		[SerializeField] private Button _askToLeaveButton;
		[SerializeField] private Button _promoteButton;
		[SerializeField] private Button _addMemberButton;
		[SerializeField] private GameObject _expandableButtons;
		[SerializeField] private GameObject _occupiedSlotGroup;

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
			_occupiedSlotGroup.SetActive(isSlotOccupied);
			_addMemberButton.gameObject.SetActive(!isSlotOccupied);
			
			_avatarImage.sprite = viewData.Avatar;
			_playerNameText.text = viewData.PlayerId;
			_acceptButton.onClick.ReplaceOrAddListener(() => onAcceptButton(viewData.PlayerId));
			_askToLeaveButton.onClick.ReplaceOrAddListener(() => onAskToLeaveButton(viewData.PlayerId));
			_promoteButton.onClick.ReplaceOrAddListener(() => onPromoteButton(viewData.PlayerId));
			_addMemberButton.onClick.ReplaceOrAddListener(() => onAddMemberButton());
		}

		public void ToggleExpand()
		{
			_expandableButtons.SetActive(!_expandableButtons.activeSelf);
		}
	}
}
