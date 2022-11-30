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
		public TextMeshProUGUI ErrorText;
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

			ErrorText.text = "";
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
			throw new System.NotImplementedException();
		}

		private void OpenSignInView()
		{
			FeatureControl.OpenSignInView();
		}

		private void OnSignUpPressed()
		{
			string email = EmailInput.text;
			string password = PasswordInput.text;
			string confirmation = ConfirmPasswordInput.text;
			if (System.IsAccountDataValid(email, password, confirmation, out string errorMessage))
			{
				// create an account
			}
			
			ErrorText.text = errorMessage;
		}
	}
}
