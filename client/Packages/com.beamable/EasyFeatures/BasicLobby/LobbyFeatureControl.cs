using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.EasyFeatures.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Beamable.EasyFeatures.BasicLobby
{
	[BeamContextSystem]
	public class LobbyFeatureControl : MonoBehaviour, IBeamableFeatureControl, IOverlayController
	{
		protected enum View
		{
			MainMenu,
			CreateLobby,
			JoinLobby,
			InsideLobby
		}

		[Header("Feature Control")]
		[SerializeField] private bool _runOnEnable = true;
		public BeamableViewGroup ViewGroup;
		public LobbyOverlaysController OverlaysController;

		[Header("Components")]
		public GameObject LoadingIndicator;

		[Header("Fast-Path Configuration")]
		public List<SimGameTypeRef> GameTypesRefs;

		public BeamContext BeamContext;

		protected View CurrentView = View.MainMenu;
		protected MainLobbyPlayerSystem MainLobbyPlayerSystem;
		protected CreateLobbyPlayerSystem CreateLobbyPlayerSystem;
		protected LobbyPlayerSystem LobbyPlayerSystem;
		protected JoinLobbyPlayerSystem JoinLobbyPlayerSystem;

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
			builder.SetupUnderlyingSystemSingleton<MainLobbyPlayerSystem, MainLobbyView.IDependencies>();
			builder.SetupUnderlyingSystemSingleton<JoinLobbyPlayerSystem, JoinLobbyView.IDependencies>();
			builder.SetupUnderlyingSystemSingleton<CreateLobbyPlayerSystem, CreateLobbyView.IDependencies>();
			builder.SetupUnderlyingSystemSingleton<LobbyPlayerSystem, LobbyView.IDependencies>();
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

			MainLobbyPlayerSystem = BeamContext.ServiceProvider.GetService<MainLobbyPlayerSystem>();
			JoinLobbyPlayerSystem = BeamContext.ServiceProvider.GetService<JoinLobbyPlayerSystem>();
			CreateLobbyPlayerSystem = BeamContext.ServiceProvider.GetService<CreateLobbyPlayerSystem>();
			LobbyPlayerSystem = BeamContext.ServiceProvider.GetService<LobbyPlayerSystem>();

			GameTypes = await FetchGameTypes();

			JoinLobbyPlayerSystem.Setup(GameTypes);
			CreateLobbyPlayerSystem.Setup(GameTypes);

			JoinLobbyView joinLobbyView = ViewGroup.ManagedViews.OfType<JoinLobbyView>().First();
			joinLobbyView.OnError = ShowErrorWindow;
			
			CreateLobbyView createLobbyView = ViewGroup.ManagedViews.OfType<CreateLobbyView>().First();
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
			MainLobbyPlayerSystem.IsVisible = CurrentView == View.MainMenu;
			CreateLobbyPlayerSystem.IsVisible = CurrentView == View.CreateLobby;
			JoinLobbyPlayerSystem.IsVisible = CurrentView == View.JoinLobby;
			LobbyPlayerSystem.IsVisible = CurrentView == View.InsideLobby;
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

		public void OpenMainView()
		{
			OpenView(View.MainMenu);
		}

		public void OpenJoinLobbyView()
		{
			OpenView(View.JoinLobby);
		}

		public void OpenCreateLobbyView()
		{
			JoinLobbyPlayerSystem.HasInitialData = false;
			OpenView(View.CreateLobby);
		}

		public void OpenLobbyView()
		{
			if (BeamContext.Lobby == null)
			{
				return;
			}

			BeamContext.Lobby.OnUpdated -= OnLobbyUpdated;
			BeamContext.Lobby.OnUpdated += OnLobbyUpdated;

			LobbyPlayerSystem.RegisterLobbyPlayers(BeamContext.Lobby.State.players);
				
			OpenView(View.InsideLobby);
		}

		private async void OnLobbyUpdated()
		{
			if (BeamContext.Lobby.State.players == null)
			{
				return;
			}
			
			LobbyPlayerSystem.RegisterLobbyPlayers(BeamContext.Lobby.State.players);
			await ViewGroup.Enrich();
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

		#region CreateLobbyView callbacks

		public virtual void CreateLobbyRequestSent()
		{
			ShowOverlayedLabel("Creating lobby...");
		}
		
		public virtual void CreateLobbyRequestReceived()
		{
			HideOverlay();
			
			if (BeamContext.Lobby.State != null)
			{
				CreateLobbyPlayerSystem.ResetData();
				OpenLobbyView();
			}
		}

		#endregion

		#region JoinLobbyView callbacks

		public virtual void JoinLobbyRequestSent()
		{
			ShowOverlayedLabel("Joining lobby...");
		}

		public virtual void JoinLobbyRequestReceived()
		{
			HideOverlay();
			
			if (BeamContext.Lobby.State != null)
			{
				OpenLobbyView();
			}
		}

		public virtual void GetLobbiesRequestSent()
		{
			ShowOverlayedLabel("Getting lobbies...");
		}

		public virtual void GetLobbiesRequestReceived()
		{
			HideOverlay();
		}

		#endregion
		
		#region InsideLobbyView callbacks

		public void AdminLeaveLobbyRequestSent()
		{
			async void ConfirmAction()
			{
				ShowOverlayedLabel("Leaving lobby...");
				await LobbyPlayerSystem.LeaveLobby();
				LobbyLeft();
			}
			
			ShowConfirmWindow("Leaving lobby", "After leaving lobby it will be closed because You are an admin. Are You sure?", ConfirmAction);
		}

		public void PlayerLeaveLobbyRequestSent()
		{
			if (BeamContext.Lobby != null)
			{
				BeamContext.Lobby.OnUpdated -= OnLobbyUpdated;
			}
			
			ShowOverlayedLabel("Leaving lobby...");
		}

		public void LobbyLeft()
		{
			OpenJoinLobbyView();
			HideOverlay();
		}

		public void PlayerCardClicked()
		{
			if (LobbyPlayerSystem.CurrentlySelectedPlayerIndex == null)
			{
				return;
			}
			
			async void ConfirmAction()
			{
				ShowOverlayedLabel("Kicking player...");
				await LobbyPlayerSystem.KickPlayer();
				HideOverlay();
			}
			
			ShowConfirmWindow("Kick player", "Are You sure You want to kick this player?", ConfirmAction);
		}

		public void SettingsButtonClicked()
		{
			if (!LobbyPlayerSystem.IsPlayerAdmin)
			{
				return;
			}

			void ConfirmAction(string name, string description)
			{
				LobbyPlayerSystem.UpdateLobby(name, description);
			}
			
			OverlaysController.ShowLobbySettings(LobbyPlayerSystem.Name, LobbyPlayerSystem.Description, ConfirmAction);
		}
		
		#endregion
	}
}
