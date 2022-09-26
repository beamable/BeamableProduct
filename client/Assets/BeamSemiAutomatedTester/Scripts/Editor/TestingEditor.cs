using Beamable.BSAT.Editor.Models;
using Beamable.BSAT.Editor.Models.Lists;
using Beamable.BSAT.Editor.UI.Components;
using Beamable.Editor.UI;
using Beamable.Editor.UI.Common;
using Beamable.BSAT.Core.Models;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;
using ActionBarVisualElement = Beamable.BSAT.Editor.UI.Components.ActionBarVisualElement;

using static Beamable.BSAT.Constants.TestConstants.General;
using static Beamable.BSAT.Constants.TestConstants.Paths;

namespace Beamable.BSAT.Editor.UI
{
	public class TestingEditor : BeamEditorWindow<TestingEditor>
	{
		private TestingEditorModel TestingEditorModel
		{
			get
			{
				if (_testingEditorModel == null)
					_testingEditorModel = new TestingEditorModel();
				return _testingEditorModel;
			}
		}

		private TestingEditorModel _testingEditorModel;

		private VisualElement _windowRoot;
		private VisualElement _windowMain;

		private RegisteredTestRuleMethodVisualElement _ruleMethodBody;

		private VisualElement _scenesList;
		private VisualElement _testablesList;
		private VisualElement _rulesList;

		private ExtendedListView _scenesListView;
		private ExtendedListView _testablesListView;
		private ExtendedListView _rulesListView;

		private RegisteredTestSceneListModel _registeredTestScenesListModel;
		private RegisteredTestListModel _registeredTestListModel;
		private RegisteredTestRuleListModel _registeredTestRuleListModel;

		private ActionBarVisualElement _actionBarVisualElement;
		private CreateNewTestVisualElement _createNewTestVisualElement;

		static TestingEditor()
		{
			WindowDefaultConfig = new BeamEditorWindowInitConfig()
			{
				Title = "Testing Editor",
				DockPreferenceTypeName = typeof(SceneView).AssemblyQualifiedName,
				FocusOnShow = true,
				RequireLoggedUser = false,
			};
		}

		[MenuItem("Testing tool/Testing Editor")]
		public static async void Init() => await GetFullyInitializedWindow();

		protected override void Build()
		{
			SetForContent();
		}

		private void SetForContent()
		{
			var root = this.GetRootVisualContainer();
			root.Clear();

			var uiAsset =
				AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
					$"Assets/{TESTING_TOOL_NAME}/Scripts/Editor/TestingEditor.uxml");
			_windowRoot = uiAsset.CloneTree();
			_windowRoot.AddStyleSheet($"Assets/{TESTING_TOOL_NAME}/Scripts/Editor/TestingEditor.uss");
			_windowRoot.name = nameof(_windowRoot);
			_windowRoot.style.flexGrow = 1;

			root.style.flexGrow = 1;
			root.Add(_windowRoot);

			_windowMain = root.Q("window-main");

			_scenesList = root.Q("scenesList");
			_testablesList = root.Q("testablesList");
			_rulesList = root.Q("rulesList");

			_actionBarVisualElement = root.Q<ActionBarVisualElement>("actionBarVisualElement");

			_actionBarVisualElement.OnScanButtonPressed -= HandleScanButton;
			_actionBarVisualElement.OnScanButtonPressed += HandleScanButton;

			_actionBarVisualElement.OnCreateNewTestSceneButtonPressed -= HandleCreateTestSceneButton;
			_actionBarVisualElement.OnCreateNewTestSceneButtonPressed += HandleCreateTestSceneButton;

			_actionBarVisualElement.OnDeleteTestSceneButtonPressed -= HandleDeleteTestSceneButton;
			_actionBarVisualElement.OnDeleteTestSceneButtonPressed += HandleDeleteTestSceneButton;

			_actionBarVisualElement.OnGenerateReportButtonPressed -= HandleGenerateReportButton;
			_actionBarVisualElement.OnGenerateReportButtonPressed += HandleGenerateReportButton;
			
			_actionBarVisualElement.OnOpenMainMenuSceneButtonPressed -= HandleOpenMainMenuSceneButton;
			_actionBarVisualElement.OnOpenMainMenuSceneButtonPressed += HandleOpenMainMenuSceneButton;

			_actionBarVisualElement.Refresh();

			_registeredTestScenesListModel = new RegisteredTestSceneListModel(TestingEditorModel);
			_registeredTestListModel = new RegisteredTestListModel(TestingEditorModel);
			_registeredTestRuleListModel = new RegisteredTestRuleListModel(TestingEditorModel);

