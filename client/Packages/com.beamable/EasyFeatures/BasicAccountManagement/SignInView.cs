using Beamable.EasyFeatures.Components;
using Beamable.Player;
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
			bool IsEmailValid(string email, out string errorMessage);
		}
		
		public AccountManagementFeatureControl FeatureControl;
		public int EnrichOrder;

		public TMP_InputField EmailInput;
		public TMP_InputField PasswordInput;
		public Button ForgotPasswordButton;
		public Button NextButton;
		public Button SignInButton;
		public Button SignUpButton;
		public ErrorMessageText ErrorText;

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
			
			ErrorText.HideMessage();
			
			PasswordInput.gameObject.SetActive(false);
			ForgotPasswordButton.gameObject.SetActive(false);
			SignInButton.gameObject.SetActive(false);
			NextButton.gameObject.SetActive(true);
			
			FeatureControl.SetBackAction(GoBack);
			FeatureControl.SetHomeAction(OpenAccountsView);
			ForgotPasswordButton.onClick.ReplaceOrAddListener(OpenForgotPasswordView);
			NextButton.onClick.ReplaceOrAddListener(OnNextButtonPressed);
			SignInButton.onClick.ReplaceOrAddListener(OnSignInPressed);
			SignUpButton.onClick.ReplaceOrAddListener(OpenCreateAccountView);
		}

		private async void OnNextButtonPressed()
		{
			if (System.IsEmailValid(EmailInput.text, out string error))
			{
				ErrorText.HideMessage();
				
				if (await System.Context.Accounts.IsEmailAvailable(EmailInput.text))
				{
					FeatureControl.OpenCreateAccountView();
				}
				else
				{
					PasswordInput.gameObject.SetActive(true);
					ForgotPasswordButton.gameObject.SetActive(true);
					NextButton.gameObject.SetActive(false);
					SignInButton.gameObject.SetActive(true);
				}
			}
			else
			{
				ErrorText.SetErrorMessage(error);
			}
		}
		
		private async void OnSignInPressed()
		{
			string email = EmailInput.text;
			string password = PasswordInput.text;

			var result = await System.Context.Accounts.RecoverAccountWithEmail(email, password);
			
			if (result.isSuccess)
			{
				ErrorText.HideMessage();
				
				await result.SwitchToAccount();
				FeatureControl.OpenAccountsView();
			}
			else
			{
				switch (result.error)
				{
					case PlayerRecoveryError.UNKNOWN_CREDENTIALS:
						ErrorText.SetErrorMessage("Incorrect credentials");
						break;
					
					default:
						ErrorText.SetErrorMessage("Unknown error has occured");
						break;
				}
			}
		}

		private void OpenAccountsView()
		{
			FeatureControl.OpenAccountsView();
		}

		private void GoBack()
		{
			FeatureControl.OpenAccountsView();
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
