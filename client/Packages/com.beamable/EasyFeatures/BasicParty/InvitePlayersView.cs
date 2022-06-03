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
			Party Party { get; set; }
			bool IsVisible { get; set; }
			List<PartySlotPresenter.ViewData> Players { get; set; }
		}

		public PartyFeatureControl FeatureControl;
		[SerializeField] private int _enrichOrder;

		[SerializeField] private TextMeshProUGUI _titleText;
		[SerializeField] private PlayersListPresenter _partyList;
		[SerializeField] private Button _settingsButton;
		[SerializeField] private Button _backButton;
		[SerializeField] private Button _createButton;
		
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

			_titleText.text = ctx.PlayerId.ToString();
			
			// set callbacks
			_settingsButton.onClick.ReplaceOrAddListener(OnSettingsButtonClicked);
			_backButton.onClick.ReplaceOrAddListener(OnBackButtonClicked);
			_createButton.onClick.ReplaceOrAddListener(OnCreateButtonClicked);
			
			_partyList.Setup(_system.Players, OnPlayerInvited, null, null, null);
		}

		private void OnPlayerInvited(string id)
		{
			throw new System.NotImplementedException();
		}

		private void OnCreateButtonClicked()
		{
			throw new System.NotImplementedException();
		}

		private void OnBackButtonClicked()
		{
			if (_system.Party != null)
			{
				FeatureControl.OpenPartyView(_system.Party);
			}
		}

		private void OnSettingsButtonClicked()
		{
			throw new System.NotImplementedException();
		}
	}
}
