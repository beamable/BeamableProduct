using Beamable.Editor.Toolbox.Components;
using Beamable.UI.Buss;
using Editor.UI.Buss;
using System;
using System.Linq;
using UnityEngine;

namespace Beamable.Editor.UI.Buss.Components
{
	public class AddSelectorWindow : BUSSWindowBase<AddSelectorWindow, AddSelectorVisualElement>
	{
		private Action<BussStyleRule> _onSelectorAdded;
		
		public void Init(Action<BussStyleRule> onSelectorAdded)
		{
			_onSelectorAdded = onSelectorAdded;
			
			titleContent = new GUIContent("Add Selector Window");
			minSize = new Vector2(520, 620);
			maxSize = minSize;
			position = new Rect((Screen.width + minSize.x) * 0.5f, Screen.width * 0.5f, minSize.x, minSize.y);
			
			Refresh();
		}
		protected override AddSelectorVisualElement GetVisualElement() => new AddSelectorVisualElement(_onSelectorAdded);
	}
}
