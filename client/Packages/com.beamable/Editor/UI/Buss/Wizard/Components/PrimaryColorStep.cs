using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Beamable.Editor.UI.Buss.Wizard.Model;
using Beamable.UI.Buss.Properties;
using Beamable.Editor.UI.Buss.Model;
using Beamable.UI.Buss;
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

   public class ColorStep : WizardStepVisualElement
   {
      private readonly OptionalColor _color;
      private readonly string _name;
      private readonly string _question;
      private readonly string _about;

      public ColorStep(OptionalColor color, string name, string question, string about, WizardModel model) : base(model)
      {
         _color = color;
         _name = name;
         _question = question;
         _about = about;
      }

      public override string StepName => _name;
      public override string QuestionText => _question;

      public override string AboutText => _about;
      public override bool Enabled => Model.HasThemeName;

      private ColorField _field;

      protected override void OnActivate(WizardContext ctx)
      {
         _field.SetValueWithoutNotify(_color.Value);

         base.OnActivate(ctx);
      }

      protected override void Populate(WizardContext ctx, VisualElement container)
      {
         _field = new ColorField();
         _field.RegisterValueChangedCallback(evt =>
         {
            _color.Set(true, evt.newValue);
         });
         container.Add(_field);
      }
   }
//
//   public class PrimaryColorStep : ColorStep
//   {
//      public PrimaryColorStep(WizardModel model) : base(model)
//      {
//      }
//
//      public override string StepName => "Primary Color";
//      public override string QuestionText => "What is the main color of your game?";
//
//      public override string AboutText =>
//         "Beamable UI's use a primary-color to drive the main look and feel. You should pick a color that aligns with the genre and feel of your game.";
//
//      protected override void Populate(VisualElement container)
//      {
//         container.Add(new ColorField());
//      }
//   }
}