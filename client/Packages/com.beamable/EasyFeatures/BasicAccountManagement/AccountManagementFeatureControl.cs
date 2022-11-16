using Beamable.Common.Dependencies;
using Beamable.EasyFeatures.Components;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicAccountManagement
{
	[BeamContextSystem]
	public class AccountManagementFeatureControl : MonoBehaviour, IBeamableFeatureControl
	{
		private enum View
		{
			CurrentAccount,
			CreateAccount,
			SignIn,
			ForgotPassword,
			Management,
			Editing,
		}
		
		public BeamableViewGroup ViewGroup;
		public OverlaysController OverlaysController;
		
		public IEnumerable<BeamableViewGroup> ManagedViewGroups { get; }

		protected BeamContext Context;
		protected AccountManagementPlayerSystem PlayerSystem;

		private IBeamableView _currentView;
		private Dictionary<View, IBeamableView> _views = new Dictionary<View, IBeamableView>();

		[SerializeField]
		private bool _runOnEnable = true;

		public bool RunOnEnable
		{
			get => _runOnEnable;
			set => _runOnEnable = value;
		}
		
		[RegisterBeamableDependencies]
		public static void RegisterDefaultViewDeps(IDependencyBuilder builder)
		{
			// initialize player systems here
		}
		
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

			PlayerSystem = Context.ServiceProvider.GetService<AccountManagementPlayerSystem>();

			foreach (var view in ViewGroup.ManagedViews)
			{
				_views.Add(TypeToViewEnum(view.GetType()), view);
			}
		}

		private View TypeToViewEnum(Type type)
		{
			if (type == typeof(CurrentAccountView))
			{
				return View.CurrentAccount;
			}

			if (type == typeof(CreateAccountView))
			{
				return View.CreateAccount;
			}

			if (type == typeof(SignInView))
			{
				return View.SignIn;
			}

			if (type == typeof(ForgotPasswordView))
			{
				return View.ForgotPassword;
			}
			
			throw new ArgumentException("View enum does not support provided type.");
		}

		public void OpenCurrentAccountView()
		{
			OpenView(View.CurrentAccount);
		}
		
		public void OpenCreateAccountView()
		{
			OpenView(View.CreateAccount);
		}
		
		public void OpenSignInView()
		{
			OpenView(View.SignIn);
		}
		
		public void OpenForgotPasswordView()
		{
			OpenView(View.ForgotPassword);
		}

		private async void OpenView(View view)
		{
			if (_currentView != null)
			{
				_currentView.IsVisible = false;
			}

			_currentView = _views[view];
			_currentView.IsVisible = true;

			await ViewGroup.Enrich();
		}
	}
}
