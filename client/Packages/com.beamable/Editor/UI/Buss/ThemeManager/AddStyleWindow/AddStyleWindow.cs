using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Editor.UI.Buss
{
	public class AddStyleWindow : BussWindowBase<AddStyleWindow, AddPropertiesVisualElement>
	{
		private Action<BussStyleRule> _onSelectorAdded;
		private List<BussStyleSheet> _styleSheets;

		public void Init(Action<BussStyleRule> onSelectorAdded, List<BussStyleSheet> activeStyleSheets)
		{
			_onSelectorAdded = onSelectorAdded;
			_styleSheets = activeStyleSheets;

			titleContent = new GUIContent(BussConstants.AddStyleWindowHeader);
			minSize = maxSize = BussConstants.AddStyleWindowSize;
			position = new Rect((Screen.width + minSize.x) * 0.5f, Screen.width * 0.5f, minSize.x, minSize.y);

			Refresh();
		}
		protected override AddPropertiesVisualElement GetVisualElement() => new AddPropertiesVisualElement(_onSelectorAdded, _styleSheets);
	}
}
