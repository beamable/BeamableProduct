using Beamable.UI.Buss;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.Components
{
	public class BeamInputField : MonoBehaviour
	{
		private const string OFF_CLASS = "off";

		public bool AllowShowHideContent = false;
		public TMP_InputField InputField;
		public Button ShowHideContentButton;
		public BussElement EyeIconElement;

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
			
			EyeIconElement.SetClass(OFF_CLASS, InputField.inputType != TMP_InputField.InputType.Password);
		}
	}
}
