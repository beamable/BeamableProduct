using System;
using System.Collections.Generic;
using System.Reflection;
using Beamable.Editor.UI.Buss.Extensions;
using Beamable.UI.Buss;
using Beamable.UI.Buss.Properties;
using Beamable.Editor.UI.Buss.Model;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;

#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif


namespace Beamable.Editor.UI.Buss.Components
{
   public class VariableTypeSearchVisualElement : BeamableVisualElement
   {
      public StyleObject Model { get; }
      public const string COMMON = BeamableComponentsConstants.UI_PACKAGE_PATH + "/Buss/Components/variableTypeSearchVisualElement";

      public Action<VariableSetWrapper, string> OnSelected;

      private VisualElement _tyepContainer;
      private TextField _textField;

      private string _filterText = "";

      public VariableTypeSearchVisualElement(StyleObject model) : base(COMMON)
      {
         Model = model;
      }

      public override void Refresh()
      {
         base.Refresh();
         _tyepContainer = Root.Q<VisualElement>("type-container");

         _textField = Root.Q<TextField>();
         _textField.BeamableFocus();

         _textField.RegisterValueChangedCallback(HandleFilterEvt);

         RefreshList();
      }

      void HandleFilterEvt(ChangeEvent<string> evt)
      {
         _filterText = evt.newValue;
         RefreshList();
      }

      void RefreshList()
      {
         _tyepContainer.Clear();
         // need to get the available types...
         foreach (var wrapper in Model.Scope.GetAvailableTypes())
         {
            var button = new Button();
            var attr = wrapper.VariableSet.GetType().GetCustomAttribute<VariableTypeNameAttribute>();
            var name  = attr?.Name ?? wrapper.VariableSet.VariableType.Name;

            if (!name.ToLower().Contains(_filterText)) continue;

            button.text = name;
            button.clickable.clicked += () => { NameStep(wrapper); };
            _tyepContainer.Add(button);
         }
      }

      void NameStep(VariableSetWrapper wrapper)
      {
         _tyepContainer.Clear();
         var submitButton = new Button(() => OnSelected?.Invoke(wrapper, _textField.value));
         _textField.SetValueWithoutNotify("");
         submitButton.SetEnabled(false);
         _textField.parent.Insert(0, new Label("Variable name:"));
         _textField.UnregisterValueChangedCallback(HandleFilterEvt);
         _textField.RegisterCallback<KeyUpEvent>(evt =>
         {
            var enabled = !string.IsNullOrEmpty(_textField.value);
            submitButton.SetEnabled(enabled);
            if (enabled && evt.keyCode == KeyCode.Return)
            {
               OnSelected?.Invoke(wrapper, _textField.value);
            }
         });
         submitButton.text = "Create";
         _tyepContainer.Add(submitButton);
         _textField.BeamableFocus();

      }
   }
}