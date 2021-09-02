using UnityEditor;
using UnityEngine;

namespace Beamable.Editor
{
   public static class HelpMenuItems
   {
      [MenuItem(
         BeamableConstants.MENU_ITEM_PATH_WINDOW_BEAMABLE_HELP + "/" +
         BeamableConstants.OPEN + " " +
         BeamableConstants.BEAMABLE_MAIN_WEBSITE,
         priority = BeamableConstants.MENU_ITEM_PATH_WINDOW_PRIORITY_3)]
      private static void OpenBeamableMainWebsite()
      {
         Application.OpenURL(BeamableConstants.URL_BEAMABLE_MAIN_WEBSITE);
      }

      [MenuItem(
         BeamableConstants.MENU_ITEM_PATH_WINDOW_BEAMABLE_HELP + "/" +
         BeamableConstants.OPEN + " " +
         BeamableConstants.BEAMABLE_DOCS_WEBSITE,
         priority = BeamableConstants.MENU_ITEM_PATH_WINDOW_PRIORITY_3)]
      private static void OpenBeamableDocsWebsite()
      {
         Application.OpenURL(BeamableConstants.URL_BEAMABLE_DOCS_WEBSITE);
      }
   }
}