using Beamable.Editor.UI.Common;
using Beamable.Editor.UI.Components;
using System;
using UnityEngine.UIElements;
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Buss
{
	public class BussThemeManagerActionBarVisualElement : BeamableBasicVisualElement
	{
		private readonly Action _onAddStyleAction;
		private readonly Action _onCopyAction;

		public BussThemeManagerActionBarVisualElement(Action onAddStyleAction, Action onCopyAction) : base(
			$"{BUSS_THEME_MANAGER_PATH}/{nameof(BussThemeManagerActionBarVisualElement)}/{nameof(BussThemeManagerActionBarVisualElement)}.uss")
		{
			_onAddStyleAction = onAddStyleAction;
			_onCopyAction = onCopyAction;
		}

		public override void Init()
		{
			base.Init();

			VisualElement leftContainer = new VisualElement {name = "leftContainer"};
			Root.Add(leftContainer);

			leftContainer.Add(CreateLabeledIconButton("addStyle", "Add style", _onAddStyleAction));
			leftContainer.Add(CreateLabeledIconButton("duplicateStylesheet", "Duplicate", _onCopyAction));

			VisualElement rightContainer = new VisualElement {name = "rightContainer"};
			Root.Add(rightContainer);

			SearchBarVisualElement searchBarVisualElement = new SearchBarVisualElement {name = "searchBar"};
			rightContainer.Add(searchBarVisualElement);

			rightContainer.Add(CreateIconButton("refresh", null));
			rightContainer.Add(CreateIconButton("doc", null));
		}

		private Button CreateLabeledIconButton(string buttonName, string buttonLabel, Action onClick)
		{
			Button button = new Button();
			button.AddToClassList("boundedButton");
			button.name = $"{buttonName}Button";
			if (onClick != null)
			{
				button.clickable.clicked += onClick;
			}

			Image icon = new Image();
			icon.AddToClassList("iconLabelButton");
			icon.name = $"{buttonName}Icon";
			button.Add(icon);

			Label label = new Label();
			label.AddToClassList("buttonText");
			label.text = buttonLabel;
			button.Add(label);

			return button;
		}

		private Button CreateIconButton(string buttonName, Action onClick)
		{
			Button button = new Button();
			button.AddToClassList("unboundedButton");
			button.name = $"{buttonName}Button";
			if (onClick != null)
			{
				button.clickable.clicked += onClick;
			}

			Image icon = new Image();
			icon.AddToClassList("iconButton");
			icon.name = $"{buttonName}Icon";
			button.Add(icon);

			return button;
		}
	}
}
