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

		public void Setup(Sprite avatarSprite,
		                  string playerName,
		                  UnityAction onAcceptButton,
		                  UnityAction onAskToLeaveButton,
		                  UnityAction onPromoteButton)
		{
			_avatarImage.sprite = avatarSprite;
			_playerNameText.text = playerName;
			_acceptButton.onClick.AddListener(onAcceptButton);
			_askToLeaveButton.onClick.AddListener(onAskToLeaveButton);
			_promoteButton.onClick.AddListener(onPromoteButton);
		}

		public void ToggleExpand()
		{
			_expandableButtons.SetActive(!_expandableButtons.activeSelf);
		}
	}
}
