using System;
using System.Collections.Generic;
using Beamable.Editor.UI.Buss.Extensions;
using Beamable.Editor.UI.Buss.Model;
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
   public class StyleObjectVisualElement : BeamableVisualElement
   {
      private readonly bool _enabled;
      public const string COMMON = BeamableComponentsConstants.UI_PACKAGE_PATH + "/Buss/Components/styleObjectVisualElement";
      public StyleRuleBundle Model { get; private set; }
      private VisualElement _propContainer;
      private VisualElement _varContainer;

      private List<VariableVisualElement> _variableElements = new List<VariableVisualElement>();
      private List<RequiredBUSSPropertyVisualElement> _propertyElements = new List<RequiredBUSSPropertyVisualElement>();

      public Action OnVariableValueChanged;
      public Action OnVariableAddOrRemoved;
      public Action OnPropertyRemoved;

      public StyleObjectVisualElement(StyleRuleBundle model, bool enabled=true) : base(COMMON)
      {
         _enabled = enabled;
         Model = model;
      }

      public override void Refresh()
      {
         base.Refresh();
         _variableElements.Clear();
         _propertyElements.Clear();

         _propContainer = Root.Q<VisualElement>("prop-container");
         _varContainer = Root.Q<VisualElement>("var-container");

         var properties = Model.Style.GetProperties();

         var variables = Model.Style.Scope.GetVariables();
         foreach (var variable in variables)
         {

            variable.OnRemoved += () => { OnVariableAddOrRemoved?.Invoke(); };
            variable.OnRenamed += () => { OnVariableAddOrRemoved?.Invoke(); };

            var element = new VariableVisualElement(variable, Model);
            _variableElements.Add(element);
            element.Refresh();
            element.OnRemoved += () =>
            {
               variable.Remove();
               OnVariableAddOrRemoved?.Invoke();
               EditorUtility.SetDirty(Model.Sheet);
            };
            element.OnChanged += () =>
            {
               StyleBehaviourExtensions.Refresh();
               OnVariableValueChanged?.Invoke();
            };
            _varContainer.Add(element);
         }

         foreach (var prop in properties)
         {
            if (!prop.Enabled) continue; // don't bother showing properties that are blank.

            // one property may have several lines
            foreach (var element in prop.GenerateElements(Model))
            {
               _propContainer.Add(element);
               _propertyElements.Add(element);
               element.Refresh();
               element.OnRemoved += () =>
               {

                  StyleBehaviourExtensions.Refresh();
                  OnPropertyRemoved?.Invoke();
               }; // refresh whole object if one property is removed.
            }
         }

         if (!_enabled)
         {
            SetEnabled(false);
         }
      }

      public void RefreshPropertyElements()
      {
         foreach (var elem in _propertyElements)
         {
            elem.Refresh();
         }
      }
   }
}