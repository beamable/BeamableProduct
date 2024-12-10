using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Beamable.BSAT.Editor.UI.Components
{
	public class ActionBarVisualElement : TestingToolComponent
	{
		public event Action OnScanButtonPressed;
		public event Action OnCreateNewTestSceneButtonPressed;
		public event Action OnDeleteTestSceneButtonPressed;
		public event Action OnGenerateReportButtonPressed;
		public event Action OnOpenMainMenuSceneButtonPressed;
		public event Action OnSetupBuildSettingsButtonPressed;

		private Button _scanButton;
		private Button _createTestSceneButton;
		private Button _deleteTestSceneButton;
		private Button _generateReportButton;
		private Button _openMainMenuSceneButton;
		private Button _setupBuildSettingsButton;

		public ActionBarVisualElement() : base(nameof(ActionBarVisualElement)) { }

		public override void Refresh()
		{
			base.Refresh();

			_scanButton = Root.Q<Button>("scan");
			_scanButton.clickable.clicked -= HandleScanButtonPressed;
			_scanButton.clickable.clicked += HandleScanButtonPressed;

			_createTestSceneButton = Root.Q<Button>("createTestScene");
			_createTestSceneButton.clickable.clicked -= HandleCreateTestSceneButton;
			_createTestSceneButton.clickable.clicked += HandleCreateTestSceneButton;

			_deleteTestSceneButton = Root.Q<Button>("deleteTestScene");
			_deleteTestSceneButton.clickable.clicked -= HandleDeleteTestSceneButton;
			_deleteTestSceneButton.clickable.clicked += HandleDeleteTestSceneButton;

			_generateReportButton = Root.Q<Button>("generateReport");
			_generateReportButton.clickable.clicked -= HandleReportGenerateButton;
			_generateReportButton.clickable.clicked += HandleReportGenerateButton;

			_openMainMenuSceneButton = Root.Q<Button>("openMainMenuScene");
			_openMainMenuSceneButton.clickable.clicked -= HandleOpenMainMenuSceneButton;
			_openMainMenuSceneButton.clickable.clicked += HandleOpenMainMenuSceneButton;
			
			_setupBuildSettingsButton = Root.Q<Button>("setupBuildSetting");
			_setupBuildSettingsButton.clickable.clicked -= HandleSetupBuildSettingsButton;
			_setupBuildSettingsButton.clickable.clicked += HandleSetupBuildSettingsButton;
		}

		private void HandleScanButtonPressed() => OnScanButtonPressed?.Invoke();
		private void HandleCreateTestSceneButton() => OnCreateNewTestSceneButtonPressed?.Invoke();
		private void HandleDeleteTestSceneButton() => OnDeleteTestSceneButtonPressed?.Invoke();
		private void HandleReportGenerateButton() => OnGenerateReportButtonPressed?.Invoke();
		private void HandleOpenMainMenuSceneButton() => OnOpenMainMenuSceneButtonPressed?.Invoke();
		private void HandleSetupBuildSettingsButton() => OnSetupBuildSettingsButtonPressed?.Invoke();
	}
}
