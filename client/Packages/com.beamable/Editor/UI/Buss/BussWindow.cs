using System;
using System.Linq;
using Beamable.Editor.UI.Buss.Components;
using Beamable.UI.Buss;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Buss
{
   public class BussWindow : EditorWindow
   {

// Not fit for end users - chrisa
#if BEAMABLE_DEVELOPER
      [MenuItem(
         BeamableConstants.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_THEME_MANAGER + "/" +
         BeamableConstants.OPEN + " " +
         BeamableConstants.BUSS,
         priority = BeamableConstants.MENU_ITEM_PATH_WINDOW_PRIORITY_3)]
#endif
      public static BussWindow Init()
      {
         var inspector = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");

         BussWindow wnd = GetWindow<BussWindow>(BeamableConstants.BUSS, true, inspector);
         return wnd;
      }
      public const string BUSS_PACKAGE_PATH = "Packages/com.beamable/Editor/UI/Buss";

      private VisualElement _windowRoot;
      private VisualElement _contentRoot;

      private StyleBehaviour _currentSelection;

      private VisualElement _inspectorContainer, _explorerContainer;
      private BussInspectorVisualElement _inspectorElement;
      private BehaviourExplorerVisualElement _explorerElement;

      private static Selector _highlightSelector;

      public void OnEnable()
      {
         VisualElement root = this.GetRootVisualContainer();
         var uiAsset =
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{BUSS_PACKAGE_PATH}/bussWindow.uxml");
         _windowRoot = uiAsset.CloneTree();
         _windowRoot.AddStyleSheet($"{BUSS_PACKAGE_PATH}/bussWindow.uss");
         _windowRoot.name = nameof(_windowRoot);

         var manualRefreshBtn = new Button(() =>
         {
            _inspectorElement.SetModel(_currentSelection);
            _explorerElement.Refresh();
            _explorerElement.SetSelected(_currentSelection);


         });
         manualRefreshBtn.text = "Refresh";
         root.Add(manualRefreshBtn);
         root.Add(_windowRoot);

         Selection.selectionChanged -= HandleSelectionChange;
         Selection.selectionChanged += HandleSelectionChange;

         // Remove delegate listener if it has previously
         // been assigned.

#if UNITY_2018
         SceneView.onSceneGUIDelegate -= OnSceneGUI;
         // Add (or re-add) the delegate.
         SceneView.onSceneGUIDelegate += OnSceneGUI;
#elif UNITY_2019
         SceneView.duringSceneGui -= OnSceneGUI;
         SceneView.duringSceneGui += OnSceneGUI;
#endif


         _inspectorContainer = _windowRoot.Q<VisualElement>("buss-inspector-container");
         _inspectorElement = new BussInspectorVisualElement();
         _inspectorContainer.Add(_inspectorElement);

         _explorerContainer = _windowRoot.Q<VisualElement>("explorer-container");
         _explorerElement = new BehaviourExplorerVisualElement();
         _explorerContainer.Add(_explorerElement);
         _explorerElement.Refresh();

         HandleSelectionChange();
      }


      private void OnFocus()
      {
         HandleSelectionChange();
      }

      StyleSheetObject ResolveStyleSheet()
      {
         var sheet = _currentSelection.GetFirstStyleSheet();
         // TODO If no sheet exists in the heirarchy, prompt to make a new one in User Space.
         return sheet;
      }

      private void OnDisable()
      {
         Selection.selectionChanged -= HandleSelectionChange;
      }

      private void HandleSelectionChange()
      {
         var selected = Selection.activeGameObject;
         _currentSelection = selected?.GetComponent<StyleBehaviour>() ?? null;
         _inspectorElement.SetModel(_currentSelection);
         _explorerElement.SetSelected(_currentSelection);
      }

      // Window has been selected

      void OnDestroy() {
         // When the window is destroyed, remove the delegate
         // so that it will no longer do any drawing.
#if UNITY_2018
         SceneView.onSceneGUIDelegate -= OnSceneGUI;
#elif UNITY_2019
         SceneView.duringSceneGui -= OnSceneGUI;
#endif

      }

      static void OnSceneGUI(SceneView sceneView) {
         DrawHighlightSelector();

         HandleUtility.Repaint();
      }



      private static void DrawHighlightSelector()
      {
        if (_highlightSelector == null) return;

         // TODO: this will be really slow, because its calling on the draw thread every tick. We really don't need to do that.
         var all = GameObject.FindObjectsOfType<StyleBehaviour>();
         var matches = all.Where(x => x.MatchesSelector(_highlightSelector));

         var faceColor = new Color(0, 0, 255, .2f);
         var outlineColor = Color.white;

         foreach (var match in matches)
         {
            var verts = new Vector3[4];
            match.GetComponent<RectTransform>().GetWorldCorners(verts);
            Handles.color = Color.white;
            Handles.DrawSolidRectangleWithOutline(verts, faceColor, outlineColor);
         }
      }

      public static void HighlightSelector(Selector selector)
      {
         _highlightSelector = selector;
      }
   }
}