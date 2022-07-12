using Beamable.Common;
using Beamable.UI.Buss;
using EasyFeatures.Components;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicMatchmaking
{
	public class MatchmakingRoomView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			List<MatchmakingSlotPresenter.ViewData> SlotsData { get; }
			bool IsVisible { get; set; }
			string Name { get; }
			int MaxPlayers { get; }
			int? CurrentlySelectedPlayerIndex { get; set; }
			int CurrentPlayers { get; }
			bool IsPlayerAdmin { get; }
			bool IsPlayerReady { get; }
			bool IsMatchStarting { get; }
			bool IsServerReady();
			void SetCurrentSelectedPlayer(int slotIndex);
			Promise LeaveMatch();
			Promise StartMatch();
		}
		
		[Header("View Configuration")]
		public int EnrichOrder;
		
		[Header("Components")]
		public TextMeshProUGUI Counter;
		public MatchmakingSlotsListPresenter MatchmakingSlotsList;
		public Button BackButton;
		public Button StartButton;
		public Button LeaveButton;
		
		public BussElement StartButtonBussElement;
		
		[Header("Callbacks")]
		public UnityEvent OnRebuildRequested;
		public UnityEvent OnAdminLeaveMatchRequestSent;
		public UnityEvent OnPlayerLeaveMatchRequestSent;
		public UnityEvent OnMatchLeft;
		public UnityEvent OnKickPlayerClicked;
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
			
			Counter.text = $"{System.CurrentPlayers}/{System.MaxPlayers}";
			
			// Buttons' callbacks
			StartButton.onClick.ReplaceOrAddListener(StartButtonClicked);
			LeaveButton.onClick.ReplaceOrAddListener(LeaveButtonClicked);
			BackButton.onClick.ReplaceOrAddListener(LeaveButtonClicked);

			// Buttons' visibility
			StartButton.gameObject.SetActive(System.IsPlayerAdmin);
			
			ValidateStartButton();

			MatchmakingSlotsList.ClearPooledRankedEntries();
			MatchmakingSlotsList.Setup(System.SlotsData, System.IsPlayerAdmin, OnAdminButtonClicked, OnKickButtonClicked);
			MatchmakingSlotsList.RebuildPooledLobbiesEntries();
		}
		
		private void ValidateStartButton()
		{
			bool buttonValid = System.IsPlayerAdmin && System.IsServerReady();
			StartButton.interactable = buttonValid;
			
			if (buttonValid)
			{
				StartButtonBussElement.SetButtonPrimary();
			}
			else
			{
				StartButtonBussElement.SetButtonDisabled();
			}
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
			if (!System.IsPlayerAdmin || System.IsMatchStarting)
			{
				return;
			}

			OnKickPlayerClicked?.Invoke();
		}

		private async void StartButtonClicked()
		{
			if (!System.IsPlayerAdmin || System.IsMatchStarting)
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
			if (System.IsMatchStarting)
			{
				return;
			}
			
			if (System.IsPlayerAdmin)
			{
				OnAdminLeaveMatchRequestSent?.Invoke();
			}
			else
			{
				try
				{
					OnPlayerLeaveMatchRequestSent?.Invoke();
					await System.LeaveMatch();
					OnMatchLeft?.Invoke();
				}
				catch (Exception e)
				{
					OnError?.Invoke(e.Message);
				}
			}
		}
	}
}
