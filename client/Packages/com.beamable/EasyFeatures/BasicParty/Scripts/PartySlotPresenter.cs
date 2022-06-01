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
		[SerializeField] private GameObject _expandableButtons;

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
		                  Action<string> onPromoteButton)
		{
			_avatarImage.sprite = viewData.Avatar;
			_playerNameText.text = viewData.PlayerId;
			_acceptButton.onClick.AddListener(() => onAcceptButton.Invoke(viewData.PlayerId));
			_askToLeaveButton.onClick.AddListener(() => onAskToLeaveButton.Invoke(viewData.PlayerId));
			_promoteButton.onClick.AddListener(() => onPromoteButton.Invoke(viewData.PlayerId));
		}

		public void ToggleExpand()
		{
			_expandableButtons.SetActive(!_expandableButtons.activeSelf);
		}
	}
}
