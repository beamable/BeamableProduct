using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#endif
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Components
{
	// TODO: TD000003
	public class InlineStyleCardVisualElement : BeamableBasicVisualElement
	{
		private VisualElement _variableContainer;
		private VisualElement _propertyContainer;

		private VariableDatabase _variableDatabase;
		private PropertySourceDatabase _propertySourceDatabase;

		private List<BussStylePropertyVisualElement> _variables = new List<BussStylePropertyVisualElement>();
		private List<BussStylePropertyVisualElement> _properties = new List<BussStylePropertyVisualElement>();

		private BussElement _bussElement;

		public InlineStyleCardVisualElement(VariableDatabase variableDatabase,
											PropertySourceDatabase propertySourceDatabase) : base(
			$"{BUSS_THEME_MANAGER_PATH}/InlineStyleCardVisualElement/InlineStyleCardVisualElement.uss", false)
		{
			_variableDatabase = variableDatabase;
			_propertySourceDatabase = propertySourceDatabase;
		}

		public override void Init()
		{
			base.Init();

			VisualElement header = new VisualElement();
			header.AddToClassList("header");
			
			Image foldIcon = new Image {name = "foldIcon"};
			foldIcon.AddToClassList("folded");
			header.Add(foldIcon);
			
			TextElement label = new TextElement();
			label.AddToClassList("headerLabel");
			label.text = "Inline Style";
			header.Add(label);
			Root.Add(header);

			var mainContainer = new VisualElement();
			mainContainer.AddToClassList("mainContainer");
			mainContainer.AddToClassList("hidden");
			Root.Add(mainContainer);

			header.RegisterCallback<MouseDownEvent>(evt =>
			{
				mainContainer.ToggleInClassList("hidden");
				foldIcon.ToggleInClassList("unfolded");
				foldIcon.ToggleInClassList("folded");
			});

			VisualElement variablesHeader = CreateSubheader("Variables", OnAddVariable);
			mainContainer.Add(variablesHeader);

			_variableContainer = new VisualElement();
			_variableContainer.AddToClassList("propertyContainer");
			mainContainer.Add(_variableContainer);

			VisualElement propertiesHeader = CreateSubheader("Properties", OnAddProperty);
			mainContainer.Add(propertiesHeader);

			_propertyContainer = new VisualElement();
			_propertyContainer.AddToClassList("propertyContainer");
			mainContainer.Add(_propertyContainer);

			Selection.selectionChanged += SelectionChanged;
			SelectionChanged();
		}

		private VisualElement CreateSubheader(string text, Action onAddClicked)
		{
			var header = new VisualElement();
			header.AddToClassList("subheader");
			var label = new TextElement();
			label.AddToClassList("subheaderLabel");
			label.text = text;
			header.Add(label);

			var separator = new VisualElement();
			separator.AddToClassList("headerSeparator");
			header.Add(separator);

			var addButton = new VisualElement();
			addButton.AddToClassList("addButton");
			header.Add(addButton);
			addButton.RegisterCallback<MouseDownEvent>(_ => onAddClicked());

			return header;
		}

		private void SelectionChanged()
		{
			GameObject target = Selection.activeGameObject;
			BussElement element = null;
			if (target != null)
			{
				element = target.GetComponent<BussElement>();
			}

			SetBussElement(element);
		}

		private void OnAddVariable()
		{
			if (_bussElement == null) return;

			NewVariableWindow window = NewVariableWindow.ShowWindow();
			if (window != null)
			{
				window.Init(_bussElement.InlineStyle, (key, property) =>
				{
					if (_bussElement.InlineStyle.TryAddProperty(key, property))
					{
						OnPropertyChange();
					}
				});
			}
		}

		private void OnAddProperty()
		{
			if (_bussElement == null) return;

			var keys = new HashSet<string>();
			foreach (BussPropertyProvider propertyProvider in _bussElement.InlineStyle.Properties)
			{
				keys.Add(propertyProvider.Key);
			}

			IOrderedEnumerable<string> sorted = BussStyle.Keys.OrderBy(k => k);
			var context = new GenericMenu();

			foreach (string key in sorted)
			{
				if (keys.Contains(key)) continue;
				Type baseType = BussStyle.GetBaseType(key);
				SerializableValueImplementationHelper.ImplementationData data = SerializableValueImplementationHelper.Get(baseType);
				IEnumerable<Type> types = data.subTypes.Where(t => t != null && t.IsClass && !t.IsAbstract &&
																   t != typeof(FractionFloatBussProperty));
				foreach (Type type in types)
				{
					var label = new GUIContent(types.Count() > 1 ? key + "/" + type.Name : key);
					context.AddItem(new GUIContent(label), false, () =>
					{
						_bussElement.InlineStyle.Properties.Add(
							BussPropertyProvider.Create(key, (IBussProperty)Activator.CreateInstance(type)));
						OnPropertyChange();
					});
				}
			}

			context.ShowAsContext();
		}

		private void SetBussElement(BussElement element)
		{
			if (element == _bussElement) return;
			_bussElement = element;
			ClearAll();
			if (element != null)
			{
				SpawnProperties();
			}
		}

		private void OnPropertyChange()
		{
			RefreshProperties();
			if (_bussElement != null)
			{
				EditorUtility.SetDirty(_bussElement);
				_bussElement.RecalculateStyle();
			}
		}

		private void RefreshProperties()
		{
			if (_bussElement == null)
			{
				ClearAll();
				return;
			}

			BussStyleDescription inlineStyle = _bussElement.InlineStyle;

			List<BussPropertyProvider> toSpawn = inlineStyle.Properties.ToList();

			foreach (BussStylePropertyVisualElement visualElement in _variables.Concat(_properties).ToArray())
			{
				BussPropertyProvider propertyProvider = inlineStyle.GetPropertyProvider(visualElement.PropertyProvider.Key);
				if (propertyProvider != visualElement.PropertyProvider)
				{
					visualElement.RemoveFromHierarchy();
					visualElement.Destroy();
					_variables.Remove(visualElement);
					_properties.Remove(visualElement);
				}
				else
				{
					visualElement.PropertyChanged -= OnPropertyChange;
					visualElement.Refresh();
					visualElement.PropertyChanged += OnPropertyChange;
					toSpawn.Remove(propertyProvider);
				}
			}

			PropertySourceTracker propertySourceTracker = _propertySourceDatabase.GetTracker(_bussElement);

			foreach (BussPropertyProvider propertyProvider in toSpawn)
			{
				var visualElement = new BussStylePropertyVisualElement();
				visualElement.InlineStyleOwner = _bussElement;
				visualElement.Setup(null, _bussElement.InlineStyle, propertyProvider, _variableDatabase,
									propertySourceTracker);
				if (propertyProvider.IsVariable)
				{
					_variableContainer.Add(visualElement);
					_variables.Add(visualElement);
				}
				else
				{
					_propertyContainer.Add(visualElement);
					_properties.Add(visualElement);
				}

				visualElement.PropertyChanged += OnPropertyChange;
			}
		}

		private void ClearAll()
		{
			foreach (BussStylePropertyVisualElement visualElement in _variables)
			{
				visualElement.RemoveFromHierarchy();
				visualElement.Destroy();
			}

			_variables.Clear();
			foreach (BussStylePropertyVisualElement visualElement in _properties)
			{
				visualElement.RemoveFromHierarchy();
				visualElement.Destroy();
			}

			_properties.Clear();
		}

		private void SpawnProperties()
		{
			PropertySourceTracker propertySourceTracker = _propertySourceDatabase.GetTracker(_bussElement);

			foreach (BussPropertyProvider property in _bussElement.InlineStyle.Properties)
			{
				var visualElement = new BussStylePropertyVisualElement();
				visualElement.InlineStyleOwner = _bussElement;
				visualElement.Setup(null, _bussElement.InlineStyle, property, _variableDatabase, propertySourceTracker);
				if (property.IsVariable)
				{
					_variableContainer.Add(visualElement);
					_variables.Add(visualElement);
				}
				else
				{
					_propertyContainer.Add(visualElement);
					_properties.Add(visualElement);
				}

				visualElement.PropertyChanged += OnPropertyChange;
			}
		}
	}
}
