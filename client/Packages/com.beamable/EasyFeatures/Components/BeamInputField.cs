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

		public string text
		{
			get => InputField.text;
			set => InputField.text = value;
		}

		private void Start()
		{
			ShowHideContentButton.gameObject.SetActive(AllowShowHideContent);
			EyeIconElement.gameObject.SetActive(AllowShowHideContent);
			
			ShowHideContentButton.onClick.ReplaceOrAddListener(OnShowHidePressed);
			
			UpdateContentVisibility();
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