			_ruleMethodBody = root.Q<RegisteredTestRuleMethodVisualElement>("ruleMethodBody");
			_ruleMethodBody.Refresh();

			_testingEditorModel.TestConfiguration.OnTestFinished -= HandleTestFinished;
			_testingEditorModel.TestConfiguration.OnTestFinished += HandleTestFinished;

			EditorApplication.delayCall += HandleScanButton;
		}

		private void HandleTestFinished()
		{
			// _rulesListView?.RefreshPolyfill();
			// _testablesListView?.RefreshPolyfill();
			// _scenesListView?.RefreshPolyfill();
		}

		private void CreateRegisteredTestScenesList()
		{
			ResetList(_rulesList, ref _rulesListView);
			ResetList(_testablesList, ref _testablesListView);
			ResetList(_scenesList, ref _scenesListView);

			_registeredTestScenesListModel.OnSelectionChanged -= CreateRegisteredTestsList;
			_scenesListView =
				_registeredTestScenesListModel.CreateListView(
					TestingEditorModel.TestConfiguration.RegisteredTestScenes);
			_registeredTestScenesListModel.OnSelectionChanged += CreateRegisteredTestsList;
			_scenesList.Add(_scenesListView);
		}

		private void CreateRegisteredTestsList(RegisteredTestScene registeredTestScene)
		{
			ResetList(_rulesList, ref _rulesListView);
			ResetList(_testablesList, ref _testablesListView);

			_registeredTestListModel.OnSelectionChanged -= CreateRegisteredTestRulesList;
			_testablesListView = _registeredTestListModel.CreateListView(registeredTestScene.RegisteredTests);
			_registeredTestListModel.OnSelectionChanged += CreateRegisteredTestRulesList;
			_testablesList.Add(_testablesListView);
		}

		private void CreateRegisteredTestRulesList(RegisteredTest registeredTest)
		{
			ResetList(_rulesList, ref _rulesListView);

			_registeredTestRuleListModel.OnSelectionChanged -= SetupDetailedInfo;
			_rulesListView = _registeredTestRuleListModel.CreateListView(registeredTest.RegisteredTestRules);
			_registeredTestRuleListModel.OnSelectionChanged += SetupDetailedInfo;
			_rulesList.Add(_rulesListView);
		}

		private void SetupDetailedInfo(RegisteredTestRule registeredTestRule) =>
			_ruleMethodBody.Setup(TestingEditorModel.TestConfiguration,
			                      registeredTestRule.RegisteredTestRuleMethods[0]);

		private void HandleScanButton()
		{
			TestingEditorModel.Scan();
			CreateRegisteredTestScenesList();
		}

		private void HandleCreateTestSceneButton()
		{
			if (_createNewTestVisualElement != null)
				return;

			_createNewTestVisualElement = new CreateNewTestVisualElement();
			_createNewTestVisualElement.Init(TestingEditorModel.TestConfiguration);

			_createNewTestVisualElement.OnCreateButtonPressed += testName =>
			{
				_createNewTestVisualElement.SetEnabled(false);
				TestingEditorModel.CreateTestScene(testName);
				_windowMain.Remove(_createNewTestVisualElement);
				_createNewTestVisualElement = null;
			};
			_createNewTestVisualElement.OnCloseButtonPressed += () =>
			{
				_windowMain.Remove(_createNewTestVisualElement);
				_createNewTestVisualElement = null;
			};

			_windowMain.Insert(1, _createNewTestVisualElement);
			_createNewTestVisualElement.Refresh();
		}

		private void HandleDeleteTestSceneButton()
		{
			if (TestingEditorModel.SelectedRegisteredTestScene == null)
				return;

			var isDeleteConfirmed = EditorUtility.DisplayDialog(
				"Confirm deletion",
				$"Are you sure you want to delete the Test=[{TestingEditorModel.SelectedRegisteredTestScene.SceneName}]?",
				"Confirm",
				"Cancel");

			if (!isDeleteConfirmed)
				return;

			TestingEditorModel.DeleteTestScene(TestingEditorModel.SelectedRegisteredTestScene);
			HandleScanButton();
		}

		private void HandleGenerateReportButton() => TestingEditorModel.GenerateReport();

		private void HandleOpenMainMenuSceneButton()
		{
			EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
			EditorSceneManager.OpenScene(GetPathToMainMenuTestScene());
		}

		private void ResetList(VisualElement ve, ref ExtendedListView elv)
		{
			if (elv == null)
				return;
			ve.Remove(elv);
			elv = null;

			_ruleMethodBody.ClearData();
		}
	}
}
