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
			EmailInput.text = string.Empty;
			PasswordInput.text = string.Empty;
			
			PasswordInput.gameObject.SetActive(false);
			ForgotPasswordButton.transform.parent.gameObject.SetActive(false);
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
				FeatureControl.SetLoadingOverlay(true);
				if (await System.Context.Accounts.IsEmailAvailable(EmailInput.text))
				{
					FeatureControl.OpenCreateAccountView(EmailInput.text);
				}
				else
				{
					PasswordInput.gameObject.SetActive(true);
					ForgotPasswordButton.transform.parent.gameObject.SetActive(true);
					NextButton.gameObject.SetActive(false);
					SignInButton.gameObject.SetActive(true);
				}
			}
			else
			{
				ErrorText.SetErrorMessage(error);
			}
			
			FeatureControl.SetLoadingOverlay(false);
		}
		
		private async void OnSignInPressed()
		{
			string email = EmailInput.text;
			string password = PasswordInput.text;

			if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
			{
				ErrorText.SetErrorMessage("Please provide your credentials");
				return;
			}

			FeatureControl.SetLoadingOverlay(true);
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
			
			FeatureControl.SetLoadingOverlay(false);
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
			string email = System.IsEmailValid(EmailInput.text, out _) ? EmailInput.text : string.Empty;
			FeatureControl.OpenForgotPasswordView(email);
		}
		
		private void OpenCreateAccountView()
		{
			FeatureControl.OpenCreateAccountView();
		}
	}
}
