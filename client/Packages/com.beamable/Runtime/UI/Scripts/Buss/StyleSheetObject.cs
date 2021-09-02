using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.UI.Buss
{

   [CreateAssetMenu(
      fileName = "Style Sheet Object",
      menuName = BeamableConstants.MENU_ITEM_PATH_ASSETS_BEAMABLE + "/" +
                 "Buss/StyleSheetObject",
      order = BeamableConstants.MENU_ITEM_PATH_ASSETS_BEAMABLE_ORDER_1)]
   public class StyleSheetObject : ScriptableObject
   {
      public List<SelectorWithStyle> Rules = new List<SelectorWithStyle>();

#if UNITY_EDITOR
      private void OnValidate()
      {
         // TODO: find all StyleBehaviours

         foreach (var behaviour in FindObjectsOfType<StyleBehaviour>())
         {
            if (!behaviour || !behaviour.isActiveAndEnabled && !behaviour.gameObject.activeInHierarchy)
            {
               continue;
            }

            behaviour.ApplyStyleTree();
         }

      }
#endif
   }

   [System.Serializable]
   public class SelectorWithStyle
   {
      public Selector Selector = new Selector();

      public StyleObject Style = new StyleObject();

   }

   public class StyleBundle
   {
      public SelectorWithStyle Rule;
      public StyleSheetObject Sheet;

      public Selector Selector => Rule?.Selector;
      public StyleObject Style => Rule?.Style;
   }

}