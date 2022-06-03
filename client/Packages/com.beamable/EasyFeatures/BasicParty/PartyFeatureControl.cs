using Beamable.Common.Dependencies;
using Beamable.EasyFeatures.Components;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicParty
{
	[BeamContextSystem]
	public class PartyFeatureControl : MonoBehaviour, IBeamableFeatureControl
	{
		private enum View
		{
			Party,
			Create,
			Join,
			Invite,
		}

		[SerializeField] private bool _runOnEnable = true;
		[SerializeField] private BeamableViewGroup _partyViewGroup;
		public OverlaysController OverlaysController;
		
		private BasicPartyPlayerSystem _partyPlayerSystem;
		private CreatePartyPlayerSystem _createPartyPlayerSystem;
		private InvitePlayersPlayerSystem _invitePlayersPlayerSystem;
		private JoinPartyPlayerSystem _joinPartyPlayerSystem;

		private View _currentView = View.Create;

		public IEnumerable<BeamableViewGroup> ManagedViewGroups
		{
			get;
		}

		[RegisterBeamableDependencies]
		public static void RegisterDefaultViewDeps(IDependencyBuilder builder)
		{
			builder.SetupUnderlyingSystemSingleton<BasicPartyPlayerSystem, BasicPartyView.IDependencies>();
			builder.SetupUnderlyingSystemSingleton<CreatePartyPlayerSystem, CreatePartyView.IDependencies>();
			builder.SetupUnderlyingSystemSingleton<InvitePlayersPlayerSystem, InvitePlayersView.IDependencies>();
			builder.SetupUnderlyingSystemSingleton<JoinPartyPlayerSystem, JoinPartyView.IDependencies>();
		}
		
		public bool RunOnEnable
		{
			get => _runOnEnable;
			set => _runOnEnable = value;
		}

		public void OnEnable()
		{
			_partyViewGroup.RebuildManagedViews();

			if (!_runOnEnable)
			{
				return;
			}
			
			Run();
		}

		public async void Run()
		{
			await _partyViewGroup.RebuildPlayerContexts(_partyViewGroup.AllPlayerCodes);

			var beamContext = _partyViewGroup.AllPlayerContexts[0];

			_partyPlayerSystem = beamContext.ServiceProvider.GetService<BasicPartyPlayerSystem>();
			_createPartyPlayerSystem = beamContext.ServiceProvider.GetService<CreatePartyPlayerSystem>();
			_invitePlayersPlayerSystem = beamContext.ServiceProvider.GetService<InvitePlayersPlayerSystem>();
			_joinPartyPlayerSystem = beamContext.ServiceProvider.GetService<JoinPartyPlayerSystem>();
			
			OpenView(_currentView);
		}

		public void OpenPartyView(Party party)
		{
			_partyPlayerSystem.Party = party;
			_partyPlayerSystem.Setup(party.Players);
			_partyPlayerSystem.IsPlayerLeader = true;	// temporary
			OpenView(View.Party);
		}
		
		// when party data is provided the view turns to settings
		public void OpenCreatePartyView(Party party = null)
		{
			_createPartyPlayerSystem.Party = party;
			OpenView(View.Create);
		}
		
		public void OpenInviteView(List<PartySlotPresenter.ViewData> friendsList, Party party)
		{
			_invitePlayersPlayerSystem.Players = friendsList;
			_invitePlayersPlayerSystem.Party = party;
			OpenView(View.Invite);
		}
		
		public void OpenJoinView()
		{
			OpenView(View.Join);
		}

		private async void OpenView(View view)
		{
			_currentView = view;
			UpdateVisibility();
			await _partyViewGroup.Enrich();
		}

		private void UpdateVisibility()
		{
			_partyPlayerSystem.IsVisible = _currentView == View.Party;
			_createPartyPlayerSystem.IsVisible = _currentView == View.Create;
			_invitePlayersPlayerSystem.IsVisible = _currentView == View.Invite;
			_joinPartyPlayerSystem.IsVisible = _currentView == View.Join;
		}
	}
}
