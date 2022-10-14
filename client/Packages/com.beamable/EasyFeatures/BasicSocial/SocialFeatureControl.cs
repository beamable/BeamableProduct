using Beamable.Common;
using Beamable.Common.Dependencies;
using Beamable.EasyFeatures.Components;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicSocial
{
	[BeamContextSystem]
	public class SocialFeatureControl : MonoBehaviour, IBeamableFeatureControl
	{
		private enum View
		{
			Friends,
			Invites,
			Blocked,
		}
		
		public IEnumerable<BeamableViewGroup> ManagedViewGroups { get; }

		[SerializeField]
		private bool _runOnEnable = true;
		public bool RunOnEnable
		{
			get => _runOnEnable;
			set => _runOnEnable = value;
		}

		public BeamableViewGroup ViewGroup;
		public Toggle FriendsTab;
		public Toggle InvitesTab;
		public Toggle BlockedTab;
		public FriendInfoPopup InfoPopup;
		public GameObject LoadingOverlay;
		public OverlaysController OverlaysController;

		protected BeamContext Context;

		private IBeamableView _currentView;
		private readonly Dictionary<View, IBeamableView> views = new Dictionary<View, IBeamableView>();
		private readonly Dictionary<View, Toggle> viewTabs = new Dictionary<View, Toggle>();

		[RegisterBeamableDependencies]
		public static void RegisterDefaultViewDeps(IDependencyBuilder builder)
		{
			builder.SetupUnderlyingSystemSingleton<BasicSocialPlayerSystem, BasicFriendsView.IDependencies>();
			builder.SetupUnderlyingSystemSingleton<BasicSocialPlayerSystem, BasicBlockedView.IDependencies>();
			builder.SetupUnderlyingSystemSingleton<BasicSocialPlayerSystem, BasicInvitesView.IDependencies>();
		}
		
		public void OnEnable()
		{
			SetLoadingOverlay(true);
			ViewGroup.RebuildManagedViews();

			if (!_runOnEnable)
			{
				return;
			}
			
			Run();
		}

		public async void Run()
		{
			await ViewGroup.RebuildPlayerContexts(ViewGroup.AllPlayerCodes);

			Context = ViewGroup.AllPlayerContexts[0];
			await Context.OnReady;

			foreach (var view in ViewGroup.ManagedViews)
			{
				views.Add(TypeToViewEnum(view.GetType()), view);
				view.IsVisible = false;
			}
			
			FriendsTab.onValueChanged.ReplaceOrAddListener(isOn => TabPicked(isOn, View.Friends));
			InvitesTab.onValueChanged.ReplaceOrAddListener(isOn => TabPicked(isOn, View.Invites));
			BlockedTab.onValueChanged.ReplaceOrAddListener(isOn => TabPicked(isOn, View.Blocked));
			viewTabs.Add(View.Friends, FriendsTab);
			viewTabs.Add(View.Invites, InvitesTab);
			viewTabs.Add(View.Blocked, BlockedTab);

			await OpenView(View.Friends);
			
			SetLoadingOverlay(false);
		}

		private async void TabPicked(bool isOn, View view)
		{
			if (!isOn)
			{
				return;
			}

			await OpenView(view);
		}

		private View TypeToViewEnum(Type type)
		{
			if (type == typeof(BasicFriendsView))
			{
				return View.Friends;
			}

			if (type == typeof(BasicInvitesView))
			{
				return View.Invites;
			}

			if (type == typeof(BasicBlockedView))
			{
				return View.Blocked;
			}

			throw new ArgumentException("View enum does not support provided type.");
		}

		private async Promise OpenView(View view)
		{
			viewTabs[view].isOn = true;
			
			if (_currentView != null)
			{
				_currentView.IsVisible = false;	
			}
			
			_currentView = views[view];
			_currentView.IsVisible = true;
			
			await ViewGroup.Enrich();
		}

		public async Promise OpenInfoPopup(long playerId, Action<long> onDeleteButton, Action<long> onBlockButton, Action<long> onMessageButton)
		{
			await InfoPopup.Setup(playerId, onDeleteButton, onBlockButton, onMessageButton);
		}

		public void SetLoadingOverlay(bool active) => LoadingOverlay.SetActive(active);
	}
}
