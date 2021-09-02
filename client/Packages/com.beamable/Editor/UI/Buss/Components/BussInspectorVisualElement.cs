
using System.Collections.Generic;
using System.Linq;
using Beamable.Editor.UI.Buss.Model;
using Beamable.UI.Buss;
using UnityEditor;

#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif


namespace Beamable.Editor.UI.Buss.Components
{
   public class BussInspectorVisualElement : BeamableVisualElement
   {
      public const string COMMON = BeamableComponentsConstants.UI_PACKAGE_PATH + "/Buss/Components/bussInspectorVisualElement";

      private StyleBehaviour _currentSelection;
      private SerializedObject _serialized;

      private ScrollView _ruleContainer;
      private VisualElement _pseudoContainer, _computedStyleContainer, _cascadeContainer, _computedContainer, _sheetSelectorContainer;
      private Label _selectedLabel;
      private Button _addRuleBtn, _tabCascade, _tabComputed;
      private PseudoStateGroupVisualElement _pseudoElement;
      private StyleObjectVisualElement _computedStyleElement;

      private List<StyleRuleVisualElement> _styleRuleElements = new List<StyleRuleVisualElement>();

      private StyleSheetObject _selectedSheet;
      private bool _cascadeState = true;

      public BussInspectorVisualElement() : base(COMMON)
      {
      }

      private void HandleAddRuleClicked()
      {
         if (_currentSelection == null) return;

         // create a rule that only applies to this object.
         var rule = new SelectorWithStyle
         {
            Selector = new Selector
            {
               IdConstraint = _currentSelection.Id,
               ElementTypeConstraint = _currentSelection.TypeString
            },
            Style = new StyleObject()
         };

         var sheet = _selectedSheet;
         sheet.Rules.Add(rule);
         EditorUtility.SetDirty(sheet);

         Refresh();
      }

      void HandleSheetChange()
      {
         _addRuleBtn.text = $"Add rule to {_selectedSheet.name}";
      }

      public override void Refresh()
      {
         base.Refresh();
         _styleRuleElements.Clear();

         _ruleContainer = Root.Q<ScrollView>("rule-container");
         _pseudoContainer = Root.Q<VisualElement>("pseudo-container");
         _addRuleBtn = Root.Q<Button>("btn-add-rule");
         _selectedLabel = Root.Q<Label>("lbl-selection-name");
         _computedStyleContainer = Root.Q<VisualElement>("property-container");
         _cascadeContainer = Root.Q<VisualElement>("cascade");
         _computedContainer = Root.Q<VisualElement>("computed");
         _sheetSelectorContainer = Root.Q<VisualElement>("sheet-selector-container");

         _addRuleBtn.clickable.clicked += HandleAddRuleClicked;
         _addRuleBtn.SetEnabled(false);

         _ruleContainer.Clear();
         _selectedLabel.text = "None";

         if (_currentSelection == null) return;


         _pseudoElement = new PseudoStateGroupVisualElement(_currentSelection);
         _pseudoElement.OnStateChanged += Refresh;
         _pseudoElement.Refresh();
         _pseudoContainer.Add(_pseudoElement);


         HandleSheetChange();
         var sheets = _currentSelection.GetStyleSheets();
         var sheetNames = sheets.Select(s => s.name).ToList();
         var defaultName = _selectedSheet.name;
         var sheetPicker = new PopupField<string>(sheetNames, defaultName);
         sheetPicker.RegisterValueChangedCallback(evt =>
         {
            _selectedSheet = sheets.FirstOrDefault(s => s.name.Equals(evt.newValue));
            HandleSheetChange();
         });
         _sheetSelectorContainer.Add(new Label("Style sheet"));
         _sheetSelectorContainer.Add(sheetPicker);

         _tabCascade = Root.Q<Button>("tab-cascade");
         _tabComputed = Root.Q<Button>("tab-computed");

         _tabCascade.clickable.clicked += SetForCascade;
         _tabComputed.clickable.clicked += SetForComputed;

         _addRuleBtn.SetEnabled(true);
         _selectedLabel.text = _currentSelection.QualifiedSelectorString;
         var applicableStyles = _currentSelection.GetApplicableStyles();

         var computedBundle = new StyleBundle
         {
            Sheet = null,
            Rule = new SelectorWithStyle
            {
               Selector = null,
               Style = applicableStyles.Select(x => x.Style).Aggregate((agg, curr) => agg.Merge(curr))
            }
         };
         _computedStyleElement = new StyleObjectVisualElement(new StyleRuleBundle(computedBundle, _currentSelection), false);
         _computedStyleContainer.Add(_computedStyleElement);
         _computedStyleElement.Refresh();


         foreach (var styleBundle in applicableStyles)
         {

            var model = new StyleRuleBundle(styleBundle, _currentSelection);
            var sr = new StyleRuleVisualElement(model);

            //sr.SetEnabled((styleBundle.Sheet?.IsPackageObject ?? false));

            _styleRuleElements.Add(sr);
            _ruleContainer.Add(sr);

            sr.VariableChanged += () =>
            {
               RefreshVariableValues();
            };
            sr.OnSelectorChanged += () =>
            {
               if (!_currentSelection.Matches(styleBundle.Selector))
               {
                  _ruleContainer.Remove(sr);
               }
               Refresh();
            };
            sr.VariableAddOrRemoved += () => { Refresh(); };
            sr.PropertyAddOrRemoved += Refresh;
            sr.OnDeleteRequested += () =>
            {
               var sheet = styleBundle.Sheet;
               sheet.Rules.Remove(styleBundle.Rule);
               _ruleContainer.Remove(sr);
               Refresh();
            };
            sr.Refresh();
         }

         if (_cascadeState)
         {
            SetForCascade();
         }
         else
         {
            SetForComputed();
         }
      }

      void ClearActiveTab()
      {
         var active = "active";
         _tabCascade.RemoveFromClassList(active);
         _tabComputed.RemoveFromClassList(active);
      }

      void SetForCascade()
      {
         ClearActiveTab();
         _tabCascade.AddToClassList("active");
         Hide(_computedContainer);
         Show(_cascadeContainer);
         _cascadeState = true;
      }

      void SetForComputed()
      {
         ClearActiveTab();
         _tabComputed.AddToClassList("active");
         Show(_computedContainer);
         Hide(_cascadeContainer);
         _cascadeState = false;
      }

      void Hide(VisualElement e)
      {
         e.RemoveFromClassList("show");
         e.AddToClassList("hide");
      }

      void Show(VisualElement e)
      {

         e.RemoveFromClassList("hide");
         e.AddToClassList("show");
      }


      public void SetModel(StyleBehaviour currentSelection)
      {
         if (_currentSelection != null)
         {
            _currentSelection.OnStateUpdated -= HandleSelectionStateUpdate;
         }
         _currentSelection = currentSelection;
         if (_currentSelection != null)
         {
            _currentSelection.OnStateUpdated += HandleSelectionStateUpdate;
            _selectedSheet = _currentSelection.GetFirstStyleSheet();

         }
         Refresh();
      }

      void RefreshVariableValues()
      {
         foreach (var sr in _styleRuleElements)
         {
            sr.RefreshVariableValues();
         }
      }

      private void HandleSelectionStateUpdate()
      {
         Refresh();
      }
   }
}