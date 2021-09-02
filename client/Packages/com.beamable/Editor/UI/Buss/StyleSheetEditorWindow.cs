using System.Collections.Generic;
using System.Linq;
using Beamable.Editor.UI.Buss.Components;
using Beamable.Editor.UI.Buss.Extensions;
using Beamable.Editor.UI.Buss.Model;
using Beamable.UI.Buss;
using Beamable.UI.Buss.Properties;
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
   [CustomEditor(typeof(StyleSheetObject))]
   public class StyleSheetObjectEditor : UnityEditor.Editor
   {
      public override void OnInspectorGUI() //2
      {
         // hook over to the inspector window...
         GUI.enabled = false;
         GUILayout.TextArea("For the best experience, please edit this object with the Beamable Sheet Inspector.");
         GUI.enabled = true;
         if (GUILayout.Button("Open in Sheet Inspector"))
         {
            StyleSheetEditorWindow.Init(target as StyleSheetObject);
         }

         GUILayout.Space(40);
         GUI.enabled = false;

         base.OnInspectorGUI();
      }
   }

   public class StyleSheetEditorWindow : EditorWindow
   {



      public static void Init(StyleSheetObject target)
      {
         if (target == null) return;

         var inspector = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");

         StyleSheetEditorWindow wnd = GetWindow<StyleSheetEditorWindow>(BeamableConstants.BUSS_SHEET_EDITOR, true, inspector);
         wnd.SetFor(target);
      }


      private VisualElement _windowRoot;
      private VisualElement _contentRoot;
      private VisualElement _ruleContainer;
      private Button _addRuleButton;

      private StyleSheetObject _sheet;

      private List<StyleRuleVisualElement> _ruleElements = new List<StyleRuleVisualElement>();
      public void OnEnable()
      {
         VisualElement root = this.GetRootVisualContainer();
         var uiAsset =
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{BussWindow.BUSS_PACKAGE_PATH}/styleSheetEditorWindow.uxml");
         _windowRoot = uiAsset.CloneTree();
         _windowRoot.AddStyleSheet($"{BussWindow.BUSS_PACKAGE_PATH}/styleSheetEditorWindow.uss");
         _windowRoot.name = nameof(_windowRoot);

         root.Add(_windowRoot);


         Selection.selectionChanged -= HandleSelectionChange;
         Selection.selectionChanged += HandleSelectionChange;

         _contentRoot = root.Q<VisualElement>("main-content");
         _ruleContainer = root.Q<VisualElement>("rules");
         _addRuleButton = root.Q<Button>("btn-add-rule");

         _addRuleButton.clickable.clicked += HandleAddRuleClicked;
         HandleSelectionChange();
      }

      private void HandleAddRuleClicked()
      {
         var nextRule = new SelectorWithStyle();
         _sheet.Rules.Insert(0, nextRule);

         SetFor(_sheet);
         _ruleElements[0].Q<SelectorVisualElement>().Edit();
      }

      private void HandleSelectionChange()
      {
         var selected = Selection.activeObject;

         if (selected is StyleSheetObject sheet)
         {
            SetFor(sheet);
         }
      }

      public void SetFor(StyleSheetObject sheet)
      {
         _sheet = sheet;

         //_contentRoot.Add(new Label("Sheet " + sheet.name));

         _ruleElements.Clear();
         _ruleContainer.Clear();


         foreach (var rule in sheet.Rules)
         {
            var styleBundle = new StyleBundle
            {
               Rule = rule,
               Sheet = sheet
            };
            var ruleBundle = new StyleRuleBundle(styleBundle, null);
            var elem = new StyleRuleVisualElement(ruleBundle);
            elem.OnDeleteRequested += () =>
            {
               sheet.Rules.Remove(rule);
               StyleBehaviourExtensions.Refresh();
               SetFor(sheet);
            };

            _ruleElements.Add(elem);
            elem.Refresh();
            _ruleContainer.Add(elem);
         }
      }
   }
}