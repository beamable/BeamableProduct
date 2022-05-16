using Beamable.Common.Dependencies;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicLobby
{
	[BeamContextSystem]
	public class BasicLobbyFeatureControl : MonoBehaviour, IBeamableFeatureControl
	{
		[Header("Feature Control"), SerializeField]
		private bool _runOnEnable = true;

		[SerializeField]
		private BeamableViewGroup _lobbyViewGroup;

		private MainLobbyPlayerSystem _mainLobbyPlayerSystem;
		private CreateLobbyPlayerSystem _createLobbyPlayerSystem;
		private BasicLobbyPlayerSystem _lobbySystem;

		public IEnumerable<BeamableViewGroup> ManagedViewGroups
		{
			get => new[] {_lobbyViewGroup};
			set => _lobbyViewGroup = value.FirstOrDefault();
		}

		public bool RunOnEnable { get => _runOnEnable; set => _runOnEnable = value; }

		[RegisterBeamableDependencies]
		public static void RegisterDefaultViewDeps(IDependencyBuilder builder)
		{
			builder.SetupUnderlyingSystemSingleton<BasicLobbyPlayerSystem, BasicLobbyView.ILobbyDeps>();
			builder.SetupUnderlyingSystemSingleton<MainLobbyPlayerSystem, MainLobbyView.IMainLobbyViewDeps>();
			builder.SetupUnderlyingSystemSingleton<CreateLobbyPlayerSystem, CreateLobbyView.ICreateLobbyDeps>();
		}

		public void OnEnable()
		{
			_lobbyViewGroup.RebuildManagedViews();

			if (!RunOnEnable) return;

			Run();
		}

		public async void Run()
		{
			// Ensures the player contexts this view is configured to use are ready (frictionless login flow completed). 
			await _lobbyViewGroup.RebuildPlayerContexts(_lobbyViewGroup.AllPlayerCodes);
			
			BeamContext ctx = _lobbyViewGroup.AllPlayerContexts[0];

			_lobbySystem = ctx.ServiceProvider.GetService<BasicLobbyPlayerSystem>();
			_mainLobbyPlayerSystem = ctx.ServiceProvider.GetService<MainLobbyPlayerSystem>();
			_createLobbyPlayerSystem = ctx.ServiceProvider.GetService<CreateLobbyPlayerSystem>();

			OpenMainLobbyView();
		}

		public async void OpenMainLobbyView()
		{
			_lobbySystem.ActiveView = BasicLobbyView.View.MainMenu;
			UpdateVisibility();
			await _lobbyViewGroup.Enrich();
		}

		public async void OpenCreateLobby()
		{
			_lobbySystem.ActiveView = BasicLobbyView.View.CreateLobby;
			UpdateVisibility();
			await _lobbyViewGroup.Enrich();
		}

		private void UpdateVisibility()
		{
			_mainLobbyPlayerSystem.IsVisible = _lobbySystem.ActiveView == BasicLobbyView.View.MainMenu;
			_createLobbyPlayerSystem.IsVisible = _lobbySystem.ActiveView == BasicLobbyView.View.CreateLobby;
		}
	}
}
