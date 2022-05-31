using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicParty
{
	public class JoinPartyView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			bool IsVisible { get; set; }
		}

		[SerializeField] private int _enrichOrder;

		[SerializeField] private TMP_InputField _partyIdInputField;
		[SerializeField] private Button _backButton;
		[SerializeField] private Button _joinButton;
		[SerializeField] private Button _cancelButton;

		public int GetEnrichOrder() => _enrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			var ctx = managedPlayers.GetSinglePlayerContext();
			var system = ctx.ServiceProvider.GetService<IDependencies>();
			
			gameObject.SetActive(system.IsVisible);
			if (!system.IsVisible)
			{
				return;
			}
			
			_joinButton.onClick.ReplaceOrAddListener(OnJoinButtonClicked);
			_backButton.onClick.ReplaceOrAddListener(OnBackButtonClicked);
			_cancelButton.onClick.ReplaceOrAddListener(OnCancelButtonClicked);
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
