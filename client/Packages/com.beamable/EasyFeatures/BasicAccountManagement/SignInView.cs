using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicAccountManagement
{
	public class SignInView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			BeamContext Context { get; set; }
		}
		
		public AccountManagementFeatureControl FeatureControl;
		public int EnrichOrder;

		public TMP_InputField EmailInput;
		public TMP_InputField PasswordInput;
		public Button ForgotPasswordButton;
		public Button SignInButton;
		public Button SignUpButton;

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
			
			FeatureControl.SetBackAction(GoBack);
			FeatureControl.SetHomeAction(OpenAccountsView);
			ForgotPasswordButton.onClick.ReplaceOrAddListener(OpenForgotPasswordView);
			SignInButton.onClick.ReplaceOrAddListener(OnSignInPressed);
			SignUpButton.onClick.ReplaceOrAddListener(OpenCreateAccountView);
		}

		private void OpenAccountsView()
		{
			FeatureControl.OpenAccountsView();
		}

		private void GoBack()
		{
			throw new System.NotImplementedException();
		}

		private void OnSignInPressed()
		{
			string email = EmailInput.text;
			string password = PasswordInput.text;
			
			throw new System.NotImplementedException();
		}

		private void OpenForgotPasswordView()
		{
			FeatureControl.OpenForgotPasswordView();
		}
		
		private void OpenCreateAccountView()
		{
			FeatureControl.OpenCreateAccountView();
		}
	}
}
