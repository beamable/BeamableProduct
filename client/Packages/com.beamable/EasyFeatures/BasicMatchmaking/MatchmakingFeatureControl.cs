using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.EasyFeatures.Basicmatchmaking;
using Beamable.EasyFeatures.Components;
using Beamable.Experimental.Api.Matchmaking;
using EasyFeatures.BasicMatchmaking;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Beamable.EasyFeatures.BasicMatchmaking
{
	[BeamContextSystem]
	public class MatchmakingFeatureControl : MonoBehaviour, IBeamableFeatureControl, IOverlayController
	{
		protected enum View
		{
			StartMatchmaking,
			MatchmakingRoom
		}
		
		[Header("Feature Control"), SerializeField]
		private bool _runOnEnable = true;
		public BeamableViewGroup ViewGroup;
		public OverlaysController OverlaysController;
		
		[Header("Components")]
		public GameObject LoadingIndicator;
		
		[Header("Fast-Path Configuration")]
		public List<SimGameTypeRef> GameTypesRefs;
		public int TimeoutSeconds;
		
		public BeamContext BeamContext;

		protected View CurrentView = View.StartMatchmaking;
		protected StartMatchmakingPlayerSystem StartMatchmakingPlayerSystem;
		protected MatchmakingRoomPlayerSystem MatchmakingRoomPlayerSystem;
		
		public bool RunOnEnable { get => _runOnEnable; set => _runOnEnable = value; }

		public IEnumerable<BeamableViewGroup> ManagedViewGroups
		{
			get => new[] {ViewGroup};
			set => ViewGroup = value.FirstOrDefault();
		}
		
		public List<SimGameType> GameTypes { get; set; }
		
		[RegisterBeamableDependencies(Constants.SYSTEM_DEPENDENCY_ORDER)]
		public static void RegisterDefaultViewDeps(IDependencyBuilder builder)
		{
			builder.SetupUnderlyingSystemSingleton<StartMatchmakingPlayerSystem, StartMatchmakingView.IDependencies>();
			builder.SetupUnderlyingSystemSingleton<MatchmakingRoomPlayerSystem, MatchmakingRoomView.IDependencies>();
		}
		
		public void OnEnable()
		{
			ViewGroup.RebuildManagedViews();

			if (!RunOnEnable)
			{
				return;
			}

			Run();
		}

		public async void Run()
		{
			LoadingIndicator.SetActive(true);

			// Ensures the player contexts this view is configured to use are ready (frictionless login flow completed). 
			await ViewGroup.RebuildPlayerContexts(ViewGroup.AllPlayerCodes);

			BeamContext = ViewGroup.AllPlayerContexts[0];
			await BeamContext.OnReady;

			StartMatchmakingPlayerSystem = BeamContext.ServiceProvider.GetService<StartMatchmakingPlayerSystem>();
			MatchmakingRoomPlayerSystem = BeamContext.ServiceProvider.GetService<MatchmakingRoomPlayerSystem>();

			GameTypes = await FetchGameTypes();

			StartMatchmakingPlayerSystem.Setup(GameTypes, TimeoutSeconds);
			
			StartMatchmakingView startMatchmakingView = ViewGroup.ManagedViews.OfType<StartMatchmakingView>().First();
			startMatchmakingView.OnError = ShowErrorWindow;
			
			MatchmakingRoomView createLobbyView = ViewGroup.ManagedViews.OfType<MatchmakingRoomView>().First();
			createLobbyView.OnError = ShowErrorWindow;

			OpenView(CurrentView);
		}

		private async void OpenView(View newView)
		{
			CurrentView = newView;
			UpdateVisibility();
			await ViewGroup.Enrich();
			LoadingIndicator.SetActive(false);
		}

		private void UpdateVisibility()
		{
			StartMatchmakingPlayerSystem.IsVisible = CurrentView == View.StartMatchmaking;
			MatchmakingRoomPlayerSystem.IsVisible = CurrentView == View.MatchmakingRoom;
		}
		
		private async Promise<List<SimGameType>> FetchGameTypes()
		{
			Assert.IsTrue(GameTypesRefs.Count > 0, "Game types count configured in inspector must be greater than 0");

			List<SimGameType> gameTypes = new List<SimGameType>();

			foreach (SimGameTypeRef simGameTypeRef in GameTypesRefs)
			{
				SimGameType simGameType = await simGameTypeRef.Resolve();
				gameTypes.Add(simGameType);
			}

			return gameTypes;
		}


		#region IOverlayController

		public void HideOverlay()
		{
			OverlaysController.HideOverlay();
		}

		public void ShowOverlayedLabel(string label)
		{
			OverlaysController.ShowLabel(label);
		}

		public void ShowErrorWindow(string message)
		{
			OverlaysController.ShowError(message);
		}

		public void ShowConfirmWindow(string label, string message, Action confirmAction)
		{
			OverlaysController.ShowConfirm(label, message, confirmAction);
		}

		#endregion
		
		#region StartMatchmaking callback

		public void StartMatchmakingRequestSent()
		{
			ShowOverlayedLabel("Searching...");
			StartMatchmakingPlayerSystem.OnStateChanged += OnMatchmakingStateChanged;
		}

		public void StartMatchmakingResponseReceived()
		{
			
		}

		private void OnMatchmakingStateChanged(MatchmakingState currentState)
		{
			switch (currentState)
			{
				case MatchmakingState.Searching:
					ShowOverlayedLabel("Searching...");
					break;
				case MatchmakingState.Ready:
					HideOverlay();
					StartMatchmakingPlayerSystem.OnStateChanged -= OnMatchmakingStateChanged;
					break;
				case MatchmakingState.Timeout:
					ShowErrorWindow("Timeout");
					StartMatchmakingPlayerSystem.OnStateChanged -= OnMatchmakingStateChanged;
					break;
				case MatchmakingState.Cancelled:
					HideOverlay();
					StartMatchmakingPlayerSystem.OnStateChanged -= OnMatchmakingStateChanged;
					break;
			}
		}

		#endregion
	}
}
