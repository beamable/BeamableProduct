using Beamable.UI.Buss;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.Components
{
	public class BeamInputField : MonoBehaviour
	{
		private const string OFF_CLASS = "off";
		private const string VALID_CLASS = "valid";
		private const string INVALID_CLASS = "invalid";

		public bool AllowShowHideContent = false;
		public TMP_InputField InputField;
		public Button ShowHideContentButton;
		public BussElement EyeIconElement;
		public BussElement MainBussElement;
		public ErrorMessageText ErrorText;

		private ValidatorDelegate _validator;

		public delegate bool ValidatorDelegate(string input, out string errorMessage);

		public string text
		{
			get => InputField.text;
			set => InputField.text = value;
		}

		public bool interactable
		{
			get => InputField.interactable;
			set => InputField.interactable = value;
		}

		private void Start()
		{
			ShowHideContentButton.gameObject.SetActive(AllowShowHideContent);
			EyeIconElement.gameObject.SetActive(AllowShowHideContent);

			ShowHideContentButton.onClick.ReplaceOrAddListener(OnShowHidePressed);
			InputField.onEndEdit.ReplaceOrAddListener(input => Validate(input));

			UpdateContentVisibility();
		}

		/// <summary>
		/// Clears the input field and sets validator function. This method is not necessary if validator is not needed.
		/// </summary>
		/// <param name="validator">The function which will validate the content on each <see cref="TMP_InputField.onEndEdit"/> event.
		/// This validator is also used in <see cref="IsValid"/> method.</param>
		public void Setup(ValidatorDelegate validator)
		{
			Clear();
			_validator = validator;
		}

		/// <summary>
		/// Checks if the content is valid and sets proper state. The content is considered valid also when there is no provided validator.
		/// </summary>
		/// <returns>True if content is valid, false otherwise.</returns>
		public bool IsValid() => Validate(InputField.text);

		private bool Validate(string input)
		{
			if (_validator == null)
			{
				SetNormalState();
				return true;
			}

			var isValid = _validator(input, out string errorMessage);
			if (isValid)
			{
				SetValidState();
			}
			else
			{
				SetInvalidState(errorMessage);
			}

			return isValid;
		}

		private void OnShowHidePressed() => UpdateContentVisibility(true);

		private void UpdateContentVisibility(bool invert = false)
		{
			if (invert)
			{
				InputField.inputType = InputField.inputType == TMP_InputField.InputType.Password
					? TMP_InputField.InputType.Standard
					: TMP_InputField.InputType.Password;
				InputField.ForceLabelUpdate();
			}

			EyeIconElement.SetClass(OFF_CLASS, InputField.inputType == TMP_InputField.InputType.Password);
		}

		/// <summary>
		/// Changes styling of the input field to indicate that the value given by the player is incorrect
		/// </summary>
		/// <param name="errorMessage">An optional message to display below the input field</param>
		public void SetInvalidState(string errorMessage = "")
		{
			SetNormalState();
			MainBussElement.SetClass(INVALID_CLASS, true);
			EyeIconElement.SetClass(INVALID_CLASS, true);
			ErrorText.SetErrorMessage(errorMessage);
		}

		/// <summary>
		/// Changes styling of the input field to indicate that the value given by the player is valid
		/// </summary>
		public void SetValidState()
		{
			SetNormalState();
			MainBussElement.SetClass(VALID_CLASS, true);
			ErrorText.HideMessage();
		}

		/// <summary>
		/// Sets a default styling and clears the error message.
		/// </summary>
		public void SetNormalState()
		{
			MainBussElement.SetClass(VALID_CLASS, false);
			MainBussElement.SetClass(INVALID_CLASS, false);
			EyeIconElement.SetClass(INVALID_CLASS, false);
			ErrorText.HideMessage();
		}

		/// <summary>
		/// Resets the input field to the default styling and empty text
		/// </summary>
		public void Clear()
		{
			SetNormalState();
			text = string.Empty;
		}
	}
}
