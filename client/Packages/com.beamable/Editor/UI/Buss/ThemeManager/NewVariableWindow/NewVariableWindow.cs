using Beamable.UI.Buss;
using System;
using UnityEngine;

namespace Beamable.Editor.UI.Buss
{
	public class NewVariableWindow : WindowBase<NewVariableWindow, NewVariableVisualElement>
	{
		private Action<string, IBussProperty> _onPropertyCreated;
		private BussStyleDescription _styleRule;

		public void Init(BussStyleDescription styleRule, Action<string, IBussProperty> onPropertyCreated)
		{
			_styleRule = styleRule;
			_onPropertyCreated = onPropertyCreated;

			titleContent = new GUIContent("New Variable Window");
			minSize = new Vector2(720, 400);
			maxSize = minSize;
			position = new Rect((Screen.width + minSize.x) * 0.5f, Screen.width * 0.5f, minSize.x, minSize.y);

			Refresh();
		}
		protected override NewVariableVisualElement GetVisualElement() => new NewVariableVisualElement(_styleRule, _onPropertyCreated);
	}
}
