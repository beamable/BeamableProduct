using Beamable.Common;
using Beamable.Common.Dependencies;
using Beamable.EasyFeatures.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicSocial
{
	[BeamContextSystem]
	public class SocialFeatureControl : MonoBehaviour, IBeamableFeatureControl
	{
		public enum View
		{
			Friends = 0,
			Invites = 1,
			Blocked = 2,
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
		public MultiToggleComponent TabToggles;
		public FriendInfoPopup InfoPopup;
		public GameObject LoadingOverlay;
		public OverlaysController OverlaysController;
		public View DefaultView = View.Friends;

		protected BeamContext Context;

		private IBeamableView _currentView;
		private readonly Dictionary<View, IBeamableView> views = new Dictionary<View, IBeamableView>();

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

			await OpenView(DefaultView);
			
			var names = Enum.GetNames(typeof(View)).ToList();
			TabToggles.Setup(names, OnTabSelected, (int)DefaultView);
			
			SetLoadingOverlay(false);
		}

		private async void OnTabSelected(int tabId)
		{
			await OpenView((View)tabId);
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
