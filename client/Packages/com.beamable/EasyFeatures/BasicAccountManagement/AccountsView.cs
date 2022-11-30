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
		public Button SignInButton;
		public Button CreateAccountButton;

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
			
			// setup callbacks
			FeatureControl.SetBackAction(GoBack);
			FeatureControl.SetHomeAction(OpenAccountsView);
			SignInButton.onClick.ReplaceOrAddListener(OnSignIn);
			CreateAccountButton.onClick.ReplaceOrAddListener(OnCreateAccount);

			AccountSlotPresenter.PoolData data = new AccountSlotPresenter.PoolData
			{
				Height = 150, Index = 0, ViewData = await System.GetAccountViewData()
			};
			
			AccountPresenter.Setup(data, OpenAccountInfoView, null);
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
