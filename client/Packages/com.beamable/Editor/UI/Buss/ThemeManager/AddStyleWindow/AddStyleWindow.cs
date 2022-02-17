using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Buss
{
	public class AddStyleWindow : WindowBase<AddStyleWindow, AddPropertiesVisualElement>
	{
		private Action<BussStyleRule> _onSelectorAdded;
		private List<BussStyleSheet> _styleSheets;

		public void Init(Action<BussStyleRule> onSelectorAdded, List<BussStyleSheet> activeStyleSheets)
		{
			_onSelectorAdded = onSelectorAdded;
			_styleSheets = activeStyleSheets;

			titleContent = new GUIContent(ADD_STYLE_WINDOW_HEADER);
			minSize = maxSize = ADD_STYLE_WINDOW_SIZE;
			position = new Rect((Screen.width + minSize.x) * 0.5f, Screen.width * 0.5f, minSize.x, minSize.y);

			Refresh();
		}
		protected override AddPropertiesVisualElement GetVisualElement() => new AddPropertiesVisualElement(_onSelectorAdded, _styleSheets);
	}
}
