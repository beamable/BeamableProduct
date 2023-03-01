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
			bool IsPasswordValid(string password, out string errorMessage);
			bool IsPasswordValid(string password, string confirmation, out string errorMessage);
			bool IsResetCodeValid(string code, out string errorMessage);
		}

		private const string SEND_TEXT = "Send code to email";
		private const string CONFIRM_TEXT = "Confirm";
		
		public AccountManagementFeatureControl FeatureControl;
		public int EnrichOrder;

		public BeamInputField EmailInput;
		public GameObject InfoLabel;
		public GameObject TryAgainLabel;
		public Button TryAgainButton;
		public GameObject HiddenFieldsGroup;
		public BeamInputField CodeInput;
		public BeamInputField PasswordInput;
		public BeamInputField ConfirmPasswordInput;
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

			CodeInput.Setup((string input, out string errormessage) => System.IsResetCodeValid(input, out errormessage));
			EmailInput.Setup((string input, out string errorMessage) => System.IsEmailValid(input, out errorMessage));
			EmailInput.text = System.Email;
			PasswordInput.Setup((string input, out string errorMessage) => System.IsPasswordValid(input, out errorMessage));
			ConfirmPasswordInput.Setup((string input, out string errorMessage) => System.IsPasswordValid(PasswordInput.text, input, out errorMessage));
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
			CodeInput.Clear();
			PasswordInput.Clear();
			ConfirmPasswordInput.Clear();
			
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
			if (!EmailInput.IsValid())
			{
				return;
			}
			
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
						EmailInput.SetInvalidState("Provided account does not exist");
						break;
						
					default:
						EmailInput.SetInvalidState();
						PasswordInput.SetInvalidState();
						ConfirmPasswordInput.SetInvalidState("Unknown error has occurred");
						break;
				}
			}
				
			FeatureControl.SetLoadingOverlay(false);
		}

		private async void ChangePassword()
		{
			if (!CodeInput.IsValid() || !PasswordInput.IsValid() || !ConfirmPasswordInput.IsValid())
			{
				return;
			}

			string code = CodeInput.text;
			string password = PasswordInput.text;
			
			FeatureControl.SetLoadingOverlay(true);
			
			try
			{
				var confirm = await _passwordResetOperation.Confirm(code, password);
				if (confirm.isSuccess)
				{
					FeatureControl.OpenSignInView();
				}
				else
				{
					CodeInput.SetInvalidState("Unknown error has occured");
				}
			}
			catch (PlatformRequesterException e)
			{
				if (e.Error.error == "InvalidConfirmationCodeError")
				{
					CodeInput.SetInvalidState("Provided code is invalid");
				}
			}
			finally
			{
				FeatureControl.SetLoadingOverlay(false);	
			}
		}
	}
}
