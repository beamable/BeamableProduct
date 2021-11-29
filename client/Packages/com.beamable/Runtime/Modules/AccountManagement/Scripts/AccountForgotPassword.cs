using System.Collections.Generic;
using Beamable.UI.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.AccountManagement
{
    public class AccountForgotPassword : MenuBase
    {
        public ForgotPasswordArguments Arguments;

        public GameObject SendEmailContainer, ConfirmContainer;

        public TextReferenceBase ErrorText;

        public Button ContinueButton;
        public List<InputValidationBehaviour> ValidationBehaviours;

        // Start is called before the first frame update
        void Start()
        {
            ErrorText.Value = "";
            SetForSendEmail();
        }

        // Update is called once per frame
        void Update()
        {
            ContinueButton.interactable = ValidationBehaviours.TrueForAll(v => v.IsValid || !v.isActiveAndEnabled);
        }

        public override void OnOpened()
        {
            Arguments.Password.Value = "";
            Arguments.Code.Value = "";
        }

        public void SetEmail(string email)
        {
            Arguments.Email.Value = email;
        }

        public void SetForConfirm()
        {
            SendEmailContainer.SetActive(false);
            ConfirmContainer.SetActive(true);
        }

        public void SetForSendEmail()
        {
            SendEmailContainer.SetActive(true);
            ConfirmContainer.SetActive(false);
        }
    }
}