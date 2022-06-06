using Beamable.Common;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
			int? CurrentlySelectedPlayerIndex { get; set; }
			bool IsVisible { get; }
			bool IsPlayerAdmin { get; }
			bool IsPlayerReady { get; }
			bool IsServerReady { get; }
			bool IsMatchStarting { get; }
			Promise LeaveLobby();
			void SetPlayerReady(bool value);
			bool SetCurrentSelectedPlayer(int slotIndex);
			void UpdateLobby(string name, string description);
		}

		[Header("View Configuration")]
		public int EnrichOrder;

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

		[Header("Callbacks")]
		public UnityEvent OnAdminLeaveLobbyRequestSent;
		public UnityEvent OnPlayerLeaveLobbyRequestSent;
		public UnityEvent OnLobbyLeft;
		public UnityEvent OnPlayerCardClicked;
		public UnityEvent OnSettingButtonClicked;
		
		protected IDependencies System;

		public int GetEnrichOrder() => EnrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			var ctx = managedPlayers.GetSinglePlayerContext();
			System = ctx.ServiceProvider.GetService<IDependencies>();

			gameObject.SetActive(System.IsVisible);

			if (!System.IsVisible)
			{
				return;
			}

			Name.text = System.Name;
			Counter.text = $"{System.CurrentPlayers}/{System.MaxPlayers}";

			// Buttons' callbacks
			SettingsButton.onClick.ReplaceOrAddListener(SettingsButtonClicked);
			ReadyButton.onClick.ReplaceOrAddListener(ReadyButtonClicked);
			WaitingButton.onClick.ReplaceOrAddListener(WaitingButtonClicked);
			StartButton.onClick.ReplaceOrAddListener(StartButtonClicked);
			LeaveButton.onClick.ReplaceOrAddListener(LeaveButtonClicked);
			BackButton.onClick.ReplaceOrAddListener(LeaveButtonClicked);

			// Buttons' visibility
			SettingsButton.gameObject.SetActive(System.IsPlayerAdmin);
			ReadyButton.gameObject.SetActive(!System.IsPlayerReady);
			WaitingButton.gameObject.SetActive(System.IsPlayerReady);
			StartButton.gameObject.SetActive(System.IsPlayerAdmin);

			// Buttons' interactivity
			StartButton.interactable = System.IsServerReady;
			BackButton.interactable = !System.IsMatchStarting;
			WaitingButton.interactable = !System.IsMatchStarting;
			LeaveButton.interactable = !System.IsMatchStarting;

			LobbySlotsList.ClearPooledRankedEntries();
			LobbySlotsList.Setup(System.SlotsData, System.IsPlayerAdmin, OnAdminButtonClicked);
			LobbySlotsList.RebuildPooledLobbiesEntries();
		}

		private void OnAdminButtonClicked(int slotIndex)
		{
			if (!System.IsPlayerAdmin)
			{
				return;
			}
			
			if (System.SetCurrentSelectedPlayer(slotIndex))
			{
				OnPlayerCardClicked?.Invoke();
			}
		}

		private void SettingsButtonClicked()
		{
			if (!System.IsPlayerAdmin)
			{
				return;
			}
			
			OnSettingButtonClicked?.Invoke();
		}

		private void ReadyButtonClicked()
		{
			System.SetPlayerReady(true);
		}

		private void WaitingButtonClicked()
		{
			System.SetPlayerReady(false);
		}

		private void StartButtonClicked() { }

		private async void LeaveButtonClicked()
		{
			if (System.IsPlayerAdmin)
			{
				OnAdminLeaveLobbyRequestSent?.Invoke();
			}
			else
			{
				OnPlayerLeaveLobbyRequestSent?.Invoke();
				await System.LeaveLobby();
				OnLobbyLeft?.Invoke();
			}
		}
	}
}
