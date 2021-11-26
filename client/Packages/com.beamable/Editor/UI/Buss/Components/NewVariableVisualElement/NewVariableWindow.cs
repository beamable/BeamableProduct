using Beamable.Editor.Toolbox.Components;
using UnityEngine;

namespace Beamable.Editor.UI.Buss.Components
{
	public class NewVariableWindow : BUSSWindowBase<NewVariableWindow, NewVariableVisualElement>
	{
		protected override void Init(NewVariableWindow wnd)
		{
			wnd.titleContent = new GUIContent("New Variable Window");
			wnd.minSize = new Vector2(620, 400);
			wnd.maxSize = wnd.minSize;
			wnd.position = new Rect((Screen.width + wnd.minSize.x) * 0.5f, Screen.width * 0.5f, wnd.minSize.x, wnd.minSize.y);
		}
		protected override NewVariableVisualElement GetVisualElement() => new NewVariableVisualElement();
	}
}
