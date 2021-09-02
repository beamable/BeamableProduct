using System;
using System.Linq;
using Beamable.Editor.UI.Buss.Extensions;
using Beamable.Editor.UI.Buss.Model;
using Beamable.UI.Buss;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Buss.Components
{
   public class VariableVisualElement : BeamableVisualElement
   {
      public VariableWrapper Model { get; }
      public const string COMMON = BeamableComponentsConstants.UI_PACKAGE_PATH + "/Buss/Components/variableVisualElement";

      private Label _label;
      private TextField _textField;
      private VisualElement _fieldContainer;
      private VisualElement _strikeThrough;


      private bool _isContributing;

      public Action OnRemoved;
      public Action OnChanged;
      public VariableVisualElement(VariableWrapper model, StyleRuleBundle bundle) : base(COMMON)
      {
         Model = model;

         if (bundle.Behaviour == null || bundle.Sheet == null)
         {
            _isContributing = true;
         }
         else
         {
            var usedVariable = bundle.Behaviour.GetApplicableStyles().FirstOrDefault(styleBundle =>
            {
               var usedVariables = styleBundle.Style.Scope.GetVariables();
               var matchingVariable = usedVariables
                  .FirstOrDefault(p => p.Name.Equals(model.Name));

               return matchingVariable != null;
            });
            _isContributing = usedVariable == null || usedVariable.Style == bundle.Style;
         }

      }

      public override void Refresh()
      {
         base.Refresh();

         InstallManipulator();

         _strikeThrough = Root.Q<VisualElement>("strike-through");

         _label = Root.Q<Label>();
         _label.text = $"--var({Model.Name})";

         _textField = Root.Q<TextField>();
         _textField.value = Model.Name;
         ShowLabel();


         if (!_isContributing)
         {
            _strikeThrough.AddToClassList("show");
         }
         else
         {
            _strikeThrough.AddToClassList("hide");
         }

         _label.RegisterCallback<MouseDownEvent>(HandleLabelClick);
         _textField.RegisterCallback<BlurEvent>(HandleTextBlur);
         _textField.RegisterCallback<KeyUpEvent>(HandleTextEnter);

         _fieldContainer = Root.Q<VisualElement>("value-holder");
         var fieldElement = new FieldValueVisualElement(new FieldValueModel
         {
            GetValue = () => Model.Value,
            Set = next =>
            {
               Model.SetValue(next);
               OnChanged?.Invoke();
            },
            FieldType = Model.VariableSet.VariableSet.VariableType,
            Range = null
         });
         fieldElement.Refresh();
         _fieldContainer.Add(fieldElement);
      }

      void CommitChange()
      {
         Model.Rename(_textField.value);
         _label.text = $"--var({Model.Name})";
      }

      private void HandleTextEnter(KeyUpEvent evt)
      {
         if (evt.keyCode != KeyCode.Return) return;
         CommitChange();
         ShowLabel();
      }

      private void HandleTextBlur(BlurEvent evt)
      {
         CommitChange();
         ShowLabel();
      }

      private void HandleLabelClick(MouseDownEvent evt)
      {
         if (evt.button != 0) return;
         ShowTextfield();
         _textField.BeamableFocus();
      }


      void ShowLabel()
      {
         Hide(_textField);
         Show(_label);
      }

      void ShowTextfield()
      {
         Hide(_label);
         Show(_textField);
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

      void InstallManipulator()
      {
         ContextualMenuManipulator m = new ContextualMenuManipulator(HandleRightClick);
         m.target = Root;
      }

      void HandleRightClick(ContextualMenuPopulateEvent evt)
      {
         // Modify event.menu
         evt.menu.BeamableAppendAction("Remove Variable", HandleClickedDelete);
      }

      void HandleClickedDelete(Vector2 mp)
      {
         parent.Remove(this);
         OnRemoved?.Invoke();
      }
   }
}