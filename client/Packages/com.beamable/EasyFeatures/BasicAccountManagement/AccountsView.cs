using Beamable.Common;
using Beamable.EasyFeatures.Components;
using System.Collections.Generic;
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

		
		private const string ENTER_EMAIL_TEXT = "Please enter your email here...";
		private const string SIGN_IN_INFO_TEXT = "Sign in or create an account to save your progress online.";
		
		public AccountManagementFeatureControl FeatureControl;
		public int EnrichOrder;

		public AccountSlotPresenter AccountPresenter;
		public TextMeshProUGUI InfoText;
		public AccountsListPresenter OtherAccountsList;
		public ToggleGroup OtherAccountsToggleGroup;
		public GameObject OtherAccountsGroup;
		
		[Header("Button groups")]
		public GameObject SignInButtonsGroup;
		public GameObject SwitchButtonsGroup;
		public GameObject NextCancelButtonsGroup;
		
		[Space]
		public Button SignInButton;
		public Button CreateAccountButton;
		public Button NextButton;
		public Button CancelButton;
		public TMP_InputField EmailInputField;
		
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
			
			await System.Context.Accounts.OnReady;
			
			// TODO enable proper set of buttons
			SignInButtonsGroup.SetActive(System.Context.Accounts.Count == 1);
			SwitchButtonsGroup.SetActive(System.Context.Accounts.Count > 1);
			NextCancelButtonsGroup.SetActive(false);
			LoadGameButton.interactable = _selectedOtherAccountId > 0;
			
			SwitchAccountPopupPrefab.gameObject.SetActive(false);
			EmailInputField.gameObject.SetActive(false);

			InfoText.text = SIGN_IN_INFO_TEXT;
			
			// setup callbacks
			FeatureControl.SetBackAction(GoBack);
			FeatureControl.SetHomeAction(OpenAccountsView);
			SignInButton.onClick.ReplaceOrAddListener(OnSignIn);
			CreateAccountButton.onClick.ReplaceOrAddListener(OnCreateAccount);
			LoadGameButton.onClick.ReplaceOrAddListener(OnLoadGame);
			SwitchAccountButton.onClick.ReplaceOrAddListener(OnSwitchAccount);
			NextButton.onClick.ReplaceOrAddListener(OnSignIn);
			CancelButton.onClick.ReplaceOrAddListener(OnCancel);

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
		}

		private void OnOtherAccountSelected(long playerId)
		{
			_selectedOtherAccountId = playerId;
			LoadGameButton.interactable = true;
		}

		private void OnCancel()
		{
			// restore normal accounts view
			NextCancelButtonsGroup.SetActive(false);
			SwitchButtonsGroup.SetActive(true);
			EmailInputField.gameObject.SetActive(false);
		}

		private void OnSwitchAccount()
		{
			var popup = FeatureControl.OverlaysController.CustomOverlay.Show(SwitchAccountPopupPrefab);
			popup.Setup(StartSignIn, OpenCreateAccountView);

			void StartSignIn()
			{
				FeatureControl.OverlaysController.CustomOverlay.Hide();
				SwitchButtonsGroup.SetActive(false);
				NextCancelButtonsGroup.SetActive(true);
				EmailInputField.gameObject.SetActive(true);
				InfoText.text = ENTER_EMAIL_TEXT;
			}

			void OpenCreateAccountView()
			{
				FeatureControl.OverlaysController.CustomOverlay.Hide();
				FeatureControl.OpenCreateAccountView();
			}
		}

		private void OnLoadGame()
		{
			// load profile with id == _selectedOtherAccountId
			throw new System.NotImplementedException();
		}

		private void OpenAccountsView()
		{
			FeatureControl.OpenAccountsView();
		}

		private void GoBack()
		{
			throw new System.NotImplementedException();
		}

		private void OpenAccountInfoView(long playerId)
		{
			FeatureControl.OpenAccountInfoView();
		}

		private void OnSignIn()
		{
			FeatureControl.OpenSignInView();
		}

		private void OnCreateAccount()
		{
			FeatureControl.OpenCreateAccountView();
		}
	}
}
