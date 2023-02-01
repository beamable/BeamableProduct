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
			bool IsAccountDataValid(string email, string password, string confirmPassword, out string errorMessage);
		}
		
		public AccountManagementFeatureControl FeatureControl;
		public int EnrichOrder;

		public TMP_InputField EmailInput;
		public TMP_InputField PasswordInput;
		public TMP_InputField ConfirmPasswordInput;
		public ErrorMessageText ErrorText;
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

			ErrorText.HideMessage();
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
			if (System.IsAccountDataValid(email, password, confirmation, out string errorMessage))
			{
				var result = await System.Context.Accounts.AddEmail(email, password);
				if (result.isSuccess)
				{
					ErrorText.HideMessage();

					await result.account.SwitchToAccount();
					FeatureControl.OpenAccountsView();
				}
				else
				{
					switch (result.error)
					{
						case PlayerRegistrationError.ALREADY_HAS_CREDENTIAL:
						case PlayerRegistrationError.CREDENTIAL_IS_ALREADY_TAKEN:
							ErrorText.SetErrorMessage("Account already exists");
							break;
						
						default:
							ErrorText.SetErrorMessage("Unknown error has occured");
							break;
					}
				}
			}
			else
			{
				ErrorText.SetErrorMessage(errorMessage);
			}
		}
	}
}
