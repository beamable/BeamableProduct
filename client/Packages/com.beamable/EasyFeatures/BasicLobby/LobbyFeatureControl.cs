using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicLobby
{
	[BeamContextSystem]
	public class LobbyFeatureControl : MonoBehaviour, IBeamableFeatureControl
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

		[Header("Fast-Path Configuration")]
		[SerializeField] private List<SimGameTypeRef> _gameTypes;

		[SerializeField] private BeamableViewGroup _lobbyViewGroup;
		[SerializeField] private bool _testMode;

		private CreateLobbyPlayerSystem _createLobbyPlayerSystem;

		private View _currentView = View.MainMenu;
		private InsideLobbyPlayerSystem _insideLobbyPlayerSystem;
		private JoinLobbyPlayerSystem _joinLobbyPlayerSystem;

		private MainLobbyPlayerSystem _mainLobbyPlayerSystem;

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

		[RegisterBeamableDependencies]
		public static void RegisterDefaultViewDeps(IDependencyBuilder builder)
		{
			builder.SetupUnderlyingSystemSingleton<MainLobbyPlayerSystem, MainLobbyView.IDependencies>();
			builder.SetupUnderlyingSystemSingleton<JoinLobbyPlayerSystem, JoinLobbyView.IDependencies>();
			builder.SetupUnderlyingSystemSingleton<CreateLobbyPlayerSystem, CreateLobbyView.IDependencies>();
			builder.SetupUnderlyingSystemSingleton<InsideLobbyPlayerSystem, InsideLobbyView.IDependencies>();
		}

		public async void Run()
		{
			// Ensures the player contexts this view is configured to use are ready (frictionless login flow completed). 
			await _lobbyViewGroup.RebuildPlayerContexts(_lobbyViewGroup.AllPlayerCodes);

			BeamContext ctx = _lobbyViewGroup.AllPlayerContexts[0];

			_mainLobbyPlayerSystem = ctx.ServiceProvider.GetService<MainLobbyPlayerSystem>();
			_joinLobbyPlayerSystem = ctx.ServiceProvider.GetService<JoinLobbyPlayerSystem>();
			_createLobbyPlayerSystem = ctx.ServiceProvider.GetService<CreateLobbyPlayerSystem>();
			_insideLobbyPlayerSystem = ctx.ServiceProvider.GetService<InsideLobbyPlayerSystem>();

			await _joinLobbyPlayerSystem.Setup(_testMode, _gameTypes);

			OpenView(_currentView);
		}

		private async void OpenView(View newView)
		{
			_currentView = newView;
			UpdateVisibility();
			await _lobbyViewGroup.Enrich();
		}

		private void UpdateVisibility()
		{
			_mainLobbyPlayerSystem.IsVisible = _currentView == View.MainMenu;
			_createLobbyPlayerSystem.IsVisible = _currentView == View.CreateLobby;
			_joinLobbyPlayerSystem.IsVisible = _currentView == View.JoinLobby;
			_insideLobbyPlayerSystem.IsVisible = _currentView == View.InsideLobby;
		}

		#region OnClick() delegate wrappers

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

		public void OpenInsideLobbyView()
		{
			OpenView(View.InsideLobby);
		}

		#endregion
	}
}
