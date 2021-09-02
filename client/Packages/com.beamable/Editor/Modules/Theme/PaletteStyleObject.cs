using System;
using Beamable.Theme;
using Beamable.Theme.Palettes;
using UnityEngine;
using FontStyle = Beamable.Theme.Palettes.FontStyle;

namespace Beamable.Editor.Modules.Theme
{
   public interface IPaletteStyleObject
   {
      void SetStyle(PaletteStyle style);
      bool Modified { get; }
   }

   public class PaletteStyleObject<T> : ScriptableObject, IPaletteStyleObject where T : PaletteStyle
   {
      public T Style;

      public void SetStyle(PaletteStyle style)
      {
         Style = style as T;
         Modified = false;
      }

      private void OnValidate()
      {
         Modified = true;
      }

      public bool Modified { get; set; }
   }

}