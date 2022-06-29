using Beamable.Common;
using System;
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
			bool IsMatchStarting { get; }
			Promise LeaveLobby();
			void SetPlayerReady(bool value);
			void SetCurrentSelectedPlayer(int slotIndex);
			void UpdateLobby(string name, string description, string host);
			Promise StartMatch();
			bool IsServerReady();
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
		public Button NotReadyButton;
		public Button StartButton;
		public Button LeaveButton;

		[Header("Callbacks")]
		public UnityEvent OnRebuildRequested;

		public UnityEvent OnAdminLeaveLobbyRequestSent;
		public UnityEvent OnPlayerLeaveLobbyRequestSent;
		public UnityEvent OnLobbyLeft;
		public UnityEvent OnKickPlayerClicked;
		public UnityEvent OnPassLeadershipClicked;
		public UnityEvent OnSettingButtonClicked;
		public UnityEvent OnStartMatchRequestSent;
		public UnityEvent OnStartMatchResponseReceived;

		public Action<string> OnError;

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
			NotReadyButton.onClick.ReplaceOrAddListener(NotReadyButtonClicked);
			StartButton.onClick.ReplaceOrAddListener(StartButtonClicked);
			LeaveButton.onClick.ReplaceOrAddListener(LeaveButtonClicked);
			BackButton.onClick.ReplaceOrAddListener(LeaveButtonClicked);

			// Buttons' visibility
			SettingsButton.gameObject.SetActive(System.IsPlayerAdmin);
			ReadyButton.gameObject.SetActive(!System.IsPlayerReady);
			NotReadyButton.gameObject.SetActive(System.IsPlayerReady);
			StartButton.gameObject.SetActive(System.IsPlayerAdmin);

			// Buttons' interactivity
			StartButton.interactable = System.IsServerReady();
			BackButton.interactable = !System.IsMatchStarting;
			NotReadyButton.interactable = !System.IsMatchStarting;
			LeaveButton.interactable = !System.IsMatchStarting;

			LobbySlotsList.ClearPooledRankedEntries();
			LobbySlotsList.Setup(System.SlotsData, System.IsPlayerAdmin, OnAdminButtonClicked, OnKickButtonClicked,
			                     OnPassLeadershipButtonClicked);
			LobbySlotsList.RebuildPooledLobbiesEntries();
		}

		private void OnAdminButtonClicked(int slotIndex)
		{
			if (!System.IsPlayerAdmin)
			{
				return;
			}

			System.SetCurrentSelectedPlayer(slotIndex);
			OnRebuildRequested?.Invoke();
		}

		private void OnKickButtonClicked(int slotIndex)
		{
			if (!System.IsPlayerAdmin)
			{
				return;
			}

			OnKickPlayerClicked?.Invoke();
		}

		private void OnPassLeadershipButtonClicked(int slotIndex)
		{
			if (!System.IsPlayerAdmin)
			{
				return;
			}

			OnPassLeadershipClicked?.Invoke();
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

		private void NotReadyButtonClicked()
		{
			System.SetPlayerReady(false);
		}

		private async void StartButtonClicked()
		{
			if (!System.IsPlayerAdmin)
			{
				return;
			}

			try
			{
				OnStartMatchRequestSent?.Invoke();
				await System.StartMatch();
				OnStartMatchResponseReceived?.Invoke();
			}
			catch (Exception e)
			{
				OnError?.Invoke(e.Message);
			}
		}

		private async void LeaveButtonClicked()
		{
			if (System.IsPlayerAdmin)
			{
				OnAdminLeaveLobbyRequestSent?.Invoke();
			}
			else
			{
				try
				{
					OnPlayerLeaveLobbyRequestSent?.Invoke();
					await System.LeaveLobby();
					OnLobbyLeft?.Invoke();
				}
				catch (Exception e)
				{
					OnError?.Invoke(e.Message);
				}
			}
		}
	}
}
