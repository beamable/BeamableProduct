using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.UIElements
{
	//
	// Summary:
	//     This custom event is sent when a key is pressed... I think. I also hate myself.
	public class CustomKeyDownEvent : KeyboardEventBase<CustomKeyDownEvent>
	{
		public char key;
		public CustomKeyDownEvent()
		{
			character = key;
			keyCode = KeyCode.A;//A for now
			modifiers = EventModifiers.None;
		}
	}
}
