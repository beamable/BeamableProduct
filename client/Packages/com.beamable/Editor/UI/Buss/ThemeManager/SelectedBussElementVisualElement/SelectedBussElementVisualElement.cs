using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_2018
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#endif

using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Components
{
	// TODO: TD000003
	public class SelectedBussElementVisualElement : BeamableBasicVisualElement
	{
		private const float MIN_CONTENT_HEIGHT = 120.0f;
		private const float SINGLE_CLASS_ENTRY_HEIGHT = 24.0f;

		private LabeledTextField _idField;
		private LabeledObjectField _currentStyleSheet;
		private ListView _classesList;
		private ThemeManagerNavigationComponent _navigationWindow;
		private BussElement _currentBussElement;
		private int? _selectedClassListIndex;
		private VisualElement _contentContainer;

		public SelectedBussElementVisualElement() : base(
			$"{BUSS_THEME_MANAGER_PATH}/SelectedBussElementVisualElement/SelectedBussElementVisualElement.uss")
		{ }

		public void Setup(ThemeManagerNavigationComponent navigationWindow)
		{
			_navigationWindow = navigationWindow;
			_navigationWindow.SelectionChanged += OnBussElementChanged;
			_navigationWindow.SelectionCleared += OnSelectionCleared;
			EditorApplication.hierarchyChanged += OnHierarchyChanged;
			Init();
		}

		public override void Init()
		{
			base.Init();

			VisualElement header = new VisualElement();
			header.AddToClassList("header");
			TextElement label = new TextElement();
			label.AddToClassList("headerLabel");
			label.text = "Selected Buss Element";
			header.Add(label);

			header.RegisterCallback<MouseDownEvent>(evt =>
			{
				_contentContainer.ToggleInClassList("hidden");
				RefreshHeight();
			});

			Root.Add(header);

			_contentContainer = new VisualElement();

			_idField = new LabeledTextField();
			_idField.Setup("Id", String.Empty, OnValueChanged, true);
			_idField.Refresh();
			_contentContainer.Add(_idField);

			Label classesLabel = new Label("Classes");
			classesLabel.AddToClassList("classesLabel");
			_contentContainer.Add(classesLabel);

			_classesList = CreateClassesList();
			_classesList.RefreshPolyfill();
			_contentContainer.Add(_classesList);

			CreateButtons();

			_currentStyleSheet = new LabeledObjectField();
			_currentStyleSheet.Setup("Style sheet", typeof(BussStyleSheet), OnStylesheetChanged);
			_contentContainer.Add(_currentStyleSheet);
			Root.Add(_contentContainer);

			RefreshHeight();
		}

		private void CreateButtons()
		{
			VisualElement buttonsContainer = new VisualElement { name = "buttonsContainer" };

			VisualElement removeButton = new VisualElement { name = "removeButton" };
			removeButton.AddToClassList("button");
			removeButton.RegisterCallback<MouseDownEvent>(RemoveClassButtonClicked);
			buttonsContainer.Add(removeButton);

			VisualElement addButton = new VisualElement { name = "addButton" };
			addButton.AddToClassList("button");
			addButton.RegisterCallback<MouseDownEvent>(AddClassButtonClicked);
			buttonsContainer.Add(addButton);

			_contentContainer.Add(buttonsContainer);
		}

		private void AddClassButtonClicked(MouseDownEvent evt)
		{
			if (_currentBussElement == null)
			{
				return;
			}

			_currentBussElement.AddClass("");
			RefreshClassesList();
			RefreshHeight();
			_navigationWindow.RefreshSelectedLabel();
		}

		private void RemoveClassButtonClicked(MouseDownEvent evt)
		{
			if (_selectedClassListIndex == null || _currentBussElement == null)
			{
				return;
			}

			string className = (string)_classesList.itemsSource[(int)_selectedClassListIndex];

			if (className.StartsWith("."))
			{
				className = className.Remove(0, 1);
			}

			_currentBussElement.RemoveClass(className);
			RefreshClassesList();
			RefreshHeight();
			_navigationWindow.RefreshSelectedLabel();
		}

		private void OnHierarchyChanged()
		{
			_currentBussElement = null;
			_selectedClassListIndex = null;

			_idField.Value = string.Empty;
			_currentStyleSheet.Reset();

			RefreshClassesList();
			RefreshHeight();
		}

		private void OnBussElementChanged(GameObject current)
		{
			_currentBussElement = current.GetComponent<BussElement>();
			_selectedClassListIndex = null;

			if (_currentBussElement != null)
			{
				_idField.Value = _currentBussElement.Id;
				_currentStyleSheet.SetValue(_currentBussElement.StyleSheet);
				RefreshClassesList();
			}

			RefreshHeight();
		}

		private void OnSelectionCleared()
		{
			_currentBussElement = null;
			_selectedClassListIndex = null;
			_idField.Value = null;
			_currentStyleSheet.Reset();
			RefreshClassesList();
			RefreshHeight();
		}

		private void OnStylesheetChanged(Object styleSheet)
		{
			BussStyleSheet newStyleSheet = (BussStyleSheet)styleSheet;

			if (_currentBussElement != null)
			{
				_currentBussElement.StyleSheet = newStyleSheet;
			}
		}

		private void RefreshHeight()
		{
			_classesList.style.SetHeight(0.0f);

			float height = MIN_CONTENT_HEIGHT;

			if (_contentContainer.ClassListContains("hidden"))
			{
				_contentContainer.style.SetHeight(0.0f);
				return;
			}

			if (_currentBussElement != null)
			{
				float allClassesHeight = SINGLE_CLASS_ENTRY_HEIGHT * _currentBussElement.Classes.Count();
				height += allClassesHeight;

				_classesList.style.SetHeight(allClassesHeight);
			}

			_contentContainer.style.SetHeight(height);
		}

		private ListView CreateClassesList()
		{
			ListView view = new ListView
			{
				makeItem = CreateListViewElement,
				bindItem = BindListViewElement,
				selectionType = SelectionType.Single,
				itemsSource = _currentBussElement != null
					? _currentBussElement.Classes.ToList()
					: new List<string>()
			};
			view.SetItemHeight(24);
			view.name = "classesList";

#if UNITY_2020_1_OR_NEWER
			view.onSelectionChange += SelectionChanged;
#else
			view.onSelectionChanged += SelectionChanged;
#endif

			return view;
		}

#if UNITY_2020_1_OR_NEWER
		private void SelectionChanged(IEnumerable<object> obj)
		{
			List<string> list = (List<string>)_classesList.itemsSource;
			List<object> objects = obj.ToList();
			_selectedClassListIndex = list.FindIndex(el => el == (string)objects[0]);
		}
#else
		private void SelectionChanged(List<object> obj)
		{
			List<string> list = (List<string>)_classesList.itemsSource;
			_selectedClassListIndex = list.FindIndex(el => el == (string)obj[0]);
		}
#endif

		private void RefreshClassesList()
		{
			_classesList.itemsSource = _currentBussElement
				? BussNameUtility.AsClassesList(_currentBussElement.Classes.ToList())
				: new List<string>();
			_classesList.RefreshPolyfill();
		}

		private VisualElement CreateListViewElement()
		{
			VisualElement classElement = new VisualElement { name = "classElement" };
			classElement.Add(new VisualElement { name = "space" });
			classElement.Add(new TextField());
			return classElement;
		}

		private void BindListViewElement(VisualElement element, int index)
		{
			TextField textField = (TextField)element.Children().ToList()[1];
			textField.value = BussNameUtility.AsClassSelector(_classesList.itemsSource[index] as string);
			textField.isDelayed = true;

#if UNITY_2018
			textField.OnValueChanged(ClassValueChanged);
#elif UNITY_2019_1_OR_NEWER
			textField.RegisterValueChangedCallback(ClassValueChanged);
#endif

			void ClassValueChanged(ChangeEvent<string> evt)
			{
				string newValue = BussNameUtility.AsClassSelector(evt.newValue);
				_classesList.itemsSource[index] = newValue;
				textField.SetValueWithoutNotify(newValue);
				_currentBussElement.UpdateClasses(BussNameUtility.AsCleanList((List<string>)_classesList.itemsSource));
				_navigationWindow.RefreshSelectedLabel();
			}
		}

		private void OnValueChanged()
		{
			if (_currentBussElement == null)
			{
				return;
			}

			string value = BussNameUtility.AsIdSelector(_idField.Value);
			_idField.SetWithoutNotify(value);
			_currentBussElement.Id = BussNameUtility.CleanString(value);
			_navigationWindow.RefreshSelectedLabel();
		}

		protected override void OnDestroy()
		{
			if (_navigationWindow != null)
			{
				_navigationWindow.SelectionChanged -= OnBussElementChanged;
				_navigationWindow.SelectionCleared -= OnSelectionCleared;
			}

			EditorApplication.hierarchyChanged -= OnHierarchyChanged;
		}
	}
}
