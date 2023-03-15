using Beamable.EasyFeatures.Components;
using Beamable.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicAccountManagement
{
	public class CreateAccountView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			BeamContext Context { get; set; }
			string Email { get; }
			bool IsEmailValid(string email, out string errorMessage);
			bool IsPasswordValid(string password, out string errorMessage);
			bool IsPasswordValid(string password, string confirmation, out string errorMessage);
		}
		
		public AccountManagementFeatureControl FeatureControl;
		public int EnrichOrder;

		public BeamInputField EmailInput;
		public BeamInputField PasswordInput;
		public BeamInputField ConfirmPasswordInput;
		public Button SignUpButton;
		public Button SignInButton;
		public ThirdPartyLoginUI ThirdPartyLogin;

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

			ThirdPartyLogin.Setup(System.Context);
			
			EmailInput.Setup((string input, out string errorMessage) => System.IsEmailValid(input, out errorMessage));
			EmailInput.text = System.Email;
			PasswordInput.Setup((string input, out string errorMessage) => System.IsPasswordValid(input, out errorMessage));
			ConfirmPasswordInput.Setup((string input, out string errorMessage) => System.IsPasswordValid(PasswordInput.text, input, out errorMessage));
			
			FeatureControl.SetBackAction(GoBack);
			FeatureControl.SetHomeAction(OpenAccountsView);
			SignUpButton.onClick.ReplaceOrAddListener(OnSignUpPressed);
			SignInButton.onClick.ReplaceOrAddListener(OpenSignInView);
		}

		private void OpenAccountsView()
		{
			FeatureControl.OpenAccountsView();
		}

		private void GoBack()
		{
			FeatureControl.OpenAccountsView();
		}

		private void OpenSignInView()
		{
			FeatureControl.OpenSignInView();
		}

		private async void OnSignUpPressed()
		{
			if (!EmailInput.IsValid() || !PasswordInput.IsValid() || !ConfirmPasswordInput.IsValid())
			{
				return;
			}
			
			string email = EmailInput.text;
			string password = PasswordInput.text;

			FeatureControl.SetLoadingOverlay(true);
			var result = await System.Context.Accounts.AddEmail(email, password);
			if (result.isSuccess)
			{
				await result.account.SwitchToAccount();
				FeatureControl.OpenAccountsView();
			}
			else
			{
				switch (result.error)
				{
					case PlayerRegistrationError.ALREADY_HAS_CREDENTIAL:
					case PlayerRegistrationError.CREDENTIAL_IS_ALREADY_TAKEN:
						EmailInput.SetInvalidState("Account already exists");
						break;
						
					default:
						EmailInput.SetInvalidState();
						PasswordInput.SetInvalidState();
						ConfirmPasswordInput.SetInvalidState("Unknown error has occured");
						break;
				}
			}
				
			FeatureControl.SetLoadingOverlay(false);
		}
	}
}
