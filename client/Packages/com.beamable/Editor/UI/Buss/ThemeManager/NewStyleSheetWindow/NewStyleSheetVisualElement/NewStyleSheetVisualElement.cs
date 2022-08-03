using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Buss
{
	public class NewStyleSheetVisualElement : BeamableVisualElement
	{
		private const int LABEL_WIDTH = 160;

		// Can start exactly with two dashes ("--") OR with any letter 
		// Numbers and special characters are not valid
		private const string VARIABLE_NAME_REGEX = "^\\A(-{2}|[a-zA-Z])*$";
		
		private LabeledTextField _styleSheetName;
		private PrimaryButtonVisualElement _confirmButton;
		private readonly List<BussStyleRule> _initialRule;

		public NewStyleSheetVisualElement(List<BussStyleRule> initialRule) : base(
			$"{BUSS_THEME_MANAGER_PATH}/NewStyleSheetWindow/{nameof(NewStyleSheetVisualElement)}/{nameof(NewStyleSheetVisualElement)}")
		{
			_initialRule = initialRule;
		}

		public override void Refresh()
		{
			base.Refresh();

			_styleSheetName = Root.Q<LabeledTextField>("styleSheetName");
			_styleSheetName.Setup("Style sheet name", string.Empty, OnValidate);
			_styleSheetName.Refresh();
			_styleSheetName.OverrideLabelWidth(LABEL_WIDTH);

			_confirmButton = Root.Q<PrimaryButtonVisualElement>("confirmButton");
			_confirmButton.Button.clickable.clicked += HandleConfirmButton;

			GenericButtonVisualElement cancelButton = Root.Q<GenericButtonVisualElement>("cancelButton");
			cancelButton.OnClick += NewStyleSheetWindow.CloseWindow;

			OnValidate();
		}

		private void OnValidate()
		{
			if (!IsNameValid(out string message))
			{
				ChangeButtonState(false, message);
			}
			else
			{
				ChangeButtonState(true);
			}
		}

		private bool IsNameValid(out string message)
		{
			message = string.Empty;
			string variableName = _styleSheetName.Value;

			if (string.IsNullOrWhiteSpace(variableName))
			{
				message = "Variable name can't be empty";
				return false;
			}
			
			if (!Regex.IsMatch(variableName, VARIABLE_NAME_REGEX))
			{
				message = "Variable name can contain only letters";
				return false;
			}

			return true;
		}

		private void HandleConfirmButton()
		{
			if (string.IsNullOrWhiteSpace(_styleSheetName.Value))
			{
				Debug.LogError("Style sheet name cannot be empty!");
				return;
			}
			
			BussStyleSheetUtility.CreateNewStyleSheetWithInitialRules(_styleSheetName.Value, _initialRule);

			NewStyleSheetWindow.CloseWindow();
		}

		private void ChangeButtonState(bool isEnabled, string tooltip = "")
		{
			_confirmButton.tooltip = tooltip;
			if (isEnabled)
				_confirmButton.Enable();
			else
				_confirmButton.Disable();
		}
	}
}
