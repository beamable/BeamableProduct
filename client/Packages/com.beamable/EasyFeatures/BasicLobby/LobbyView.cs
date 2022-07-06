using Beamable.Common;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class LobbyView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			List<LobbySlotPresenter.ViewData> SlotsData { get; }
			string Id { get; }
			string Name { get; }
			string Description { get; }
			int MaxPlayers { get; }
			int CurrentPlayers { get; }
			bool IsVisible { get; }
			bool IsPlayerAdmin { get; }
			bool IsPlayerReady { get; }
			bool IsServerReady { get; }
			bool IsMatchStarting { get; }
			Promise LeaveLobby();
		}

		[Header("View Configuration")]
		public int EnrichOrder;

		public LobbyFeatureControl FeatureControl;

		[Header("Components")]
		public TextMeshProUGUI Name;

		public TextMeshProUGUI Counter;
		public LobbySlotsListPresenter LobbySlotsList;
		public Button SettingsButton;

		public Button BackButton; 
		public Button ReadyButton; 
		public Button WaitingButton;
		public Button StartButton;
		public Button LeaveButton;

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

			Name.text = _system.Name;
			Counter.text = $"{_system.CurrentPlayers}/{_system.MaxPlayers}";

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
			LobbySlotsList.Setup(_system.SlotsData, _system.IsPlayerAdmin, OnReadyButtonClicked,
			                     OnNotReadyButtonClicked, OnAdminButtonClicked);
			LobbySlotsList.RebuildPooledLobbiesEntries();
		}

		private void OnReadyButtonClicked(int slotIndex) { }

		private void OnNotReadyButtonClicked(int slotIndex) { }

		private void OnAdminButtonClicked(int slotIndex) { }

		private void SettingsButtonClicked() { }

		private void ReadyButtonClicked() { }

		private void WaitingButtonClicked() { }

		private void StartButtonClicked() { }

		private void LeaveButtonClicked()
		{
			async void LeaveLobby()
			{
				FeatureControl.ShowOverlayedLabel("Leaving lobby...");
				await _system.LeaveLobby();
				FeatureControl.HideOverlay();
				FeatureControl.OpenJoinLobbyView();
			}

			if (_system.IsPlayerAdmin)
			{
				FeatureControl.ShowConfirmWindow("Leaving lobby",
				                                 "After leaving lobby it will be closed because You are an admin. Are You sure?",
				                                 LeaveLobby);
			}
			else
			{
				LeaveLobby();
			}
		}
	}
}
