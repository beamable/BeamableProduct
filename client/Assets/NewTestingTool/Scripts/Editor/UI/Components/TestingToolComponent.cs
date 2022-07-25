using Beamable.Editor.UI.Components;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

using static Beamable.Common.Constants.Features.Services;


namespace Beamable.Editor.NewTestingTool.UI.Components
{
	public class TestingToolComponent : BeamableVisualElement
	{
		public TestingToolComponent(string name) : base($"Assets/NewTestingTool/Scripts/Editor/UI/Components/{name}/{name}") { }
	}
}
