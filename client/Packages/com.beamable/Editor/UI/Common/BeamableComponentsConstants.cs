#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Buss
{
   public static class BeamableComponentsConstants
   {
      public const string UI_PACKAGE_PATH = "Packages/com.beamable/Editor/UI";
      public const string COMP_PATH = UI_PACKAGE_PATH  +"/Common/Components";
      public const string COMMON_USS_PATH = UI_PACKAGE_PATH + "/Common/Common.uss";
   }
}