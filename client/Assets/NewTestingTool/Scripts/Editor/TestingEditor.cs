using Beamable.Editor.NewTestingTool.Models;
using Beamable.Editor.NewTestingTool.UI.Components;
using Beamable.Editor.UI;
using UnityEditor;
using UnityEngine.UIElements;

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

		var btn = root.Q<Button>("findTestsButton");
		btn.clicked -= TestingEditorModel.Scan;
		btn.clicked += TestingEditorModel.Scan;
		
		_actionBarVisualElement = root.Q<ActionBarVisualElement>("actionBarVisualElement");
		_actionBarVisualElement.Refresh();

		TestingEditorModel.Scan();
	}
}
