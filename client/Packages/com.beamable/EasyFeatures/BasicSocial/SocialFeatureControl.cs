using Beamable.Common;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicSocial
{
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

		protected BeamContext Context;

		private IBeamableView _currentView;
		private readonly Dictionary<View, IBeamableView> views = new Dictionary<View, IBeamableView>();
		private readonly Dictionary<View, Toggle> viewTabs = new Dictionary<View, Toggle>();

		public void OnEnable()
		{
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

			await OpenView(View.Invites);
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
	}
}
