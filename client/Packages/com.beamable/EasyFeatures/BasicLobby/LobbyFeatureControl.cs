using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.EasyFeatures.Components;
using Beamable.Experimental.Api.Lobbies;
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
		private enum View
		{
			MainMenu,
			CreateLobby,
			JoinLobby,
			InsideLobby
		}

		[Header("Feature Control")]
		[SerializeField] private bool _runOnEnable = true;

		[SerializeField] private BeamableViewGroup _lobbyViewGroup;
		[SerializeField] private OverlaysController _overlaysController;

		[Header("Components")]
		[SerializeField] private GameObject _loadingIndicator;

		[Header("Fast-Path Configuration")]
		[SerializeField] private List<SimGameTypeRef> _gameTypes;

		private CreateLobbyPlayerSystem _createLobbyPlayerSystem;

		private View _currentView = View.MainMenu;
		private LobbyPlayerSystem _lobbyPlayerSystem;
		private JoinLobbyPlayerSystem _joinLobbyPlayerSystem;

		private MainLobbyPlayerSystem _mainLobbyPlayerSystem;
		private BeamContext _beamContext;

		public IEnumerable<BeamableViewGroup> ManagedViewGroups
		{
			get => new[] {_lobbyViewGroup};
			set => _lobbyViewGroup = value.FirstOrDefault();
		}

		public bool RunOnEnable { get => _runOnEnable; set => _runOnEnable = value; }

		public void OnEnable()
		{
			_lobbyViewGroup.RebuildManagedViews();

			if (!RunOnEnable)
			{
				return;
			}

			Run();
		}

		[RegisterBeamableDependencies(Constants.SYSTEM_DEPENDENCY_ORDER)]
		public static void RegisterDefaultViewDeps(IDependencyBuilder builder)
		{
			builder.SetupUnderlyingSystemSingleton<MainLobbyPlayerSystem, MainLobbyView.IDependencies>();
			builder.SetupUnderlyingSystemSingleton<JoinLobbyPlayerSystem, JoinLobbyView.IDependencies>();
			builder.SetupUnderlyingSystemSingleton<CreateLobbyPlayerSystem, CreateLobbyView.IDependencies>();
			builder.SetupUnderlyingSystemSingleton<LobbyPlayerSystem, LobbyView.IDependencies>();
		}

		public async void Run()
		{
			_loadingIndicator.SetActive(true);

			// Ensures the player contexts this view is configured to use are ready (frictionless login flow completed). 
			await _lobbyViewGroup.RebuildPlayerContexts(_lobbyViewGroup.AllPlayerCodes);

			_beamContext = _lobbyViewGroup.AllPlayerContexts[0];

			_mainLobbyPlayerSystem = _beamContext.ServiceProvider.GetService<MainLobbyPlayerSystem>();
			_joinLobbyPlayerSystem = _beamContext.ServiceProvider.GetService<JoinLobbyPlayerSystem>();
			_createLobbyPlayerSystem = _beamContext.ServiceProvider.GetService<CreateLobbyPlayerSystem>();
			_lobbyPlayerSystem = _beamContext.ServiceProvider.GetService<LobbyPlayerSystem>();

			List<SimGameType> gameTypes = await FetchGameTypes();

			_joinLobbyPlayerSystem.Setup(gameTypes);
			_createLobbyPlayerSystem.Setup(_beamContext, gameTypes);

			// We need some initial data before first Enrich will be called
			await _joinLobbyPlayerSystem.GetLobbies();

			OpenView(_currentView);
		}

		private async void OpenView(View newView)
		{
			_currentView = newView;
			UpdateVisibility();
			await _lobbyViewGroup.Enrich();
			_loadingIndicator.SetActive(false);
		}

		private void UpdateVisibility()
		{
			_mainLobbyPlayerSystem.IsVisible = _currentView == View.MainMenu;
			_createLobbyPlayerSystem.IsVisible = _currentView == View.CreateLobby;
			_joinLobbyPlayerSystem.IsVisible = _currentView == View.JoinLobby;
			_lobbyPlayerSystem.IsVisible = _currentView == View.InsideLobby;
		}

		private async Promise<List<SimGameType>> FetchGameTypes()
		{
			Assert.IsTrue(_gameTypes.Count > 0, "Game types count configured in inspector must be greater than 0");

			List<SimGameType> gameTypes = new List<SimGameType>();

			foreach (SimGameTypeRef simGameTypeRef in _gameTypes)
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
			OpenView(View.CreateLobby);
		}

		public void OpenLobbyView(Lobby lobby)
		{
			_lobbyPlayerSystem.Setup(lobby, _beamContext.PlayerId.ToString() == lobby.host);
			OpenView(View.InsideLobby);
		}

		public void HideOverlay()
		{
			_overlaysController.HideOverlay();
		}

		public void ShowOverlayedLabel(string label)
		{
			_overlaysController.ShowLabel(label);
		}

		public void ShowErrorWindow(string message)
		{
			_overlaysController.ShowError(message);
		}

		public void ShowConfirmWindow(string label, string message, Action confirmAction)
		{
			_overlaysController.ShowConfirm(label, message, confirmAction);
		}
	}
}
