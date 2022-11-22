using Beamable.Common;
using Beamable.EasyFeatures.Components;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicAccountManagement
{
	public class CurrentAccountView : MonoBehaviour, IAsyncBeamableView
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
			SignInButton.onClick.ReplaceOrAddListener(OnSignIn);
			CreateAccountButton.onClick.ReplaceOrAddListener(OnCreateAccount);

			AccountSlotPresenter.PoolData data = new AccountSlotPresenter.PoolData
			{
				Height = 150, Index = 0, ViewData = await System.GetAccountViewData()
			};
			
			AccountPresenter.Setup(data, null, null);
		}
		
		private void OnSignIn()
		{
			throw new System.NotImplementedException();
		}

		private void OnCreateAccount()
		{
			throw new System.NotImplementedException();
		}
	}
}
