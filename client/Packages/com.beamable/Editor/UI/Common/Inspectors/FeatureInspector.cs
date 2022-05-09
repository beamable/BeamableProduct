using Beamable.Common;
using Beamable.Editor.UI.Components;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beamable.Editor.UI.Common.Inspectors
{
	public abstract class FeatureInspector : UnityEditor.Editor
	{
		protected abstract string DocsURL { get; }
		protected abstract string Title { get; }
		protected abstract string Description { get; }

		public override VisualElement CreateInspectorGUI()
		{
			VisualElement rootObject = new VisualElement();
			rootObject.AddStyleSheet(Constants.Files.COMMON_USS_FILE);
			var popup = new PopupWindow {text = Title};
			popup.AddToClassList("inspectorHelp");
			VisualElement description = new Label(Description);
			description.AddToClassList("description");
			var button = new PrimaryButtonVisualElement();
			button.Refresh();
			button.SetText("Open Documentation");
			button.Button.clickable.clicked += () => Application.OpenURL(DocsURL);
			VisualElement defaultContainer = new VisualElement();
			popup.contentContainer.Add(description);
			popup.contentContainer.Add(button);
			rootObject.Add(popup);
			rootObject.Add(defaultContainer);
			
			InspectorElement.FillDefaultInspector(defaultContainer, serializedObject, this);

			return rootObject;
		}
	}
}
