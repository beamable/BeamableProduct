using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicAccountManagement
{
	public class AccountInfoView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			BeamContext Context { get; set; }
		}

		public AccountManagementFeatureControl FeatureControl;
		public int EnrichOrder;

		public Button ConfirmButton;
		public Button CancelButton;

		protected IDependencies System;

		public bool IsVisible
		{
			get => gameObject.activeSelf;
			set => gameObject.SetActive(value);
		}

		public int GetEnrichOrder() => EnrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			var context = managedPlayers.GetSinglePlayerContext();
			System = context.ServiceProvider.GetService<IDependencies>();
			System.Context = context;
			
			if (!IsVisible)
			{
				return;
			}
			
			FeatureControl.SetBackAction(OpenAccountsView);
			FeatureControl.SetHomeAction(OpenAccountsView);
			ConfirmButton.onClick.ReplaceOrAddListener(OnConfirmPressed);
			CancelButton.onClick.ReplaceOrAddListener(OpenAccountsView);
		}

		private void OnConfirmPressed()
		{
			// set new account data
			
			OpenAccountsView();
		}

		private void OpenAccountsView() => FeatureControl.OpenAccountsView();
	}
}
