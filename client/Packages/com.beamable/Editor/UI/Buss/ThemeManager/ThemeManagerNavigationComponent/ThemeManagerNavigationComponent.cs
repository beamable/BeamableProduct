using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
using System.Collections.Generic;
#region
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#endif



using static Beamable.Common.Constants.Features.Buss.ThemeManager;

#endregion

namespace Beamable.Editor.UI.Components
{
	public class ThemeManagerNavigationComponent : BeamableBasicVisualElement
	{
		private readonly List<IndentedLabelVisualElement> _spawnedLabels = new List<IndentedLabelVisualElement>();
		private bool _hasDelayedChangeCallback;
		private ScrollView _hierarchyContainer;
		private IndentedLabelVisualElement _selectedLabel;
		private readonly ThemeManagerModel _model;

		public ThemeManagerNavigationComponent(ThemeManagerModel model) : base(
			$"{BUSS_THEME_MANAGER_PATH}/ThemeManagerNavigationComponent/ThemeManagerNavigationComponent.uss")
		{
			_model = model;
		}

		public override void Init()
		{
			base.Init();
			
			VisualElement header = new VisualElement {name = "header"};
			TextElement label = new TextElement {name = "headerLabel", text = "Navigation"};
			header.Add(label);

			header.RegisterCallback<MouseDownEvent>(evt =>
			{
				_hierarchyContainer.ToggleInClassList("hidden");
			});

			Root.Add(header);

			_hierarchyContainer = new ScrollView {name = "elementsContainer"};
			Root.Add(_hierarchyContainer);

			_model.Change += Refresh;

			Refresh();
		}

		public override void Refresh()
		{
			foreach (IndentedLabelVisualElement child in _spawnedLabels)
			{
				child.Destroy();
			}

			_spawnedLabels.Clear();
			_hierarchyContainer.Clear();
			
			foreach (KeyValuePair<BussElement,int> pair in _model.FoundElements)
			{
				IndentedLabelVisualElement label = new IndentedLabelVisualElement();
				label.Setup(pair.Key, BussNameUtility.FormatLabel(pair.Key), _model.NavigationElementClicked,
				            pair.Value, IndentedLabelVisualElement.DEFAULT_SINGLE_INDENT_WIDTH, pair.Key == _model.SelectedElement);
				label.Init();
				_spawnedLabels.Add(label);
				_hierarchyContainer.Add(label);
			}
		}

		protected override void OnDestroy()
		{
			_model.Change -= Refresh;
		}
	}
}
