using Beamable.Common;
using Beamable.EasyFeatures.Components;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicAccountManagement
{
	public class AccountsView : MonoBehaviour, IAsyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			BeamContext Context { get; set; }

			/// <inheritdoc cref="AccountManagementPlayerSystem.GetAccountViewData"/>
			Promise<AccountSlotPresenter.ViewData> GetAccountViewData(long playerId = -1);
		}
		
		public AccountManagementFeatureControl FeatureControl;
		public int EnrichOrder;

		public AccountSlotPresenter AccountPresenter;
		public AccountsListPresenter OtherAccountsList;
		public ToggleGroup OtherAccountsToggleGroup;
		public GameObject OtherAccountsGroup;
		
		[Header("Button groups")]
		public GameObject SignInButtonsGroup;
		public GameObject SwitchButtonsGroup;
		
		[Space]
		public Button SignInButton;
		public Button CreateAccountButton;
		
		[Header("Switch Account Popup")]
		public SwitchAccountPopup SwitchAccountPopupPrefab;
		public Button LoadGameButton;
		public Button SwitchAccountButton;

		protected IDependencies System;

		private long _selectedOtherAccountId = -1;
		
		public bool IsVisible
		{
			get => gameObject.activeSelf;
			set => gameObject.SetActive(value);
		}

		public int GetEnrichOrder() => EnrichOrder;

		public async Promise EnrichWithContext(BeamContextGroup managedPlayers)
		{
			var context = managedPlayers.GetSinglePlayerContext();
			System = context.ServiceProvider.GetService<IDependencies>();
			System.Context = context;

			if (!IsVisible)
			{
				return;
			}

			FeatureControl.SetLoadingOverlay(true);
			
			await System.Context.Accounts.OnReady;
			
			SignInButtonsGroup.SetActive(System.Context.Accounts.Count == 1);
			SwitchButtonsGroup.SetActive(System.Context.Accounts.Count > 1);
			LoadGameButton.interactable = _selectedOtherAccountId > 0;
			
			SwitchAccountPopupPrefab.gameObject.SetActive(false);

			// setup callbacks
			FeatureControl.SetBackAction(GoBack);
			FeatureControl.SetHomeAction(OpenAccountsView);
			SignInButton.onClick.ReplaceOrAddListener(OnSignIn);
			CreateAccountButton.onClick.ReplaceOrAddListener(OnCreateAccount);
			LoadGameButton.onClick.ReplaceOrAddListener(OnLoadGame);
			SwitchAccountButton.onClick.ReplaceOrAddListener(OnSwitchAccount);

			// setup current account
			AccountSlotPresenter.PoolData data = new AccountSlotPresenter.PoolData
			{
				Height = 150, Index = 0, ViewData = await System.GetAccountViewData()
			};
			
			AccountPresenter.Setup(data, OpenAccountInfoView, null);
			
			// setup other accounts
			bool hasOtherAccounts = System.Context.Accounts.Count > 1;
			OtherAccountsGroup.SetActive(hasOtherAccounts);
			if (hasOtherAccounts)
			{
				List<AccountSlotPresenter.ViewData> accountsViewData = new List<AccountSlotPresenter.ViewData>();
				foreach (var account in System.Context.Accounts)
				{
					if (account.GamerTag == System.Context.PlayerId)
						continue;

					accountsViewData.Add(await System.GetAccountViewData(account.GamerTag));
				}
				
				OtherAccountsList.SetupToggles(accountsViewData, OtherAccountsToggleGroup, OnOtherAccountSelected);
			}
			
			FeatureControl.SetLoadingOverlay(false);
		}

		private void OnOtherAccountSelected(long playerId)
		{
			_selectedOtherAccountId = playerId;
			LoadGameButton.interactable = true;
		}

		private void OnSwitchAccount()
		{
			var popup = FeatureControl.OverlaysController.ShowCustomOverlay(SwitchAccountPopupPrefab);
			popup.Setup(OpenSignInView, OnCreateAccount, FeatureControl.OverlaysController);

			void OpenSignInView()
			{
				FeatureControl.OverlaysController.HideOverlay();
				OnSignIn();
			}
		}

		private async void OnLoadGame()
		{
			var account = System.Context.Accounts.FirstOrDefault(acc => acc.GamerTag == _selectedOtherAccountId);
			if (account != null)
			{
				FeatureControl.SetLoadingOverlay(true);
				await account.SwitchToAccount();
				FeatureControl.OpenAccountsView();
			}
			else
			{
				Debug.LogError($"Account with id '{_selectedOtherAccountId}' was not found");
			}
		}

		private void OpenAccountsView()
		{
			FeatureControl.OpenAccountsView();
		}

		private void GoBack() { }

		private void OpenAccountInfoView(long playerId)
		{
			FeatureControl.OpenAccountInfoView();
		}

		private void OnSignIn()
		{
			FeatureControl.OpenSignInView();
		}

		private async void OnCreateAccount()
		{
			var guestAccount = await System.Context.Accounts.CreateNewAccount();
			await guestAccount.SwitchToAccount();
			FeatureControl.OpenAccountsView();
		}
	}
}
