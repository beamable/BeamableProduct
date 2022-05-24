using Beamable.Common;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class InsideLobbyView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			LobbiesListEntryPresenter.Data LobbyData { get; }
			List<LobbySlotPresenter.Data> SlotsData { get; }
			bool IsVisible { get; }
			bool IsPlayerAdmin { get; }
			bool IsPlayerReady { get; }
			bool IsServerReady { get; }
			bool IsMatchStarting { get; }
			Promise ConfigureData();
		}
		
		[Header("View Configuration")]
		public int EnrichOrder;
		public LobbyFeatureControl FeatureControl;

		[Header("Components")]
		public TextMeshProUGUI Name;
		public TextMeshProUGUI Counter;
		public LobbySlotsListPresenter LobbySlotsList;
		public Button SettingsButton;
		public Button BackButton;		// Visible all the time, interactable while match is not starting (admin didn't clicked start)
		public Button ReadyButton;		// Visible when not ready
		public Button WaitingButton;	// Visible when ready, interactable while match is not starting (admin didn't clicked start)
		public Button StartButton;		// Visible when admin, interactable when everyone clicked Ready button
		public Button LeaveButton;		// Visible all the time, interactable while match is not starting (admin didn't clicked start)
		
		private IDependencies _system;

		public int GetEnrichOrder() => EnrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			var ctx = managedPlayers.GetSinglePlayerContext();
			_system = ctx.ServiceProvider.GetService<IDependencies>();
			
			gameObject.SetActive(_system.IsVisible);

			if (!_system.IsVisible)
			{
				return;
			}

			Name.text = _system.LobbyData.Name;
			Counter.text = $"{_system.LobbyData.CurrentPlayers}/{_system.LobbyData.MaxPlayers}";
			
			// Buttons' callbacks
			SettingsButton.onClick.ReplaceOrAddListener(SettingsButtonClicked);
			ReadyButton.onClick.ReplaceOrAddListener(ReadyButtonClicked);
			WaitingButton.onClick.ReplaceOrAddListener(WaitingButtonClicked);
			StartButton.onClick.ReplaceOrAddListener(StartButtonClicked);
			LeaveButton.onClick.ReplaceOrAddListener(LeaveButtonClicked);
			BackButton.onClick.ReplaceOrAddListener(LeaveButtonClicked);
			
			// Buttons' visibility
			SettingsButton.gameObject.SetActive(_system.IsPlayerAdmin);
			ReadyButton.gameObject.SetActive(!_system.IsPlayerReady);
			WaitingButton.gameObject.SetActive(_system.IsPlayerReady);
			StartButton.gameObject.SetActive(_system.IsPlayerAdmin);
			
			// Buttons' interactivity
			StartButton.interactable = _system.IsServerReady;
			BackButton.interactable = !_system.IsMatchStarting;
			WaitingButton.interactable = !_system.IsMatchStarting;
			LeaveButton.interactable = !_system.IsMatchStarting;
			
			LobbySlotsList.ClearPooledRankedEntries();
			LobbySlotsList.Setup(_system.SlotsData, _system.IsPlayerAdmin, OnReadyButtonClicked, OnNotReadyButtonClicked, OnAdminButtonClicked);
			LobbySlotsList.RebuildPooledLobbiesEntries();
		}

		private void OnReadyButtonClicked(int slotIndex)
		{
			
		}

		private void OnNotReadyButtonClicked(int slotIndex)
		{

		}

		private void OnAdminButtonClicked(int slotIndex)
		{

		}

		private void SettingsButtonClicked()
		{
			
		}

		private void ReadyButtonClicked()
		{
			
		}

		private void WaitingButtonClicked()
		{
			
		}

		private void StartButtonClicked()
		{
			
		}

		private void LeaveButtonClicked()
		{
			FeatureControl.OpenJoinLobbyView();
		}
	}
}
