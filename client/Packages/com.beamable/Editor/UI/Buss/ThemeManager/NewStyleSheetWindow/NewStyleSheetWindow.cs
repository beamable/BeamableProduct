using Beamable.UI.Buss;
using UnityEngine;

namespace Beamable.Editor.UI.Buss
{
	public class NewStyleSheetWindow : WindowBase<NewStyleSheetWindow, NewStyleSheetVisualElement>
	{
		private BussStyleRule _initialRule;

		public void Init(BussStyleRule initialRule)
		{
			titleContent = new GUIContent("New Style Sheet Window");
			minSize = new Vector2(720, 200);
			maxSize = minSize;
			position = new Rect((Screen.width + minSize.x) * 0.5f, Screen.width * 0.5f, minSize.x, minSize.y);

			_initialRule = initialRule;

			Refresh();
		}
		
		protected override NewStyleSheetVisualElement GetVisualElement() => new NewStyleSheetVisualElement(_initialRule);
	}
}
