using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
using System;
using System.Diagnostics;
using System.Linq;
#if UNITY_2018
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
using static Beamable.Common.Constants.Features.Buss.ThemeManager;
using Debug = UnityEngine.Debug;

namespace Beamable.Editor.UI.Components
{
	[DebuggerDisplay("Label = {_label} Depth = {Level}")]
	public class IndentedLabelVisualElement : BeamableBasicVisualElement
	{
		public const float DEFAULT_SINGLE_INDENT_WIDTH = 20.0f;

		private float _singleIndentWidth;
		public int Level { get; private set; }
		private string _label;

		private Action<BussElement> _onMouseClicked;
		public Action<bool> OnFoldChanged;

		private VisualElement _container;
		private BussElement _bussElement;
		private bool _selected;
		public bool IsFolded { get; private set; }

		public IndentedLabelVisualElement() : base(
			$"{BUSS_THEME_MANAGER_PATH}/{nameof(NavigationVisualElement)}/{nameof(IndentedLabelVisualElement)}/{nameof(IndentedLabelVisualElement)}.uss")
		{ }

		public void Setup(BussElement bussElement,
						  string label,
						  Action<BussElement> onMouseClicked,
						  int level,
						  float width,
						  bool selected,
						  bool isFolded)
		{
			IsFolded = isFolded;
			_bussElement = bussElement;
			_onMouseClicked = onMouseClicked;

			_label = label;
			Level = level;
			_singleIndentWidth = width;
			_selected = selected;
		}

		public override void Init()
		{
			base.Init();

			_container = new VisualElement { name = "indentedLabelContainer" };

			var elem = new VisualElement { name="element"};
			
			var typeLabel = new TextElement { name = "typeLabel", text = _bussElement.TypeName };
			_container.SetSelected(_selected);

			var idLabel = new TextElement {name = "idLabel", text = BussNameUtility.AsIdSelector(_bussElement.Id) };
			var classLabel = new TextElement {name = "classLabel", text = BussNameUtility.ClassListString(_bussElement.Classes)};

			var iconContainer = new VisualElement {name = "iconContainer"};
			Image foldIcon = new Image { name = "foldIcon" };
			foldIcon.EnableInClassList("folded", IsFolded);
			foldIcon.EnableInClassList("unfolded", !IsFolded);
			
			iconContainer.RegisterCallback<MouseDownEvent>(e =>
			{
				IsFolded = !IsFolded;
				foldIcon.EnableInClassList("folded", IsFolded);
				foldIcon.EnableInClassList("unfolded", !IsFolded);

				OnFoldChanged?.Invoke(IsFolded);
				e.StopPropagation();
				e.PreventDefault();
			});
			
			iconContainer.Add(foldIcon);

			if (_bussElement.Children.Any())
			{
				elem.Add(iconContainer);
			}
			
			elem.Add(typeLabel);
			elem.Add(idLabel);
			
			
			elem.Add(classLabel);

			float width = (_singleIndentWidth * Level) + _singleIndentWidth;

#if UNITY_2018
			elem.SetLeft(width);
#elif UNITY_2019_1_OR_NEWER
			elem.style.paddingLeft = new StyleLength(width);
#endif

			_container.Add(elem);
			
			Root.Add(_container);

			_container.RegisterCallback<MouseDownEvent>(OnMouseClicked);
			_container.RegisterCallback<MouseOverEvent>(OnMouseOver);
			_container.RegisterCallback<MouseOutEvent>(OnMouseOut);
		}

		protected override void OnDestroy()
		{
			_container?.UnregisterCallback<MouseDownEvent>(OnMouseClicked);
			_container?.UnregisterCallback<MouseOverEvent>(OnMouseOver);
			_container?.UnregisterCallback<MouseOutEvent>(OnMouseOut);
		}

		private void OnMouseOver(MouseOverEvent evt)
		{
			if (!Root.IsSelected())
			{
				Root.SetHovered(true);
			}
		}

		private void OnMouseOut(MouseOutEvent evt)
		{
			Root.SetHovered(false);
		}

		private void OnMouseClicked(MouseDownEvent evt)
		{
			_onMouseClicked?.Invoke(_bussElement);
		}
	}
}
