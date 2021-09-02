using System;
using System.Collections.Generic;
using System.Reflection;
using Beamable.Editor.UI.Buss.Extensions;
using Beamable.Editor.UI.Buss.Model;
using Beamable.UI.Buss;
using UnityEditor;
using UnityEngine;
using Object = System.Object;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Buss.Components
{
   public class StyleRuleVisualElement : BeamableVisualElement
   {
      public const string COMMON = BeamableComponentsConstants.UI_PACKAGE_PATH + "/Buss/Components/styleRuleVisualElement";


      public StyleRuleBundle Model { get; private set; }
      public Action OnSelectorChanged, OnDeleteRequested;

      private VisualElement _selectorContainer, _styleContainer, _variableContainer;
      private SelectorVisualElement _selectorElement;
      private StyleObjectVisualElement _styleObjectElement;
      private VariableScopeVisualElement _variableScopeElement;
      private Button _addButton, _addVariableButton;
      private Button _sheetNameButton;

      public Action VariableChanged;
      public Action VariableAddOrRemoved, PropertyAddOrRemoved;

      public StyleRuleVisualElement(StyleRuleBundle model) : base(COMMON)
      {
         Model = model;
      }

      public override void Refresh()
      {
         base.Refresh();
         _selectorContainer = Root.Q<VisualElement>("selector-container");
         _styleContainer = Root.Q<VisualElement>("style-container");

         _sheetNameButton = Root.Q<Button>("btn-sheet-name");
         _sheetNameButton.text = Model.SheetName;
         _sheetNameButton.clickable.clicked += FocusOnStyleSheet;
         _sheetNameButton.SetEnabled(Model.Sheet != null);

         _selectorElement = new SelectorVisualElement(Model.Selector);
         _selectorContainer.Add(_selectorElement);
         _selectorElement.OnChanged += () => OnSelectorChanged?.Invoke();
         _selectorElement.OnDeleteRequested += () => OnDeleteRequested?.Invoke();
         _selectorElement.Refresh();

         if (Model.Selector.IsInline && Model.Selector.InlineConstraint != Model.Behaviour)
         {
            _selectorElement.Q<Label>().text =
               "inherited inline from " + Model.Selector.InlineConstraint.QualifiedSelectorString;
         }

         _variableContainer = Root.Q<VisualElement>("variable-container");
         _variableScopeElement = new VariableScopeVisualElement(Model.Style.Scope);


         _styleObjectElement = new StyleObjectVisualElement(Model);
         _styleObjectElement.OnVariableValueChanged += () => VariableChanged?.Invoke();
         _styleObjectElement.OnVariableAddOrRemoved += () => VariableAddOrRemoved?.Invoke();
         _styleObjectElement.OnPropertyRemoved += () => PropertyAddOrRemoved?.Invoke();
         _styleContainer.Add(_styleObjectElement);
         _styleObjectElement.Refresh();

         _addButton = Root.Q<Button>("btn-add-property");
         _addButton.clickable.clicked += HandleAddPropertyClick;

         _addVariableButton = Root.Q<Button>("btn-add-variable");
         _addVariableButton.clickable.clicked += HandleAddVariableClick;

      }

      private void HandleAddVariableClick()
      {
         var mousePos = new Vector2(_addButton.worldBound.xMin, _addButton.worldBound.yMin);
         mousePos = GUIUtility.GUIToScreenPoint(mousePos);
         var rect = new Rect(mousePos.x, mousePos.y, 10, 10);

         var content = new VariableTypeSearchVisualElement(Model.Style);
         var wnd = BeamablePopupWindow.ShowDropdown("Add Variable", rect, new Vector2(300, 350), content);

         content.OnSelected += (wrapper, name) =>
         {
            wnd.Close();
            EditorUtility.SetDirty(Model.Sheet);

            wrapper.Create(name);
            _styleObjectElement.Refresh();

            VariableAddOrRemoved?.Invoke();
         };

         content.Refresh();
      }

      private void FocusOnStyleSheet()
      {
         StyleSheetEditorWindow.Init(Model.Sheet);
      }

      private void HandleAddPropertyClick()
      {
         var mousePos = new Vector2(_addButton.worldBound.xMin, _addButton.worldBound.yMin);
         mousePos = GUIUtility.GUIToScreenPoint(mousePos);
         var rect = new Rect(mousePos.x, mousePos.y, 10, 10);

         var content = new PropertySearchVisualElement(Model.Style);
         var wnd = BeamablePopupWindow.ShowDropdown("Add Property", rect, new Vector2(300, 350), content);

         content.OnSelectedProperty += property =>
         {
            wnd.Close();
            property.Enable();
            _styleObjectElement.Refresh();
            StyleBehaviourExtensions.Refresh();
            PropertyAddOrRemoved?.Invoke();
         };

         content.Refresh();

      }

      public void RefreshVariableValues()
      {
         _styleObjectElement.RefreshPropertyElements();
      }
   }
}