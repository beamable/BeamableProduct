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
	public class InlineStyleVisualElement : BeamableBasicVisualElement
	{
		private VisualElement _variableContainer;
		private VisualElement _propertyContainer;

		private readonly ThemeManagerModel _model;

		public InlineStyleVisualElement(ThemeManagerModel model) : base(
			$"{BUSS_THEME_MANAGER_PATH}/{nameof(InlineStyleVisualElement)}/{nameof(InlineStyleVisualElement)}.uss", false)
		{
			_model = model;
		}

		public override void Init()
		{
			base.Init();

			VisualElement header = new VisualElement();
			header.AddToClassList("header");
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
			});

			VisualElement variablesHeader = CreateSubheader("Variables", _model.AddInlineVariable);
			mainContainer.Add(variablesHeader);

			_variableContainer = new VisualElement();
			_variableContainer.AddToClassList("propertyContainer");
			mainContainer.Add(_variableContainer);

			VisualElement propertiesHeader = CreateSubheader("Properties", Test);
			mainContainer.Add(propertiesHeader);

			_propertyContainer = new VisualElement();
			_propertyContainer.AddToClassList("propertyContainer");
			mainContainer.Add(_propertyContainer);

			_model.Change += Refresh;
		}

		protected override void OnDestroy()
		{
			_model.Change -= Refresh;
			ClearAll();
		}

		public override void Refresh()
		{
			ClearAll();

			if (_model.SelectedElement == null)
			{
				return;
			}

			SpawnProperties();
		}

		private void Test()
		{
			var keys = new HashSet<string>();
			foreach (BussPropertyProvider propertyProvider in _model.SelectedElement.InlineStyle.Properties)
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
				                                                   t != typeof(FractionFloatBussProperty)).ToList();
				foreach (Type type in types)
				{
					var label = new GUIContent(types.Count() > 1 ? key + "/" + type.Name : key);
					context.AddItem(new GUIContent(label), false, () =>
					{
						_model.SelectedElement.InlineStyle.Properties.Add(
							BussPropertyProvider.Create(key, (IBussProperty)Activator.CreateInstance(type)));
						_model.ForceRefresh();
					});
				}
			}

			context.ShowAsContext();
		}

		private void SpawnProperties()
		{
			var selectedElement = _model.SelectedElement;

			PropertySourceTracker propertySourceTracker = _model.PropertyDatabase.GetTracker(selectedElement);

			foreach (BussPropertyProvider property in selectedElement.InlineStyle.Properties)
			{
				StylePropertyModel model = new StylePropertyModel(selectedElement.StyleSheet, null,
				                                                  property, _model.VariableDatabase,
				                                                  propertySourceTracker, selectedElement, _model.RemoveInlineProperty);

				var element = new StylePropertyVisualElement(model);
				element.Init();
				(model.IsVariable ? _variableContainer : _propertyContainer).Add(element);
			}
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

		private void ClearAll()
		{
			_propertyContainer.Clear();
			_variableContainer.Clear();
		}
	}
}
