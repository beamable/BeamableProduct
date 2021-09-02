using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Editor.UI.Buss.Extensions;
using Beamable.Editor.UI.Buss.Model;
using Beamable.UI.Buss;
using Beamable.UI.Buss.Properties;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;

#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Buss.Components
{
   public class VariableSearchVisualElement : BeamableVisualElement
   {
      public OptionalPropertyFieldWrapper PropertyFieldWrapper { get; }

      public const string COMMON = BeamableComponentsConstants.UI_PACKAGE_PATH + "/Buss/Components/variableSearchVisualElement";

      private VisualElement _container;
      private TextField _textField;
      private string _filterText = "";
      public Action<VariableWrapper> OnSelected;
      private readonly StyleRuleBundle _model;


      public VariableSearchVisualElement(OptionalPropertyFieldWrapper propertyFieldWrapper, StyleRuleBundle model) : base(COMMON)
      {
         _model = model;
         PropertyFieldWrapper = propertyFieldWrapper;

      }

      private VariableSetWrapper _availableTypeSet;
      private List<VariableWrapper> _availableVariables;

      public override void Refresh()
      {
         base.Refresh();
         _container = Root.Q<VisualElement>("container");
         _textField = Root.Q<TextField>();

         // find the available variables...

         _availableTypeSet = _model.SafeComputedStyles.Scope.GetAvailableTypes()
            .FirstOrDefault(t => t.VariableSet.VariableType.IsAssignableFrom(PropertyFieldWrapper.PropertyType));

         if (_availableTypeSet == null)
         {
            _container.Add(new Label("No Variables Available for this value type"));
            _textField.parent.Remove(_textField);
         }
         else
         {
            if (_model.Behaviour != null)
            {
               _availableVariables = _availableTypeSet.GetVariables();
            }
            else
            {
               _availableVariables = _model.Sheet.GetVariables();
            }

            _textField.RegisterValueChangedCallback(evt =>
            {
               _filterText = evt.newValue;
               RefreshList();
            });
            RefreshList();
            _textField.BeamableFocus();
         }
      }

      void RefreshList()
      {
         _container.Clear();

         foreach (var variable in _availableVariables)
         {
            if (!variable.Name.ToLower().Contains(_filterText)) continue;
            var button = new Button();
            button.text = variable.Name;

            button.clickable.clicked += () => { OnSelected?.Invoke(variable);};

            _container.Add(button);
         }
      }
   }
}