using System;
using Beamable.Editor.Login.UI.Components;
using Beamable.Editor.UI.Components;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Login.UI.Components
{
   public class NewUserVisualElement : LoginBaseComponent
   {
      private TextField _cidOrAliasTextField;
      private TextField _emailTextField;
      private TextField _passwordTextField;
      private TextField _passwordConfirmTextField;
      private PrimaryButtonVisualElement _continueButton;
      private Button _legalButton;
      private Toggle _legalCheckbox;
      private Label _errorText;
      private Button _existingAccountButton;
      private Button _cancelButton;
      private Button _switchOrgButton;

      public NewUserVisualElement() : base(nameof(NewUserVisualElement))
      {
      }

      public override string GetMessage()
      {
         return "Create a new account. You will need to check with your Organization's administrator to receive full access to the project.";
      }


      public override void Refresh()
      {
         base.Refresh();

         _cidOrAliasTextField = Root.Q<TextField>("organizationID");
         _cidOrAliasTextField.AddPlaceholder(LoginBaseConstants.PLACEHOLDER_CID_FIELD);
         _cidOrAliasTextField.SetValueWithoutNotify(Model.Customer.CidOrAlias);
         var isAlias = _cidOrAliasTextField.AddErrorLabel("Alias", PrimaryButtonVisualElement.AliasErrorHandler);

         _emailTextField = Root.Q<TextField>("account");
         _emailTextField.AddPlaceholder(LoginBaseConstants.PLACEHOLDER_EMAIL_FIELD);
         var isEmail = _emailTextField.AddErrorLabel("Email", PrimaryButtonVisualElement.EmailErrorHandler);

         _passwordTextField = Root.Q<TextField>("password");
         _passwordTextField.AddPlaceholder(LoginBaseConstants.PLACEHOLDER_PASSWORD_FIELD);
         _passwordTextField.isPasswordField = true;
         var isPasswordValid = _passwordTextField.AddErrorLabel("Password", PrimaryButtonVisualElement.PasswordErrorHandler);


         _passwordConfirmTextField = Root.Q<TextField>("confirmPassword");
         _passwordConfirmTextField.AddPlaceholder(LoginBaseConstants.PLACEHOLDER_PASSWORD_CONFIRM_FIELD);
         _passwordConfirmTextField.isPasswordField = true;
         var doPasswordsMatch = _passwordConfirmTextField.AddErrorLabel("Password Match", m => m != _passwordTextField.value
            ? "Passwords don't match"
            : null);

         _legalCheckbox = Root.Q<Toggle>();
         _legalCheckbox.SetValueWithoutNotify(Model.ReadLegalCopy);
         _legalCheckbox.RegisterValueChangedCallback(evt => Model.ReadLegalCopy = evt.newValue);
         var isLegal = _legalCheckbox.AddErrorLabel("Legal", PrimaryButtonVisualElement.LegalErrorHandler);

         _continueButton = Root.Q<PrimaryButtonVisualElement>("signIn");
         _continueButton.Button.clickable.clicked += Continue_OnClicked;
         _continueButton.AddGateKeeper(isAlias, isEmail, isLegal, isPasswordValid, doPasswordsMatch);

         _legalButton = Root.Q<Button>("legalButton");
         _legalButton.clickable.clicked +=() => { Application.OpenURL(BeamableConstants.BEAMABLE_LEGAL_WEBSITE); };

         _existingAccountButton = Root.Q<Button>("existingAccount");
         _existingAccountButton.clickable.clicked += Manager.GotoExistingCustomer;

//         _switchOrgButton = Root.Q<Button>("newOrganization");
//         _switchOrgButton.clickable.clicked += Manager.GotoCustomerSelection;

         _errorText = Root.Q<Label>("errorLabel");
         _errorText.AddTextWrapStyle();
         _errorText.text = "";
      }

      private void Continue_OnClicked()
      {
         Model.Customer.SetExistingCustomerData(_cidOrAliasTextField.value, _emailTextField.value, _passwordTextField.value);
         var promise = Manager.AttemptNewUser(Model);
         _continueButton.Load(AddErrorLabel(promise, _errorText));
      }
   }
}