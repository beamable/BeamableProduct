using Beamable.Theme.Palettes;
using Beamable.UI.Scripts;
using UnityEngine;

namespace Beamable.Theme.Appliers
{
   [System.Serializable]
   public class TransformStyleApplier : StyleApplier<TransformOffsetBehaviour>
   {
      public TransformBinding Transform;
      public override void Apply(ThemeObject theme, TransformOffsetBehaviour component)
      {
         var transformStyle = theme.GetPaletteStyle(Transform);
         if (transformStyle == null) return;

         component.Offset = transformStyle.PositionOffset;
         component.Scale = transformStyle.Scale;
         component.ApplyOffset();
      }
   }
}