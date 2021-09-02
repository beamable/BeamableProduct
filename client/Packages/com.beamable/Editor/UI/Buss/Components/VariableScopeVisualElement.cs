using System;
using Beamable.UI.Buss.Properties;
using Beamable.Editor.UI.Buss.Extensions;
using Beamable.Editor.UI.Buss.Model;
using Beamable.UI.Buss;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;

#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif


namespace Beamable.Editor.UI.Buss.Components
{
   public class VariableScopeVisualElement : BeamableVisualElement
   {
      public VariableScope Model { get; }
      public const string COMMON = BeamableComponentsConstants.UI_PACKAGE_PATH + "/Buss/Components/variableScopeVisualElement";

      public VariableScopeVisualElement(VariableScope model) : base(COMMON)
      {
         Model = model;
      }
   }
}