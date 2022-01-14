using Beamable.Editor.Toolbox.Components;
using Beamable.UI.Buss;
using UnityEngine;

namespace Beamable.Editor.UI.Buss.Components
{
	public class NewRuleWindow : BUSSWindowBase<NewRuleWindow, NewRuleVisualElement>
	{
		public void Init()
		{
			titleContent = new GUIContent("New Rule Window");
			minSize = new Vector2(720, 400);
			maxSize = minSize;
			position = new Rect((Screen.width + minSize.x) * 0.5f, Screen.width * 0.5f, minSize.x, minSize.y);
			
			Refresh();
		}
		protected override NewRuleVisualElement GetVisualElement() => new NewRuleVisualElement();
	}
}
