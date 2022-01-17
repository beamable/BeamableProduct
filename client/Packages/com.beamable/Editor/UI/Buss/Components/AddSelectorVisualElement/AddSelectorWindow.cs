using Beamable.Editor.Toolbox.Components;
using UnityEngine;

namespace Beamable.Editor.UI.Buss.Components
{
	public class AddSelectorWindow : BUSSWindowBase<AddSelectorWindow, AddSelectorVisualElement>
	{
		public void Init()
		{
			titleContent = new GUIContent("Add Selector Window");
			minSize = new Vector2(520, 350);
			maxSize = minSize;
			position = new Rect((Screen.width + minSize.x) * 0.5f, Screen.width * 0.5f, minSize.x, minSize.y);
			
			Refresh();
		}
		protected override AddSelectorVisualElement GetVisualElement() => new AddSelectorVisualElement();
	}
}
