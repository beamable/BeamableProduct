using Beamable.Api;
using Beamable.EasyFeatures.Components;
using Beamable.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicAccountManagement
{
	public class ForgotPasswordView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			BeamContext Context { get; set; }
			string Email { get; }
			bool IsEmailValid(string email, out string errorMessage);
			bool IsPasswordValid(string password, string confirmation, out string errorMessage);
		}

		private const string SEND_TEXT = "Send code to email";
		private const string CONFIRM_TEXT = "Confirm";
		
		public AccountManagementFeatureControl FeatureControl;
		public int EnrichOrder;

		public TMP_InputField EmailInput;
		public GameObject InfoLabel;
		public GameObject TryAgainLabel;
		public Button TryAgainButton;
		public GameObject HiddenFieldsGroup;
		public TMP_InputField CodeInput;
		public TMP_InputField PasswordInput;
		public TMP_InputField ConfirmPasswordInput;
		public ErrorMessageText ErrorText;
		public Button ConfirmButton;
		public TextMeshProUGUI ConfirmButtonLabel;
		public Button CancelButton;

		protected IDependencies System;

		private PasswordResetOperation _passwordResetOperation;
		
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

			EmailInput.text = System.Email;
			SetCodeSentState(false);
			
			FeatureControl.SetBackAction(GoBack);
			FeatureControl.SetHomeAction(OpenAccountsView);
			TryAgainButton.onClick.ReplaceOrAddListener(() => SetCodeSentState(false));
		}

		private void OpenAccountsView()
		{
			FeatureControl.OpenAccountsView();
		}

		private void GoBack()
		{
			throw new System.NotImplementedException();
		}

		private void SetCodeSentState(bool wasSent)
		{
			EmailInput.interactable = !wasSent;
			InfoLabel.SetActive(!wasSent);
			TryAgainLabel.SetActive(wasSent);
			HiddenFieldsGroup.SetActive(wasSent);
			ConfirmButtonLabel.text = wasSent ? CONFIRM_TEXT : SEND_TEXT;
			ErrorText.HideMessage();
			
			ConfirmButton.onClick.RemoveAllListeners();
			CancelButton.onClick.RemoveAllListeners();
			if (wasSent)
			{
				ConfirmButton.onClick.AddListener(ChangePassword);
				CancelButton.onClick.AddListener(() => SetCodeSentState(false));
			}
			else
			{
				ConfirmButton.onClick.AddListener(SendCode);
				CancelButton.onClick.AddListener(() => FeatureControl.OpenSignInView());
			}
		}

		private async void SendCode()
		{
			if (System.IsEmailValid(EmailInput.text, out string errorMessage))
			{
				ErrorText.HideMessage();
				FeatureControl.SetLoadingOverlay(true);
				_passwordResetOperation = await System.Context.Accounts.ResetPassword();

				if (_passwordResetOperation.isSuccess)
				{
					SetCodeSentState(true);	
				}
				else
				{
					switch (_passwordResetOperation.error)
					{
						case PasswordResetError.NO_EXISTING_CREDENTIAL:
							ErrorText.SetErrorMessage("Provided account does not exist");
							break;
						
						default:
							ErrorText.SetErrorMessage("Unknown error has occurred");
							break;
					}
				}
				
				FeatureControl.SetLoadingOverlay(false);
			}

			ErrorText.SetErrorMessage(errorMessage);
		}

		private async void ChangePassword()
		{
			string password = PasswordInput.text;
			string confirmation = ConfirmPasswordInput.text;
			if (System.IsPasswordValid(password, confirmation, out string errorMessage))
			{
				if (!string.IsNullOrWhiteSpace(CodeInput.text))
				{
					ErrorText.HideMessage();
					FeatureControl.SetLoadingOverlay(true);

					try
					{
						var confirm = await _passwordResetOperation.Confirm(CodeInput.text, password);
						if (confirm.isSuccess)
						{
							FeatureControl.OpenSignInView();
						}
						else
						{
							ErrorText.SetErrorMessage("Unknown error has occured");
						}
					}
					catch (PlatformRequesterException e)
					{
						if (e.Error.error == "InvalidConfirmationCodeError")
						{
							ErrorText.SetErrorMessage("Provided code is invalid");
						}
					}
					finally
					{
						FeatureControl.SetLoadingOverlay(false);	
					}
				}
				else
				{
					ErrorText.SetErrorMessage("You must provide reset code");
				}
			}
			else
			{
				ErrorText.SetErrorMessage(errorMessage);
			}
		}
	}
}
