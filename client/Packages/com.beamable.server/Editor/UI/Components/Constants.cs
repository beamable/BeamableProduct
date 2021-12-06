
using UnityEditor;
using UnityEngine;

namespace Beamable.Server.Editor.UI.Components
{
   public static class Constants
   {
      public const string SERVER_UI = "Packages/com.beamable.server/Editor/UI";
      public const string COMP_PATH = "Packages/com.beamable.server/Editor/UI/Components";

      // Configuration
      public static string UssExt => EditorGUIUtility.isProSkin ? "uss" : "light.uss";
      public static Vector2 WindowSizeMinimum = new Vector2(500, 400);

      public static string Publish = "Publish";
      public static string PopUpBtn = "Console Log";

      public static string DEPENDENT_SERVICES_WINDOW_TITLE = "Dependent services";

      public static string START = "Play";
      public static string STOP = "Stop";
      public static string BUILD_DEBUG_PREFIX = "[DEBUG]";
      public static string BUILD_START = "Play";
      public static string BUILD_ENABLE_DEBUG = "Enable Debug Tools";
      public static string BUILD_DISABLE_DEBUG = "Disable Debug Tools";
      public static string BUILD_RESET = "Replay";

      public static string PROMPT_STARTED_FAILURE = "MICROSERVICE HASN'T STARTED...";
      public static string PROMPT_STOPPED_FAILURE = "MICROSERVICE HASN'T STOPPED...";

      public static string REMOTE_NOT_ENABLED = "Remote Disabled";
      public static string REMOTE_ENABLED = "Remote Enabled";
      public static string REMOTE_ONLY = "Remote Only";

      public const string OBSOLETE_WILL_BE_REMOVED = "This is no longer supported, and will be removed in the future.";

      public static string GetBuildButtonString(bool includeDebugTools, string text) => includeDebugTools
         ? $"{Constants.BUILD_DEBUG_PREFIX} {text}"
         : text;

   }
}
