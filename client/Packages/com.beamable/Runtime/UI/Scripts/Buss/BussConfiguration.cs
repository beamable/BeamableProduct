using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beamable;
using UnityEditor;
using UnityEngine;

namespace Beamable.UI.Buss
{
   public class BussConfiguration : ModuleConfigurationObject
   {
      public static BussConfiguration Instance => Get<BussConfiguration>();

      public StyleSheetObject FallbackSheet;

      public List<StyleSheetObject> DefaultSheets = new List<StyleSheetObject>();

      public IEnumerable<StyleSheetObject> EnumerateSheets()
      {

         foreach (var sheet in DefaultSheets)
         {
            if (sheet != null)
            {
               yield return sheet;
            }
         }

         if (FallbackSheet != null)
         {
            yield return FallbackSheet;
         }


      }
   }
}