using UnityEngine;

namespace Beamable.Editor.UI.Buss
{
   public static class BeamableComponentsConstants
   {
      public const string UI_PACKAGE_PATH = "Packages/com.beamable/Editor/UI";
      public const string COMP_PATH = UI_PACKAGE_PATH  +"/Common/Components";
      public const string COMMON_USS_PATH = UI_PACKAGE_PATH + "/Common/Common.uss";

      // Schedules
      public static readonly Vector2 SchedulesWindowSize = new Vector2(450, 790);
      public const string SCHEDULES_PATH = UI_PACKAGE_PATH + "/Schedules";
      public const string SCHEDULES_WINDOW_HEADER = "Schedules";
   }
}