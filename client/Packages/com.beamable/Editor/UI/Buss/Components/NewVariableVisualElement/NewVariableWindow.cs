using System;
using Beamable.Editor.Toolbox.Components;
using Beamable.UI.Buss;
using UnityEngine;

namespace Beamable.Editor.UI.Buss.Components
{
	public class NewVariableWindow : BUSSWindowBase<NewVariableWindow, NewVariableVisualElement>
	{
		private Action<string, IBussProperty> _onPropertyCreated;

		public void Init(Action<string, IBussProperty> onPropertyCreated)
		{
			_onPropertyCreated = onPropertyCreated;
			
			titleContent = new GUIContent("New Variable Window");
			minSize = new Vector2(720, 400);
			maxSize = minSize;
			position = new Rect((Screen.width + minSize.x) * 0.5f, Screen.width * 0.5f, minSize.x, minSize.y);
			
			Refresh();
		}
		protected override NewVariableVisualElement GetVisualElement() => new NewVariableVisualElement(_onPropertyCreated);
	}
}
