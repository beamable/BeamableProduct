
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

      public static string START = "Run";
      public static string STOP = "Stop";
      public static string BUILD_DEBUG_PREFIX = "[DEBUG]";
      public static string BUILD_START = "Build and Run";
      public static string BUILD_ENABLE_DEBUG = "Enable Debug Tools";
      public static string BUILD_DISABLE_DEBUG = "Disable Debug Tools";
      public static string BUILD_RESET = "Build and Rerun";

      public static string GetBuildButtonString(bool includeDebugTools, string text) => includeDebugTools
         ? $"{Constants.BUILD_DEBUG_PREFIX} {text}"
         : text;

   }
}