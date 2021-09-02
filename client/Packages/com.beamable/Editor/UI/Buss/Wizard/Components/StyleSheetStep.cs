using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Beamable.Editor.UI.Buss.Wizard.Model;
using Beamable.UI.Buss;
using Beamable.Editor.UI.Buss.Model;
using Beamable.UI.Buss.Properties;
using UnityEditor;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;

#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Buss.Wizard.Components
{
   public class StyleSheetStep : WizardStepVisualElement
   {
      public StyleSheetStep(WizardModel model) : base(model)
      {
      }

      public override string StepName => "Setup";
      public override string QuestionText => "Welcome.";

      public override string AboutText
      {
         get
         {
            if (AnySheets)
            {
               return "You've already configured some style sheet objects. ";
            }
            else
            {
               return AboutFirstTime;
            }
         }
      }

      public override bool Enabled => true;

      private bool AnySheets => BussConfiguration.Instance.DefaultSheets.Any();

      public string AboutFirstTime
      {
         get { return "Let's get your Beamable UIs looking perfect! Beamable themes live in style sheets. " +
                      "you can have many style sheets, configured at a project level, or a Gameobject level. " +
                      "We're going to start by creating a project wide theme, but you can always override it later on. "; }
      }


      private TextField _nameField;
      private PopupField<int> _popupField;

//      protected override void OnActivate(WizardContext ctx)
//      {
//         if (Model.IsNewTheme)
//         {
//
//            ctx.SetNextEnabled(!string.IsNullOrEmpty(Model.ThemeName));
//            _nameField?.OnValueChanged(evt =>
//            {
//               Model.ThemeName = evt.newValue;
//               ctx.SetNextEnabled(!string.IsNullOrEmpty(Model.ThemeName));
//
//            });
//            _nameField?.BeamableFocus();
//         }
//
//      }

      void SetForTextField(WizardContext ctx)
      {
         ctx.SetNextEnabled(Model.HasThemeName);
         _nameField?.RegisterValueChangedCallback(evt =>
         {
            Model.ThemeName = evt.newValue;
            ctx.SetNextEnabled(Model.HasThemeName);

         });
         _nameField?.BeamableFocus();
      }

      protected override void Populate(WizardContext ctx, VisualElement container)
      {

         if (AnySheets)
         {
            // do one thing
            container.Add(new Label("Select an existing style sheet to modify, or create a new one..."));

            var existing = BussConfiguration.Instance.DefaultSheets;

            var names = existing
               .Where(x => x != null)
               .Select(x => x.name).ToList();
            names.Insert(0, "New...");
            var indexValues = new List<int>();
            for (var i = 0; i < names.Count; i++)
            {
               indexValues.Add(i);
            }

            _popupField = new PopupField<int>(indexValues, 0, i => names[i], i => names[i]);
            _popupField.SetValueWithoutNotify(names.Count > 1 ? 1 : 0);
            container.Add(_popupField);

            var rest = new VisualElement();
            container.Add(rest);

            void Handle(int nextValue)
            {
               if (nextValue == 0)
               {
                  Model.ClearSheet();
                  rest.Add(new Label("What do you want to call your style sheet? "));
                  _nameField = new TextField();
                  rest.Add(_nameField);
                  SetForTextField(ctx);

               }
               else
               {
                  Model.SetFromSheet(existing[nextValue - 1]);
                  rest.Clear();
                  ctx.SetNextEnabled(true);
               }
            }
            _popupField.RegisterValueChangedCallback(evt =>
            {
               Handle(evt.newValue);
            });
            Handle(_popupField.value);
         }
         else
         {
            Model.IsNewTheme = true;
            container.Add(new Label("What do you want to call your style sheet? "));
            _nameField = new TextField();


            container.Add(_nameField);
            container.Add(new Label("We'll create a style sheet in your /Assets directory. Feel free to move it wherever you want later."));
            SetForTextField(ctx);
         }

      }



   }
}