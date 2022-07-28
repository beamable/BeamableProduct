using Beamable.Editor.NewTestingTool.Models;
using Beamable.Editor.NewTestingTool.Models.Lists;
using Beamable.Editor.UI;
using Beamable.Editor.UI.Common;
using Beamable.NewTestingTool.Core.Models;
using UnityEditor;
using UnityEngine.UIElements;
using ActionBarVisualElement = Beamable.Editor.NewTestingTool.UI.Components.ActionBarVisualElement;

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
	
	[MenuItem("TestingTool/Testing Editor")]
	public static async void Init() => await GetFullyInitializedWindow();
	
	protected override void Build()
	{
		SetForContent();
	}
	private void SetForContent()
	{
		var root = this.GetRootVisualContainer();
		root.Clear();

		var uiAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"Assets/NewTestingTool/Scripts/Editor/TestingEditor.uxml");
		_windowRoot = uiAsset.CloneTree();
		_windowRoot.AddStyleSheet($"Assets/NewTestingTool/Scripts/Editor/TestingEditor.uss");
		_windowRoot.name = nameof(_windowRoot);
		_windowRoot.style.flexGrow = 1;

		root.style.flexGrow = 1;
		root.Add(_windowRoot);

		_scenesList = root.Q("scenesList");
		_testablesList = root.Q("testablesList");
		_rulesList = root.Q("rulesList");

		var btn = root.Q<Button>("findTestsButton");
		btn.clicked -= HandleScanRequest;
		btn.clicked += HandleScanRequest;
		
		_actionBarVisualElement = root.Q<ActionBarVisualElement>("actionBarVisualElement");
		_actionBarVisualElement.Init(TestingEditorModel);
		_actionBarVisualElement.Refresh();

		_registeredTestScenesListModel = new RegisteredTestSceneListModel(TestingEditorModel);
		_registeredTestListModel = new RegisteredTestListModel(TestingEditorModel);
		_registeredTestRuleListModel = new RegisteredTestRuleListModel(TestingEditorModel);
		
		HandleScanRequest();
		CreateRegisteredTestScenesList();
	}
	
	private void CreateRegisteredTestScenesList()
	{
		ResetList(_rulesList, ref _rulesListView);
		ResetList(_testablesList, ref _testablesListView);
		ResetList(_scenesList, ref _scenesListView);
		
		_scenesListView = _registeredTestScenesListModel.CreateListView(TestingEditorModel.TestConfiguration.RegisteredTestScenes);
		_registeredTestScenesListModel.OnItemChosen += CreateRegisteredTestsList;
		_registeredTestScenesListModel.OnSelectionChanged += CreateRegisteredTestsList;
		_scenesList.Add(_scenesListView);
	}
	private void CreateRegisteredTestsList(RegisteredTestScene registeredTestScene)
	{
		ResetList(_rulesList, ref _rulesListView);
		ResetList(_testablesList, ref _testablesListView);
		
		_testablesListView = _registeredTestListModel.CreateListView(registeredTestScene.RegisteredTests);
		_registeredTestListModel.OnItemChosen += CreateRegisteredTestRulesList;
		_registeredTestListModel.OnSelectionChanged += CreateRegisteredTestRulesList;
		_testablesList.Add(_testablesListView);
	}
	private void CreateRegisteredTestRulesList(RegisteredTest registeredTest)
	{
		ResetList(_rulesList, ref _rulesListView);
		
		_rulesListView = _registeredTestRuleListModel.CreateListView(registeredTest.RegisteredTestRules);
		// _rulesListModel.OnItemChosen += CreateTestablesList;
		// _rulesListModel.OnSelectionChanged += CreateTestablesList;
		_rulesList.Add(_rulesListView);
	}
	
	private void HandleScanRequest()
	{
		TestingEditorModel.Scan();
		CreateRegisteredTestScenesList();
	}
	private void ResetList(VisualElement ve, ref ExtendedListView elv)
	{
		if (elv == null) 
			return;
		ve.Remove(elv);
		elv = null;
	}
}
