using TMPro;
using UnityEngine;
using Button = UnityEngine.UI.Button;
using Toggle = UnityEngine.UI.Toggle;

namespace Beamable.EasyFeatures.BasicParty
{
	public class CreatePartyView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			bool IsVisible { get; set; }
		}

		[SerializeField] private int _enrichOrder;

		[Header("Components")]
		[SerializeField] private TMP_InputField _maxPlayersTextField;
		[SerializeField] private Toggle _publicAccessToggle;
		[SerializeField] private Button _nextButton;
		[SerializeField] private Button _cancelButton;
		[SerializeField] private Button _backButton;

		private PartyAccess _access;

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
			
			PartyAccessChanged(_publicAccessToggle.isOn);
			
			// set callbacks
			_publicAccessToggle.onValueChanged.ReplaceOrAddListener(PartyAccessChanged);
			_nextButton.onClick.ReplaceOrAddListener(OnNextButtonClicked);
			_cancelButton.onClick.ReplaceOrAddListener(OnCancelButtonClicked);
			_backButton.onClick.ReplaceOrAddListener(OnBackButtonClicked);
		}

		private void OnBackButtonClicked()
		{
			throw new System.NotImplementedException();
		}
		
		private void OnCancelButtonClicked()
		{
			
		}

		private void OnNextButtonClicked()
		{
			if (!int.TryParse(_maxPlayersTextField.text, out int maxPlayers))
			{
				return;
			}
		}
		
		private void PartyAccessChanged(bool isPublic)
		{
			_access = isPublic ? PartyAccess.Public : PartyAccess.Private;
		}
	}
}
