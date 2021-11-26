using UnityEngine;

// TODO: move this to some common namespace??
namespace Beamable.Editor.UI.Buss
{
   public static class BeamableComponentsConstants
   {
      public const string UI_PACKAGE_PATH = "Packages/com.beamable/Editor/UI";
      public const string COMP_PATH = UI_PACKAGE_PATH  +"/Common/Components";
      public const string COMMON_USS_PATH = UI_PACKAGE_PATH + "/Common/Common.uss";
      public const string BUSS_PACKAGE_PATH = UI_PACKAGE_PATH + "/Buss";
      public const string BUSS_COMPONENTS_PATH = BUSS_PACKAGE_PATH + "/Components";
      public const string BUSS_THEME_MANAGER_PATH = BUSS_PACKAGE_PATH + "/ThemeManager";

      // Schedules
      public static readonly Vector2 SchedulesWindowSize = new Vector2(450, 420);
      public const string SCHEDULES_PATH = UI_PACKAGE_PATH + "/Schedules";
      public const string SCHEDULES_WINDOW_HEADER = "Schedules";
   }
}
