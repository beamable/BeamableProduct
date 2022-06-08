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

		public BeamableViewGroup PartyViewGroup;
		public OverlaysController OverlaysController;
		
		protected BasicPartyPlayerSystem PartyPlayerSystem;
		protected CreatePartyPlayerSystem CreatePartyPlayerSystem;
		protected InvitePlayersPlayerSystem InvitePlayersPlayerSystem;
		protected JoinPartyPlayerSystem JoinPartyPlayerSystem;

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

		[SerializeField]
		private bool _runOnEnable = true;
		public bool RunOnEnable
		{
			get => _runOnEnable;
			set => _runOnEnable = value;
		}

		public void OnEnable()
		{
			PartyViewGroup.RebuildManagedViews();

			if (!_runOnEnable)
			{
				return;
			}
			
			Run();
		}

		public async void Run()
		{
			await PartyViewGroup.RebuildPlayerContexts(PartyViewGroup.AllPlayerCodes);

			var beamContext = PartyViewGroup.AllPlayerContexts[0];

			PartyPlayerSystem = beamContext.ServiceProvider.GetService<BasicPartyPlayerSystem>();
			CreatePartyPlayerSystem = beamContext.ServiceProvider.GetService<CreatePartyPlayerSystem>();
			InvitePlayersPlayerSystem = beamContext.ServiceProvider.GetService<InvitePlayersPlayerSystem>();
			JoinPartyPlayerSystem = beamContext.ServiceProvider.GetService<JoinPartyPlayerSystem>();
			
			OpenView(_currentView);
		}

		public void OpenPartyView(Party party)
		{
			PartyPlayerSystem.Party = party;
			PartyPlayerSystem.Setup(party.Players);
			PartyPlayerSystem.IsPlayerLeader = true;	// temporary
			OpenView(View.Party);
		}
		
		// when party data is provided the view turns to settings
		public void OpenCreatePartyView(Party party = null)
		{
			CreatePartyPlayerSystem.Party = party;
			OpenView(View.Create);
		}
		
		public void OpenInviteView(List<PartySlotPresenter.ViewData> friendsList, Party party)
		{
			InvitePlayersPlayerSystem.FriendsList = friendsList;
			InvitePlayersPlayerSystem.Party = party;
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
			await PartyViewGroup.Enrich();
		}

		private void UpdateVisibility()
		{
			PartyPlayerSystem.IsVisible = _currentView == View.Party;
			CreatePartyPlayerSystem.IsVisible = _currentView == View.Create;
			InvitePlayersPlayerSystem.IsVisible = _currentView == View.Invite;
			JoinPartyPlayerSystem.IsVisible = _currentView == View.Join;
		}
	}
}
