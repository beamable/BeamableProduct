using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicParty
{
	public class InvitePlayersView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			bool IsVisible { get; set; }
			string PlayerName { get; set; }
			List<PartySlotPresenter.ViewData> Players { get; set; }
		}
		
		[SerializeField] private int _enrichOrder;

		[SerializeField] private TextMeshProUGUI _titleText;
		[SerializeField] private PlayersListPresenter _partyList;
		[SerializeField] private Button _settingsButton;
		[SerializeField] private Button _backButton;
		[SerializeField] private Button _createButton;

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

			_titleText.text = system.PlayerName;
			
			// set callbacks
			_settingsButton.onClick.ReplaceOrAddListener(OnSettingsButtonClicked);
			_backButton.onClick.ReplaceOrAddListener(OnBackButtonClicked);
			_createButton.onClick.ReplaceOrAddListener(OnCreateButtonClicked);
			
			_partyList.Setup(system.Players, OnPlayerAccepted, OnAskedToLeave, OnPromoted);
		}

		private void OnPromoted(string id)
		{
			throw new System.NotImplementedException();
		}

		private void OnAskedToLeave(string id)
		{
			throw new System.NotImplementedException();
		}

		private void OnPlayerAccepted(string id)
		{
			throw new System.NotImplementedException();
		}

		private void OnCreateButtonClicked()
		{
			throw new System.NotImplementedException();
		}

		private void OnBackButtonClicked()
		{
			throw new System.NotImplementedException();
		}

		private void OnSettingsButtonClicked()
		{
			throw new System.NotImplementedException();
		}
	}
}
