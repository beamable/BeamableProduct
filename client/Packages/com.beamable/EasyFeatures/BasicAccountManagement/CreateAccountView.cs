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
			bool IsPasswordValid(string password, string confirmation, out string errorMessage);
		}
		
		public AccountManagementFeatureControl FeatureControl;
		public int EnrichOrder;

		public BeamInputField EmailInput;
		public BeamInputField PasswordInput;
		public BeamInputField ConfirmPasswordInput;
		public Button SignUpButton;
		public Button SignInButton;

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

			EmailInput.Clear();
			EmailInput.text = System.Email;
			PasswordInput.Clear();
			ConfirmPasswordInput.Clear();
			
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
			string email = EmailInput.text;
			string password = PasswordInput.text;
			string confirmation = ConfirmPasswordInput.text;

			if (!System.IsEmailValid(email, out string emailError))
			{
				EmailInput.SetInvalidState(emailError);
				return;
			}
			EmailInput.SetValidState();

			if (!System.IsPasswordValid(password, confirmation, out string passwordError))
			{
				PasswordInput.SetInvalidState();
				ConfirmPasswordInput.SetInvalidState(passwordError);
				return;
			}
			PasswordInput.SetValidState();
			ConfirmPasswordInput.SetValidState();
			
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
