using System;
using Beamable.UI.MSDF;
using Beamable.UI.Buss.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.UI.Buss
{
   public class ButtonStyleBehaviour : StyleBehaviour
   {
      static ButtonStyleBehaviour()
      {
         RegisterType<ButtonStyleBehaviour>("button");
      }

      public override string TypeString => "button";

      public Selectable Button;
      public BeamableMSDFBehaviour MsdfBehaviour;

      public void Update()
      {
         SetClass("disabled", !Button.interactable);
      }

      public override void Apply(StyleObject styles)
      {
         MsdfBehaviour.ApplyStyleObject(styles);
      }
   }
}