
namespace UnityEngine.UIElements
{
	public class ContextLeftClickEvent : MouseEventBase<ContextClickEvent>
	{
		public ContextLeftClickEvent()
		{
			pressedButtons = 0;
			button = 0;
			clickCount = 0;
			mousePosition = new Vector2(0, 0);
			modifiers = EventModifiers.None;
		}
	}
}

