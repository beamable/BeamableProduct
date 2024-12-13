using Beamable.Editor.UI.Components;
using Beamable.BSAT.Core;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Beamable.BSAT.Editor.UI.Components
{
	public class CreateNewTestVisualElement : TestingToolComponent
	{
		public event Action<string> OnCreateButtonPressed;
		public event Action OnCloseButtonPressed;
		
		private LabeledTextField _testNameInput;
		private Button _create;
		private Button _cancel;
		
		private TestConfiguration _testConfiguration;

		private const string TEST_NAME_REGEX = "^[A-Z][A-Za-z]*$";
		public CreateNewTestVisualElement() : base(nameof(CreateNewTestVisualElement)) { }

		public void Init(TestConfiguration testConfiguration)
		{
			_testConfiguration = testConfiguration;
		}
		public override void Refresh()
		{
			base.Refresh();
			
			_testNameInput = Root.Q<LabeledTextField>("testNameInput");
			_testNameInput.Setup("Test name", string.Empty, s => HandleInputChanged(s));
			_testNameInput.Refresh();

			_create = Root.Q<Button>("create");
			_create.clickable.clicked -= HandleCreateButton;
			_create.clickable.clicked += HandleCreateButton;

			_cancel = Root.Q<Button>("cancel");
			_cancel.clickable.clicked -= HandleCancelButton;
			_cancel.clickable.clicked += HandleCancelButton;

			HandleInputChanged(String.Empty);
		}
		private void HandleCreateButton() => OnCreateButtonPressed?.Invoke(_testNameInput.Value);
		private void HandleCancelButton() => OnCloseButtonPressed?.Invoke();
		private void HandleInputChanged(string value)
		{
			_create.parent.tooltip = !IsValid(out var errorMessage) ? errorMessage : string.Empty;
			_create.SetEnabled(string.IsNullOrWhiteSpace(errorMessage));
		}

		private bool IsValid(out string errorMessage)
		{
			errorMessage = string.Empty;
			var input = _testNameInput.Value;
			
			if (input.Length < 3)
			{
				errorMessage += "- Test name must be at least 3 characters long\n";
			}
			if (!Regex.IsMatch(input, TEST_NAME_REGEX))
			{
				errorMessage += "- Test name must start with a capital letter and contain only letters\n";
			}
			if (_testConfiguration.RegisteredTestScenes.Any(x => x.SceneName == input))
			{
				errorMessage += "- Test name is already taken. Choose an other test name\n";
			}
			return string.IsNullOrWhiteSpace(errorMessage);
		}
	}
}
