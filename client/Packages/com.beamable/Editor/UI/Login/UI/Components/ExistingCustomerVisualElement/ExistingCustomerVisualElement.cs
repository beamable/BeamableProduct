using Beamable.Common;
using Beamable.Editor.Login.UI.Components;
using Beamable.Editor.UI.Components;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Login.UI.Components
{
   public class ExistingCustomerVisualElement : LoginBaseComponent
   {
      private Button _switchCustomerButton;
      private Button _cancelButton;
      private TextField _cidTextField;
      private PrimaryButtonVisualElement _continueButton;
      private TextField _emailTextField;
      private TextField _passwordTextField;
      private Label _errorText;
      private Button _newUserButton;
      private Button _forgotPasswordButton;

      public ExistingCustomerVisualElement() : base(nameof(ExistingCustomerVisualElement))
      {
      }

      public override string GetMessage()
      {
         return "Welcome to Beamable. Please sign into your account.";
      }

      public override void Refresh()
      {
         base.Refresh();

         _switchCustomerButton = Root.Q<Button>("newOrganization");
         _switchCustomerButton.clickable.clicked += Manager.GotoNewCustomer;

         _forgotPasswordButton = Root.Q<Button>("forgotPassword");
         _forgotPasswordButton.clickable.clicked += () =>
         {
            Model.Customer.SetExistingCustomerData(_cidTextField.value, _emailTextField.value, null);
            Manager.GotoForgotPassword();
         };


         _newUserButton = Root.Q<Button>("createNewLink");
         _newUserButton.clickable.clicked += Manager.GotoNewUser;

         _continueButton = Root.Q<PrimaryButtonVisualElement>("signIn");
         _continueButton.Button.clickable.clicked += Continue_OnClicked;
         _continueButton.tooltip = "Enter all Data";

         _cidTextField = Root.Q<TextField>("organizationID");
         _cidTextField.AddPlaceholder(LoginBaseConstants.PLACEHOLDER_CID_FIELD);
         var isAlias = _cidTextField.AddErrorLabel("Alias", PrimaryButtonVisualElement.AliasOrCidErrorHandler);

         _emailTextField = Root.Q<TextField>("account");
         _emailTextField.AddPlaceholder(LoginBaseConstants.PLACEHOLDER_EMAIL_FIELD);
         var isEmail = _emailTextField.AddErrorLabel("Email", PrimaryButtonVisualElement.EmailErrorHandler);


         _passwordTextField = Root.Q<TextField>("password");
         _passwordTextField.AddPlaceholder(LoginBaseConstants.PLACEHOLDER_PASSWORD_FIELD);
         _passwordTextField.isPasswordField = true;
         var isPassword = _passwordTextField.AddErrorLabel("Password", m => { return null; });

         _cidTextField.SetValueWithoutNotify(Model.Customer.CidOrAlias);
         _emailTextField.SetValueWithoutNotify(Model.Customer.Email);

         _continueButton.AddGateKeeper(isAlias, isEmail, isPassword);

         _errorText = Root.Q<Label>("errorLabel");
         _errorText.AddTextWrapStyle();
         _errorText.text = "";


      }

      private void Continue_OnClicked()
      {
         _errorText.text = "";
         Model.Customer.SetExistingCustomerData(_cidTextField.value, _emailTextField.value, _passwordTextField.value);
         var promise = Manager.AttemptLoginExistingCustomer(Model);
         _continueButton.Load(AddErrorLabel(promise, _errorText));

      }
   }
}