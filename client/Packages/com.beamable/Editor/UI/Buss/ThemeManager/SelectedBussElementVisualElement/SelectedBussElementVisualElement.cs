using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using System.Linq;
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
		private VisualElement _mainContainer;
		private LabeledTextField _idField;
		private ListView _classesList;
		private Foldout _foldout;
		private BussElementHierarchyVisualElement _navigationWindow;
		private BussElement _currentBussElement;
		private List<string> _classes = new List<string>();

		public SelectedBussElementVisualElement() : base(
			$"{BUSS_THEME_MANAGER_PATH}/SelectedBussElementVisualElement/SelectedBussElementVisualElement.uss") { }

		public void Setup(BussElementHierarchyVisualElement navigationWindow)
		{
			_navigationWindow = navigationWindow;
			_navigationWindow.SelectionChanged += OnSelectionChanged;

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

			Button addButton = new Button {name = "addButton"};
			buttonsContainer.Add(addButton);
			
			Button removeButton = new Button {name = "removeButton"};
			buttonsContainer.Add(removeButton);

			Root.Add(buttonsContainer);
		}

		private void OnSelectionChanged(GameObject current)
		{
			_currentBussElement = current.GetComponent<BussElement>();
			_classes.Clear();

			if (_currentBussElement != null)
			{
				_idField.Value = _currentBussElement.Id;
				_classes = _currentBussElement.Classes.ToList();
			}

			RefreshClassesList();
			RefreshHeight();
		}

		private void RefreshHeight()
		{
			_classesList.SetHeight(0.0f);
			
			float height = 130.0f;

			if (_currentBussElement != null)
			{
				float allClassesHeight = 24 * _currentBussElement.Classes.Count();
				height += allClassesHeight;
				
				_classesList.SetHeight(allClassesHeight);
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
				itemsSource = _classes
			};
			view.name = "classesList";

			return view;
		}

		private void RefreshClassesList()
		{
			_classesList.itemsSource = _classes;
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

			// consoleLogVisualElement.SetNewModel(_listView.itemsSource[index] as LogMessage);
			// if (index % 2 == 0)
			// {
			// 	consoleLogVisualElement.RemoveFromClassList("oddRow");
			// }
			// else
			// {
			// 	consoleLogVisualElement.AddToClassList("oddRow");
			// }
			// consoleLogVisualElement.MarkDirtyRepaint();
		}

		private void OnValueChanged() { }
	}
}
