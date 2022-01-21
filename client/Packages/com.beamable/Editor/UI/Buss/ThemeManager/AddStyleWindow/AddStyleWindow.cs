using Beamable.Editor.UI.Buss;
using Beamable.UI.Buss;
using System;
using UnityEngine;

namespace Beamable.UI.BUSS
{
	public class AddStyleWindow : BussWindowBase<AddStyleWindow, AddPropertiesVisualElement>
	{
		private Action<BussStyleRule> _onSelectorAdded;

		public void Init(Action<BussStyleRule> onSelectorAdded)
		{
			_onSelectorAdded = onSelectorAdded;

			titleContent = new GUIContent(BussConstants.AddStyleWindowHeader);
			minSize = maxSize = BussConstants.AddStyleWindowSize;
			position = new Rect((Screen.width + minSize.x) * 0.5f, Screen.width * 0.5f, minSize.x, minSize.y);

			Refresh();
		}
		protected override AddPropertiesVisualElement GetVisualElement() => new AddPropertiesVisualElement(_onSelectorAdded);
	}
}
