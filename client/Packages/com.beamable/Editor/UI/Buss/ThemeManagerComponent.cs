using Beamable.Editor.UI.Common;
using Beamable.Editor.UI.Components;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Buss
{
	/// <summary>
	/// <see cref="ThemeManagerBasicComponent"/> are those using only USS style files
	/// </summary>
	public class ThemeManagerBasicComponent : BeamableBasicVisualElement
	{
		public ThemeManagerBasicComponent(string name, bool createRoot = true) : base(
			$"{BUSS_THEME_MANAGER_PATH}/{name}/{name}.uss", createRoot) { }
	}

	/// <summary>
	/// <see cref="ThemeManagerComponent"/> are those using both UXML and USS style files
	/// </summary>
	public class ThemeManagerComponent : BeamableVisualElement
	{
		public ThemeManagerComponent(string name) : base($"{BUSS_THEME_MANAGER_PATH}/{name}/{name}") { }
	}
}
