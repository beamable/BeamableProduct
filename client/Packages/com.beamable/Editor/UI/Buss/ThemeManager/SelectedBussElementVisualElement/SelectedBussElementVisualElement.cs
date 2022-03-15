using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
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
	public class SelectedBussElementVisualElement : BeamableBasicVisualElement
	{
		private LabeledTextField _idField;
		private ListView _classesList;
		private BussElementHierarchyVisualElement _navigationWindow;
		private BussElement _currentBussElement;
		private int? _selectedClassListIndex;

		public SelectedBussElementVisualElement() : base(
			$"{BUSS_THEME_MANAGER_PATH}/SelectedBussElementVisualElement/SelectedBussElementVisualElement.uss") { }

		public void Setup(BussElementHierarchyVisualElement navigationWindow)
		{
			_navigationWindow = navigationWindow;
			_navigationWindow.SelectionChanged += OnSelectionChanged;
			
			EditorApplication.hierarchyChanged += OnHierarchyChanged;
			Init();
		}

		public override void Init()
		{
			base.Init();

			VisualElement header = new VisualElement();
			header.name = "header";
			TextElement label = new TextElement();
			label.name = "headerLabel";
			label.text = "Selected Buss Element";
			header.Add(label);
			Root.Add(header);

			_idField = new LabeledTextField();
			_idField.Setup("Id", String.Empty, OnValueChanged);
			_idField.Refresh();
			Root.Add(_idField);

			Label classesLabel = new Label("Classes");
			classesLabel.name = "classesLabel";
			Root.Add(classesLabel);

			_classesList = CreateClassesList();
			_classesList.Refresh();
			Root.Add(_classesList);

			CreateButtons();
			RefreshHeight();
		}

		private void CreateButtons()
		{
			VisualElement buttonsContainer = new VisualElement {name = "buttonsContainer"};

			VisualElement removeButton = new VisualElement {name = "removeButton"};
			removeButton.AddToClassList("button");
			removeButton.RegisterCallback<MouseDownEvent>(RemoveClassButtonClicked);
			buttonsContainer.Add(removeButton);

			VisualElement addButton = new VisualElement {name = "addButton"};
			addButton.AddToClassList("button");
			addButton.RegisterCallback<MouseDownEvent>(AddClassButtonClicked);
			buttonsContainer.Add(addButton);

			Root.Add(buttonsContainer);
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

			_currentBussElement.RemoveClass((string)_classesList.itemsSource[(int)_selectedClassListIndex]);
			RefreshClassesList();
			RefreshHeight();
			_navigationWindow.RefreshSelectedLabel();
		}

		private void OnHierarchyChanged()
		{
			_currentBussElement = null;
			_selectedClassListIndex = null;

			_idField.Value = string.Empty;
			
			RefreshClassesList();
			RefreshHeight();
		}

		private void OnSelectionChanged(GameObject current)
		{
			_currentBussElement = current.GetComponent<BussElement>();
			_selectedClassListIndex = null;

			if (_currentBussElement != null)
			{
				_idField.Value = _currentBussElement.Id;
			}

			RefreshClassesList();
			RefreshHeight();
		}

		private void RefreshHeight()
		{
#if UNITY_2018
			_classesList.SetHeight(0.0f);
#elif UNITY_2019_1_OR_NEWER
			_classesList.style.SetHeight(0.0f);
#endif

			float height = 130.0f;

			if (_currentBussElement != null)
			{
				float allClassesHeight = 24 * _currentBussElement.Classes.Count();
				height += allClassesHeight;

#if UNITY_2018
				_classesList.SetHeight(allClassesHeight);
#elif UNITY_2019_1_OR_NEWER
				_classesList.style.SetHeight(allClassesHeight);
#endif
			}

			Root.style.SetHeight(height);
		}

		private ListView CreateClassesList()
		{
			ListView view = new ListView
			{
				makeItem = CreateListViewElement,
				bindItem = BindListViewElement,
				selectionType = SelectionType.Single,
				itemHeight = 24,
				itemsSource = _currentBussElement ? _currentBussElement.Classes.ToList() : new List<string>()
			};
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
			_classesList.itemsSource = _currentBussElement ? _currentBussElement.Classes.ToList() : new List<string>();
			_classesList.Refresh();
		}

		private VisualElement CreateListViewElement()
		{
			VisualElement classElement = new VisualElement {name = "classElement"};
			classElement.Add(new TextField());
			return classElement;
		}

		private void BindListViewElement(VisualElement element, int index)
		{
			TextField textField = (TextField)element.Children().ToList()[0];
			textField.value = _classesList.itemsSource[index] as string;

#if UNITY_2018
			textField.OnValueChanged(ClassValueChanged);
#elif UNITY_2019_1_OR_NEWER
			textField.RegisterValueChangedCallback(ClassValueChanged);
#endif

			void ClassValueChanged(ChangeEvent<string> evt)
			{
				_classesList.itemsSource[index] = evt.newValue;
				_currentBussElement.UpdateClasses((List<string>)_classesList.itemsSource);
				_navigationWindow.RefreshSelectedLabel();
			}
		}

		private void OnValueChanged()
		{
			if (_currentBussElement == null)
			{
				return;
			}
			
			_currentBussElement.Id = _idField.Value;
			_navigationWindow.RefreshSelectedLabel();
		}

		protected override void OnDestroy()
		{
			if (_navigationWindow != null)
			{
				_navigationWindow.SelectionChanged -= OnSelectionChanged;
			}

			EditorApplication.hierarchyChanged -= OnHierarchyChanged;
		}
	}
}
