using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beamable.Editor.UI.Buss.Extensions;
using Beamable.Editor.UI.Buss.Model;
using Beamable.UI.Buss.Properties;
using TMPro;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;

#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Buss.Components
{

   public class RequiredBUSSPropertyVisualElement : BeamableVisualElement
   {
      private const string COMMON = BeamableComponentsConstants.UI_PACKAGE_PATH + "/Buss/Components/requiredBUSSPropertyVisualElement";
      private readonly OptionalPropertyFieldWrapper _propertyField;
      private readonly StyleRuleBundle _model;
      public Action OnRemoved;

      private Label _label;
      private VisualElement _field;
      private VisualElement _strikeThrough;

      public bool _isContributing;

      public RequiredBUSSPropertyVisualElement(OptionalPropertyFieldWrapper propertyField, StyleRuleBundle model) : base(COMMON)
      {
         _propertyField = propertyField;
         _model = model;

         var name = propertyField.GetName();
         if (model.Behaviour == null || model.Sheet == null)
         {
            _isContributing = true;
         }
         else
         {
            var usedStyle = _model.Behaviour.GetApplicableStyles().FirstOrDefault(styleBundle =>
            {
               var usedProperties = styleBundle.Style.GetUsedProperties().ToList();
               var matchingProp = usedProperties
                  .FirstOrDefault(p => p.GetName().Equals(name));

               return matchingProp != null;
            });
            _isContributing = usedStyle == null || usedStyle.Style == model.Style;
         }
      }



      public override void Refresh()
      {
         base.Refresh();
         InstallManipulator();

         _strikeThrough = Root.Q<VisualElement>("strike-through");
         _label = Root.Q<Label>();
         _field = Root.Q<VisualElement>("field");


         if (!_isContributing)
         {
            _strikeThrough.AddToClassList("show");
         }
         else
         {
            _strikeThrough.AddToClassList("hide");
         }

         _label.text = _propertyField.GetName();

         // the value might be a variable, in which case we should show that fact, and a way to unhook it.
         if (_propertyField.IsVariable)
         {
            var variableName = _propertyField.GetVariable();

            var method = _model.Style.Scope.GetType().GetMethod(nameof(VariableScope.TryResolve));
            var genMethod = method.MakeGenericMethod(_propertyField.PropertyType);
            var parameters = new object[]{variableName, null};

            var styleObject = _model.Behaviour?.ComputeStyleObject() ?? _model.Style;
            var result = (bool)genMethod.Invoke(styleObject.Scope, parameters);

            if (result)
            {
               _field.Add(new Label($"--var({variableName}) "));

               var fieldValueElement = new FieldValueVisualElement(new FieldValueModel
               {
                  FieldType = _propertyField.PropertyType,
                  GetValue = () => parameters[1],
                  Set = (next) => {},
                  Readonly = true
               });
               _field.Add(fieldValueElement);
               fieldValueElement.Refresh();
            }
            else
            {
               if (_model.Behaviour == null)
               {
                  _field.Add(new Label($"--var({variableName})"));

               }
               else
               {
                  _field.Add(new Label($"--var({variableName}) (No Definition Available"));

               }

            }

         }
         else
         {
            var fieldValueElement = new FieldValueVisualElement(new FieldValueModel
            {
               FieldType = _propertyField.PropertyType,
               GetValue = () => _propertyField.GetValue(_model.ComputedStyles),
               Set = (next) =>
               {
                  _propertyField.Set(next);
                  StyleBehaviourExtensions.Refresh();
               },
               Range = _propertyField.Field.GetCustomAttribute<RangeAttribute>()
            });
            _field.Add(fieldValueElement);
            fieldValueElement.Refresh();
         }
      }

      void InstallManipulator()
      {
         ContextualMenuManipulator m = new ContextualMenuManipulator(HandleRightClick);
         m.target = Root;
      }

      void HandleRightClick(ContextualMenuPopulateEvent evt)
      {
         // Modify event.menu
         if (_propertyField.IsVariable)
         {
            evt.menu.BeamableAppendAction("Use Value", HandleUseValue);
            evt.menu.BeamableAppendAction("Use Value", HandleUseValue);
            evt.menu.BeamableAppendAction("Change Variable", HandleUseVariable);
         }
         else
         {
            evt.menu.BeamableAppendAction("Use Variable", HandleUseVariable);
         }
         evt.menu.BeamableAppendAction("Remove Property", HandleClickedDelete);
      }

      void HandleUseVariable(Vector2 mousePosition)
      {
         // show a drop down to pick a value.
         var content = new VariableSearchVisualElement(_propertyField, _model);
         content.Refresh();

         var evtMousePos = mousePosition;
         var windowPos = EditorWindow.focusedWindow.position;
         var mousePos = new Vector2(windowPos.x + evtMousePos.x, windowPos.y + evtMousePos.y);
         var rect = new Rect(mousePos.x, mousePos.y, 10, 10);

         var wnd = BeamablePopupWindow.ShowDropdown("Find Variable", rect, new Vector2(300, 350), content);

         content.OnSelected += wrapper =>
         {
            wnd.Close();
            _propertyField.SetVariable(wrapper.Name);
            StyleBehaviourExtensions.Refresh();

            Refresh();
         };

         content.Refresh();
      }

      void HandleUseValue(Vector2 mp)
      {
         _propertyField.ClearVariable();
         StyleBehaviourExtensions.Refresh();


         Refresh();
      }

      void HandleClickedDelete(Vector2 mp)
      {
         _propertyField.Remove();
         OnRemoved?.Invoke();
      }

   }



}