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
   public class NewCustomerVisualElement : LoginBaseComponent
   {
      private Toggle _legalCheckbox;
      private TextField _cidTextField;
      private TextField _gameNameField;
      private TextField _emailField;
      private TextField _passwordField;
      private TextField _passwordConfField;
      private Button _legalButton;
      private Button _cancelButton;
      private Button _switchCustomerButton;
      private Label _errorText;
      private PrimaryButtonVisualElement _continueButton;

      public NewCustomerVisualElement() : base(nameof(NewCustomerVisualElement))
      {
      }

      public override string GetMessage()
      {
         return "Welcome to Beamable! Create an organization and your first Beamable game.";
      }

      public override void Refresh()
      {
         base.Refresh();

         _cidTextField = Root.Q<TextField>("organizationID");
         _cidTextField.AddPlaceholder(LoginBaseConstants.PLACEHOLDER_ALIAS_FIELD);
         var isAlias = _cidTextField.AddErrorLabel("Alias", PrimaryButtonVisualElement.AliasErrorHandler);

         _gameNameField = Root.Q<TextField>("projectID");
         _gameNameField.AddPlaceholder(LoginBaseConstants.PLACEHOLDER_GAMENAME_FIELD);
         var isGame = _gameNameField.AddErrorLabel("Game", m => m.Length > 0
            ? null
            : "Game Name Required");

         _emailField = Root.Q<TextField>("account");
         _emailField.AddPlaceholder(LoginBaseConstants.PLACEHOLDER_EMAIL_FIELD);
         var isEmail = _emailField.AddErrorLabel("Email", PrimaryButtonVisualElement.EmailErrorHandler);

         _passwordField = Root.Q<TextField>("password");
         _passwordField.AddPlaceholder(LoginBaseConstants.PLACEHOLDER_PASSWORD_FIELD);
         _passwordField.isPasswordField = true;
         var isPasswordValid = _passwordField.AddErrorLabel("Password", m => PrimaryButtonVisualElement.IsPassword(m)
            ? null
            : "A valid password must be at least 4 characters long");

         _passwordConfField = Root.Q<TextField>("confirmPassword");
         _passwordConfField.AddPlaceholder(LoginBaseConstants.PLACEHOLDER_PASSWORD_CONFIRM_FIELD);
         _passwordConfField.isPasswordField = true;
         var doPasswordsMatch = _passwordConfField.AddErrorLabel("Password Match",  m => m != _passwordField.value
                                                                                                ? "Passwords don't match"
                                                                                                : null);
         _legalCheckbox = Root.Q<Toggle>();
         _legalCheckbox.SetValueWithoutNotify(Model.ReadLegalCopy);
         _legalCheckbox.RegisterValueChangedCallback(evt => Model.ReadLegalCopy = evt.newValue);
         var isLegal = _legalCheckbox.AddErrorLabel("Legal", PrimaryButtonVisualElement.LegalErrorHandler);

         _legalButton = Root.Q<Button>("legalButton");
         _legalButton.clickable.clicked +=() => { Application.OpenURL(BeamableConstants.BEAMABLE_LEGAL_WEBSITE); };
         
         

         _continueButton = Root.Q<PrimaryButtonVisualElement>();
         _continueButton.Button.clickable.clicked += CreateCustomer_OnClicked;

         _continueButton.AddGateKeeper(doPasswordsMatch, isPasswordValid, isEmail, isAlias, isGame, isLegal);

         _switchCustomerButton = Root.Q<Button>("existingOrganization");
         _switchCustomerButton.clickable.clicked += Manager.GotoExistingCustomer;

         _errorText = Root.Q<Label>("errorLabel");
         _errorText.AddTextWrapStyle();
         _errorText.text = "";
      }

      private void CreateCustomer_OnClicked()
      {
         Model.Customer.SetNewCustomer(_cidTextField.value, _gameNameField.value, _emailField.value, _passwordField.value);
         var promise = Manager.AttemptNewCustomer(Model);
         _continueButton.Load(AddErrorLabel(promise, _errorText));
      }
   }
}