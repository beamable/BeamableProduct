using System;
using Beamable.Editor.UI.Buss.Extensions;
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

namespace Beamable.Editor.UI.Buss.Components
{
   public class SelectorVisualElement : BeamableVisualElement
   {
      public const string COMMON = BeamableComponentsConstants.UI_PACKAGE_PATH + "/Buss/Components/selectorVisualElement";
      public Selector Model { get; private set; }
      private Label _label;
      public TextField _textField;
      public Action OnChanged;
      public Action OnDeleteRequested;
      public SelectorVisualElement(Selector selector) : base(COMMON)
      {
         Model = selector;
      }

      public override void Refresh()
      {
         base.Refresh();


         // decide what to do.
         _label = Root.Q<Label>();
         _textField = Root.Q<TextField>();

         var text = Model.ToString();
         _label.text = text;
         _textField.SetValueWithoutNotify(text);

         ShowLabel();

         _label.RegisterCallback<MouseOverEvent>(HandleHoverOnLabel);
         _label.RegisterCallback<MouseOutEvent>(HandleMouseExitLabel);

         if (!Model.IsInline)
         {
            _label.RegisterCallback<MouseDownEvent>(HandleLabelClick);
            _textField.RegisterCallback<BlurEvent>(HandleTextBlur);
            _textField.RegisterCallback<KeyUpEvent>(HandleTextEnter);
            InstallManipulator();
         }
         else
         {
            _label.AddToClassList("readonly");
         }
      }

      private void HandleMouseExitLabel(MouseOutEvent evt)
      {
         BussWindow.HighlightSelector(null);

      }

      private void HandleHoverOnLabel(MouseOverEvent evt)
      {
         BussWindow.HighlightSelector(Model);

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

      void CommitChange()
      {
         var newSelectorText = _textField.value;
         var nextSelector = SelectorParser.Parse(newSelectorText);
         // update the existing model in place. TODO: factor into method somewhere else.
         Model.ClassConstraints = nextSelector.ClassConstraints;
         Model.ChildSelector = nextSelector.ChildSelector;
         Model.IdConstraint = nextSelector.IdConstraint;
         Model.PseudoConstraints = nextSelector.PseudoConstraints;
         Model.ElementTypeConstraint = nextSelector.ElementTypeConstraint;
         Model.Commit();


         var nextText = nextSelector.ToString();
         _label.text = nextText;
         _textField.SetValueWithoutNotify(nextText);

         StyleBehaviourExtensions.Refresh();

         OnChanged?.Invoke();

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
         evt.menu.BeamableAppendAction("Delete", HandleClickedDelete);
      }

      void HandleClickedDelete(Vector2 mp)
      {
         OnDeleteRequested?.Invoke();
      }

      public void Edit()
      {
         ShowTextfield();
         _textField.BeamableFocus();
      }
   }
}