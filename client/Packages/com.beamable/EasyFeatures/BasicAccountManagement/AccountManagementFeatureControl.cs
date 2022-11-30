using Beamable.Common.Dependencies;
using Beamable.EasyFeatures.Components;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicAccountManagement
{
	[BeamContextSystem]
	public class AccountManagementFeatureControl : MonoBehaviour, IBeamableFeatureControl
	{
		public enum View
		{
			Accounts,
			CreateAccount,
			SignIn,
			ForgotPassword,
			AccountInfo,
		}
		
		public BeamableViewGroup ViewGroup;
		public OverlaysController OverlaysController;
		public View DefaultView = View.Accounts;
		public Button BackButton;
		public Button HomeButton;
		
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
			builder.SetupUnderlyingSystemSingleton<AccountManagementPlayerSystem, AccountsView.IDependencies>();
			builder.SetupUnderlyingSystemSingleton<AccountManagementPlayerSystem, CreateAccountView.IDependencies>();
			builder.SetupUnderlyingSystemSingleton<AccountManagementPlayerSystem, SignInView.IDependencies>();
			builder.SetupUnderlyingSystemSingleton<AccountManagementPlayerSystem, ForgotPasswordView.IDependencies>();
			builder.SetupUnderlyingSystemSingleton<AccountManagementPlayerSystem, AccountInfoView.IDependencies>();
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
			
			OpenView(DefaultView);
		}

		private View TypeToViewEnum(Type type)
		{
			if (type == typeof(AccountsView))
			{
				return View.Accounts;
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

			if (type == typeof(AccountInfoView))
			{
				return View.AccountInfo;
			}
			
			throw new ArgumentException("View enum does not support provided type.");
		}

		public void OpenAccountsView()
		{
			OpenView(View.Accounts);
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

		public void OpenAccountInfoView()
		{
			OpenView(View.AccountInfo);
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

		public void SetHomeAction(UnityAction action)
		{
			HomeButton.onClick.RemoveAllListeners();
			HomeButton.onClick.AddListener(action);
		}

		public void SetBackAction(UnityAction action)
		{
			BackButton.onClick.RemoveAllListeners();
			BackButton.onClick.AddListener(action);
		}
	}
}
