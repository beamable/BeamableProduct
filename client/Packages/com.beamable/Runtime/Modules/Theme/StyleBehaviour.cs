using Beamable.Theme.Appliers;
using UnityEngine;

namespace Beamable.Theme
{
	[ExecuteInEditMode]
	public class StyleBehaviour : MonoBehaviour
	{
		public ImageStyleApplier StyledImages;
		public TextStyleApplier StyledTexts;
		public GradientStyleApplier StyledGradients;
		public LayoutStyleApplier StyledLayouts;
		public SelectableStyleApplier StyledSelectables;
		public ButtonStyleApplier StyledButtons;
		public TransformStyleApplier StyledTransforms;
		public WindowStyleApplier StyledWindow;
		public SoundStyleApplier StyledSounds;
		public StringStyleApplier StyledStrings;

#if UNITY_EDITOR && BEAMABLE_DEVELOPER
		private string _lastHash = "";
		private ThemeObject _lastTheme;
#endif

		public void Refresh()
		{
			if (!gameObject.activeInHierarchy || !isActiveAndEnabled || !gameObject.scene.IsValid()) return;  // OnValidate runs on prefabs, which we absolutely don't want.

			var theme = ThemeConfiguration.Instance.Style;
#if UNITY_EDITOR && BEAMABLE_DEVELOPER
			_lastTheme = theme;
			_lastHash = theme.Hash;
#endif

			StyledImages?.ApplyAll(theme);
			StyledTexts?.ApplyAll(theme);
			StyledGradients?.ApplyAll(theme);
			StyledLayouts?.ApplyAll(theme);
			StyledSelectables?.ApplyAll(theme);
			StyledButtons?.ApplyAll(theme);
			StyledTransforms?.ApplyAll(theme);
			StyledWindow?.ApplyAll(theme);
			StyledSounds?.ApplyAll(theme);
			StyledStrings?.ApplyAll(theme);
		}

		private void OnEnable()
		{
			Refresh();
		}

#if UNITY_EDITOR && BEAMABLE_DEVELOPER
		void Update()
		{
			if (!gameObject.activeInHierarchy) return;

			var theme = ThemeConfiguration.Instance.Style;

			if (_lastHash != theme.Hash || theme != _lastTheme)
			{
				Refresh();
			}
		}

		private void OnValidate()
		{
			if (!gameObject.activeInHierarchy || Application.isPlaying) return;

			Refresh();
		}
#endif
	}
}
