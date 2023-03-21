using Beamable.Editor.UI.Buss;
using Beamable.UI.Buss;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Beamable.Editor.UI.Components
{
	public class NavigationVisualElement : ThemeManagerBasicComponent
	{
		private readonly List<IndentedLabelVisualElement> _spawnedLabels = new List<IndentedLabelVisualElement>();
		private bool _hasDelayedChangeCallback;
		private ScrollView _hierarchyContainer;
		private IndentedLabelVisualElement _selectedLabel;
		private readonly ThemeManagerModel _model;

		public NavigationVisualElement(ThemeManagerModel model) : base(nameof(NavigationVisualElement))
		{
			_model = model;
		}

		public override void Init()
		{
			base.Init();

			VisualElement header = new VisualElement { name = "header" };

			Image foldIcon = new Image { name = "foldIcon" };
			foldIcon.AddToClassList("unfolded");
			header.Add(foldIcon);

			TextElement label = new TextElement { name = "headerLabel", text = "Navigation" };
			header.Add(label);

			header.RegisterCallback<MouseDownEvent>(evt =>
			{
				_hierarchyContainer.ToggleInClassList("hidden");
				foldIcon.ToggleInClassList("unfolded");
				foldIcon.ToggleInClassList("folded");
			});

			Root.Add(header);

			_hierarchyContainer = new ScrollView { name = "elementsContainer" };
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

			var totalCount = 0;
			var foldedAtDepth = 0;
			var isParentFolded = false;
			foreach (KeyValuePair<BussElement, int> pair in _model.FoundElements)
			{
				IndentedLabelVisualElement label = new IndentedLabelVisualElement();
				var selfDepth = pair.Value;
				var initialFold = _model.GetElementFolded(pair.Key);
				label.Setup(pair.Key, BussNameUtility.GetFormattedLabel(pair.Key), _model.NavigationElementClicked,
							pair.Value, IndentedLabelVisualElement.DEFAULT_SINGLE_INDENT_WIDTH, pair.Key == _model.SelectedElement, initialFold);
				label.Init();
				var index = totalCount;
				totalCount++;

				void HandleFold(bool isFolded)
				{
					_model.SetElementFold(pair.Key, isFolded);

					var stack = new Stack<IndentedLabelVisualElement>();
					stack.Push(label);
					
					
					for (var i = index + 1; i < _spawnedLabels.Count; i++)
					{
						// if the stack is empty, we exit.
						if (!stack.Any())
						{
							break;
						}
						
						var master = stack.Peek();
						var curr = _spawnedLabels[i];

						// if the current element matches the parent depth, we step out.
						if (curr.Level <= master.Level)
						{
							stack.Pop();
							i--; // go back one element so we can re-process with the parent context
							continue;
						}
						
						curr.EnableInClassList("hidden", master.IsFolded);
						if (!master.IsFolded && curr.IsFolded)
						{
							stack.Push(curr);
						}
					}
					
				}

				label.OnFoldChanged += HandleFold;
				_spawnedLabels.Add(label);
				_hierarchyContainer.Add(label);
				

				label.EnableInClassList("hidden", isParentFolded);
				if (selfDepth <= foldedAtDepth)
				{
					isParentFolded = initialFold;
					foldedAtDepth = selfDepth;
					label.EnableInClassList("hidden", false);
				}
				else if (!isParentFolded && initialFold)
				{
					foldedAtDepth = selfDepth;
					isParentFolded = true;
				}
				
			}
		}

		protected override void OnDestroy()
		{
			_model.Change -= Refresh;
		}
	}
}
