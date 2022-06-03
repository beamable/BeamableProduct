using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicParty
{
	public class JoinPartyView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			string PartyId { get; set; }
			bool IsVisible { get; set; }
			bool ValidateJoinButton();
		}

		[SerializeField] private int _enrichOrder;

		[SerializeField] private TMP_InputField _partyIdInputField;
		[SerializeField] private Button _backButton;
		[SerializeField] private Button _joinButton;
		[SerializeField] private Button _cancelButton;
		
		private IDependencies _system;

		public int GetEnrichOrder() => _enrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			var ctx = managedPlayers.GetSinglePlayerContext();
			_system = ctx.ServiceProvider.GetService<IDependencies>();
			
			gameObject.SetActive(_system.IsVisible);
			if (!_system.IsVisible)
			{
				return;
			}
			
			OnPartyIdValueChanged(_partyIdInputField.text);
			
			_partyIdInputField.onValueChanged.ReplaceOrAddListener(OnPartyIdValueChanged);
			_joinButton.onClick.ReplaceOrAddListener(OnJoinButtonClicked);
			_backButton.onClick.ReplaceOrAddListener(OnBackButtonClicked);
			_cancelButton.onClick.ReplaceOrAddListener(OnCancelButtonClicked);
		}

		private void OnPartyIdValueChanged(string value)
		{
			_system.PartyId = value;
			_joinButton.interactable = _system.ValidateJoinButton();
		}

		private void OnCancelButtonClicked()
		{
			throw new System.NotImplementedException();
		}

		private void OnBackButtonClicked()
		{
			throw new System.NotImplementedException();
		}

		private void OnJoinButtonClicked()
		{
			throw new System.NotImplementedException();
		}
	}
}
