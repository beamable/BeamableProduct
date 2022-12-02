using Beamable.EasyFeatures.Components;
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
			
			SetCodeSentState(false);
			
			FeatureControl.SetBackAction(GoBack);
			FeatureControl.SetHomeAction(OpenAccountsView);
			TryAgainButton.onClick.ReplaceOrAddListener(SendCode);
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
			ErrorText.SetErrorMessage("");
			
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

		private void SendCode()
		{
			if (System.IsEmailValid(EmailInput.text, out string errorMessage))
			{
				SetCodeSentState(true);
				// send code to email
				throw new System.NotImplementedException();
			}

			ErrorText.SetErrorMessage(errorMessage);
		}

		private void ChangePassword()
		{
			string email = EmailInput.text;
			string password = PasswordInput.text;
			string confirmation = ConfirmPasswordInput.text;
			if (System.IsPasswordValid(password, confirmation, out string errorMessage))
			{
				// change password if the code matches
				throw new System.NotImplementedException();
			}

			ErrorText.SetErrorMessage(errorMessage);
		}
	}
}
