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
			int MaxPlayers { get; }
			int CurrentPlayers { get; }
			bool IsPlayerAdmin { get; }
			bool IsPlayerReady { get; }
			bool IsMatchStarting { get; set; }
			bool IsServerReady();
			void SetPlayerReady(bool value);
		}
		
		[Header("View Configuration")]
		public int EnrichOrder;
		
		[Header("Components")]
		public TextMeshProUGUI Counter;
		public MatchmakingSlotsListPresenter MatchmakingSlotsList;
		public Button StartButton;
		public Button ReadyButton;
		public Button NotReadyButton;
		public BussElement StartButtonBussElement;
		
		[Header("Callbacks")]
		public UnityEvent OnStartMatchRequestSent;
		
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
			ReadyButton.onClick.ReplaceOrAddListener(ReadyButtonClicked);
			NotReadyButton.onClick.ReplaceOrAddListener(NotReadyButtonClicked);

			// Buttons' visibility
			StartButton.gameObject.SetActive(System.IsPlayerAdmin);
			ReadyButton.gameObject.SetActive(!System.IsPlayerReady);
			NotReadyButton.gameObject.SetActive(System.IsPlayerReady);
			
			ValidateStartButton();

			MatchmakingSlotsList.ClearPooledRankedEntries();
			MatchmakingSlotsList.Setup(System.SlotsData);
			MatchmakingSlotsList.RebuildPooledLobbiesEntries();
		}
		
		private void ValidateStartButton()
		{
			if (!System.IsPlayerAdmin)
			{
				return;
			}
			
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
		
		private void ReadyButtonClicked()
		{
			if (System.IsMatchStarting)
			{
				return;
			}
			
			System.SetPlayerReady(true);
		}

		private void NotReadyButtonClicked()
		{
			if (System.IsMatchStarting)
			{
				return;
			}
			
			System.SetPlayerReady(false);
		}

		private void StartButtonClicked()
		{
			if (!System.IsPlayerAdmin || System.IsMatchStarting)
			{
				return;
			}

			System.IsMatchStarting = true;

			try
			{
				OnStartMatchRequestSent?.Invoke();
			}
			catch (Exception e)
			{
				OnError?.Invoke(e.Message);
			}
		}
	}
}
