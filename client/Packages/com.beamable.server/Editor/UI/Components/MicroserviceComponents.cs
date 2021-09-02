using Beamable.Editor.UI.Buss;
using Beamable.Server.Editor.UI.Components;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Microservice.UI.Components
{
    public class MicroserviceComponent : BeamableVisualElement
    {
        public MicroserviceComponent(string name) : base($"{Constants.COMP_PATH}/{name}/{name}")
        {

        }
    }
}