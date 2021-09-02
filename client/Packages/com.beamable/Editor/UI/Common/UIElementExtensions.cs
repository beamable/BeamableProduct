
using System;
using System.Linq;
using Beamable.Common;
using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Common;
using UnityEditor;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor
{
   public static class UIElementExtensions
   {


      public static Action AddErrorLabel(this Toggle self, FormConstraint constraint)
      {
         var errorLabel = new Label();
         errorLabel.AddToClassList("beamableErrorHidden");
         errorLabel.AddToClassList("beamableErrorLabel");
         errorLabel.AddStyleSheet(BeamableComponentsConstants.COMMON_USS_PATH);

         self.AddStyleSheet(BeamableComponentsConstants.COMMON_USS_PATH);
         self.AddToClassList("beamableErrorToggleField");

         var selfIndex = self.parent.IndexOf(self);
//         self.parent.Insert(selfIndex + 1, errorLabel);
         errorLabel.text = " ";

         self.Add(errorLabel);
         // has the blur event been fired?
         var hasFocused = false;
         void Check()
         {

            if (hasFocused && constraint.ErrorCheck(out var error))
            {
               errorLabel.text = error;
               self.AddToClassList("hasError");
               errorLabel.RemoveFromClassList("beamableErrorHidden");
            }
            else
            {
               errorLabel.text = " ";
               self.RemoveFromClassList("hasError");
               errorLabel.AddToClassList("beamableErrorHidden");
            }
         }

         self.RegisterCallback<FocusEvent>(evt => { hasFocused = true; });

         constraint.OnNotify += Check;
         constraint.OnValidate += Check;
         return Check;
      }

      public static Action AddErrorLabel(this TextField self, FormConstraint constraint)
      {
         var errorLabel = new Label();
         errorLabel.AddToClassList("beamableErrorHidden");
         errorLabel.AddToClassList("beamableErrorLabel");
         errorLabel.AddStyleSheet(BeamableComponentsConstants.COMMON_USS_PATH);
         errorLabel.AddTextWrapStyle();

         self.AddStyleSheet(BeamableComponentsConstants.COMMON_USS_PATH);
         self.AddToClassList("beamableErrorTextField");

         var selfIndex = self.parent.IndexOf(self);
         self.parent.Insert(selfIndex + 1, errorLabel);
         errorLabel.text = " ";

         // has the blur event been fired?
         var hasFocused = false;
         void Check()
         {

            if (hasFocused && constraint.ErrorCheck(out var error))
            {
               errorLabel.text = error;
               self.AddToClassList("hasError");
               errorLabel.RemoveFromClassList("beamableErrorHidden");
            }
            else
            {
               errorLabel.text = " ";
               self.RemoveFromClassList("hasError");
               errorLabel.AddToClassList("beamableErrorHidden");
            }
         }

         self.RegisterCallback<FocusEvent>(evt => { hasFocused = true; });

         constraint.OnNotify += Check;
         constraint.OnValidate += Check;
         return Check;
      }


      public static FormConstraint AddErrorLabel(this TextField self, string name, FormErrorCheckWithInput checker)
      {
         var constraint = new FormConstraint
         {
            ErrorCheck = (out string err) =>
            {
               err = checker(self.value);
               return !string.IsNullOrEmpty(err);
            },
            Name = name
         };

         var nextCheckTime = 0.0;

         void Debounce()
         {
            var time = EditorApplication.timeSinceStartup;
            if (time < nextCheckTime)
            {
               EditorApplication.delayCall += Debounce;
               return;
            }
            constraint.Check();
         }

         void StartDebounce()
         {
            // wait up to .25 seconds.
            var time = EditorApplication.timeSinceStartup;
            nextCheckTime = time + .25;
            Debounce();
         }

         self.RegisterValueChangedCallback(evt => StartDebounce());
         var check = AddErrorLabel(self, constraint);
         constraint.OnValidate += check;
         return constraint;
      }

      public static FormConstraint AddErrorLabel(this Toggle self, string name, FormBoolErrorCheckWithInput checker)
      {
         var constraint = new FormConstraint
         {
            ErrorCheck = (out string err) =>
            {
               err = checker(self.value);
               return !string.IsNullOrEmpty(err);
            },
            Name = name
         };


         self.RegisterValueChangedCallback(evt => { constraint.Check(); });
         var check = AddErrorLabel(self, constraint);
         constraint.OnValidate += check;
         return constraint;
      }


      public static void AddPlaceholder(this TextField self, string text)
      {
         VisualElement container = self;
         #if UNITY_2019_1_OR_NEWER
            container = self.Q("unity-text-input");
         #endif


         container.AddStyleSheet(BeamableComponentsConstants.COMMON_USS_PATH);
         var lbl = new Label(text);
         lbl.AddToClassList("beamablePlaceholder");

         container.Add(lbl);

         lbl.AddToClassList("hidden");
         lbl.style.fontSize = self.style.fontSize;

         void CheckForContent()
         {
            if (!string.IsNullOrEmpty(self.value))
            {
               lbl.AddToClassList("hidden");
            }
            else
            {
               lbl.RemoveFromClassList("hidden");
            }
         }

         self.RegisterValueChangedCallback(evt =>
         {
            CheckForContent();
         });

         void Center()
         {
            CheckForContent();

            var height = container.localBound.height;
            var lblHeight = lbl.localBound.height;

            if (height < 1 || float.IsNaN(height))
            {
               EditorApplication.delayCall += Center;
               return;
            }

            var top = (height * .5f) - (lblHeight * .5f);

            lbl.style.SetTop(top - 1);
            lbl.style.SetLeft(container.GetLeft());
         }

         EditorApplication.delayCall += () =>
         {
            lbl.RemoveFromClassList("hidden");
            Center();
         };
      }
   }
}