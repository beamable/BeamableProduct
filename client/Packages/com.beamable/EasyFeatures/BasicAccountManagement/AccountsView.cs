using Beamable.Common;
using Beamable.EasyFeatures.Components;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicAccountManagement
{
	public class AccountsView : MonoBehaviour, IAsyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			BeamContext Context { get; set; }

			Promise<AccountSlotPresenter.ViewData> GetAccountViewData();
		}
		
		public AccountManagementFeatureControl FeatureControl;
		public int EnrichOrder;

		public AccountSlotPresenter AccountPresenter;
		public GameObject SignInButtonsGroup;
		public GameObject SwitchButtonsGroup;
		public Button SignInButton;
		public Button CreateAccountButton;
		public Button LoadGameButton;
		public Button SwitchAccountButton;
		public SwitchAccountPopup SwitchAccountPopupPrefab;

		protected IDependencies System;
		
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
			
			// TODO enable proper set of buttons
			SignInButtonsGroup.SetActive(false);
			SwitchButtonsGroup.SetActive(true);
			SwitchAccountPopupPrefab.gameObject.SetActive(false);
			
			// setup callbacks
			FeatureControl.SetBackAction(GoBack);
			FeatureControl.SetHomeAction(OpenAccountsView);
			SignInButton.onClick.ReplaceOrAddListener(OnSignIn);
			CreateAccountButton.onClick.ReplaceOrAddListener(OnCreateAccount);
			LoadGameButton.onClick.ReplaceOrAddListener(OnLoadGame);
			SwitchAccountButton.onClick.ReplaceOrAddListener(OnSwitchAccount);

			AccountSlotPresenter.PoolData data = new AccountSlotPresenter.PoolData
			{
				Height = 150, Index = 0, ViewData = await System.GetAccountViewData()
			};
			
			AccountPresenter.Setup(data, OpenAccountInfoView, null);
		}

		private void OnSwitchAccount()
		{
			var popup = FeatureControl.OverlaysController.CustomOverlay.Show(SwitchAccountPopupPrefab);
			popup.Setup(OpenSignInView, OpenCreateAccountView);

			void OpenSignInView()
			{
				FeatureControl.OverlaysController.CustomOverlay.Hide();
				FeatureControl.OpenSignInView();
			}

			void OpenCreateAccountView()
			{
				FeatureControl.OverlaysController.CustomOverlay.Hide();
				FeatureControl.OpenCreateAccountView();
			}
		}

		private void OnLoadGame()
		{
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
