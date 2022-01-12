using Beamable.Editor.Toolbox.Components;
using Beamable.UI.Buss;
using UnityEngine;

namespace Beamable.Editor.UI.Buss.Components
{
	public class NewVariableWindow : BUSSWindowBase<NewVariableWindow, NewVariableVisualElement>
	{
		private BussStyleDescription _description;

		public void Init(BussStyleDescription description)
		{
			_description = description;
			
			titleContent = new GUIContent("New Variable Window");
			minSize = new Vector2(720, 400);
			maxSize = minSize;
			position = new Rect((Screen.width + minSize.x) * 0.5f, Screen.width * 0.5f, minSize.x, minSize.y);
			
			Refresh();
		}
		protected override NewVariableVisualElement GetVisualElement() => new NewVariableVisualElement(_description);
	}
}
