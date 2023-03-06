using Beamable.Common;
using Beamable.EasyFeatures.Components;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicAccountManagement
{
	public class AccountsView : MonoBehaviour, IAsyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			BeamContext Context { get; set; }

			/// <inheritdoc cref="AccountManagementPlayerSystem.GetOverridenAccountData"/>
			Promise<AccountSlotPresenter.ViewData> GetOverridenAccountData(bool includeAuthMethods, bool isOnline, long playerId = -1);
			int AuthenticatedAccountsCount();
			/// <inheritdoc cref="AccountManagementPlayerSystem.GetLinkedEmailAddress"/>
			string GetLinkedEmailAddress(long playerId);
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

			bool hasSingleGuestAccount = System.Context.Accounts.Count == 1 && System.AuthenticatedAccountsCount() == 0;
			SignInButtonsGroup.SetActive(hasSingleGuestAccount);
			SwitchButtonsGroup.SetActive(!hasSingleGuestAccount);
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
				Height = 150, Index = 0, ViewData = await System.GetOverridenAccountData(true, true)
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

					accountsViewData.Add(await System.GetOverridenAccountData(true, false, account.GamerTag));
				}
				
				OtherAccountsList.SetupToggles(accountsViewData, OtherAccountsToggleGroup, OnOtherAccountSelected, RemoveAccount);
			}
			
			FeatureControl.SetLoadingOverlay(false);
		}

		private async void RemoveAccount(long playerId)
		{
			var account = System.Context.Accounts.FirstOrDefault(acc => acc.GamerTag == playerId);
			if (account != null)
			{
				await account.Remove();
				FeatureControl.OpenAccountsView();
			}
			else
			{
				Debug.LogError($"Account with ID '{playerId}' does not exist on this device!");
			}
		}

		private void OnDisable()
		{
			_selectedOtherAccountId = -1;
		}

		private void OnOtherAccountSelected(bool selected, long playerId)
		{
			_selectedOtherAccountId = playerId;
			LoadGameButton.interactable = selected;
		}

		private void OnSwitchAccount()
		{
			var popup = FeatureControl.OverlaysController.ShowCustomOverlay(SwitchAccountPopupPrefab);
			popup.Setup(OpenSignInView, CreateAccount, FeatureControl.OverlaysController);

			void OpenSignInView()
			{
				FeatureControl.OverlaysController.HideOverlay();
				OnSignIn();
			}

			void CreateAccount()
			{
				FeatureControl.OverlaysController.HideOverlay();
				OnCreateAccount();
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
			string email = System.GetLinkedEmailAddress(playerId);
			FeatureControl.OpenAccountInfoView(email);
		}

		private void OnSignIn()
		{
			FeatureControl.OpenSignInView();
		}

		private async void OnCreateAccount()
		{
			FeatureControl.SetLoadingOverlay(true);
			var guestAccount = await System.Context.Accounts.CreateNewAccount();
			await guestAccount.SwitchToAccount();
			FeatureControl.SetLoadingOverlay(false);
			FeatureControl.OpenAccountsView();
		}
	}
}
