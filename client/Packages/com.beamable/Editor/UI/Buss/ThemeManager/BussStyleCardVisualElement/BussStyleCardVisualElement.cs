using Beamable.Editor.UI.Buss;
using Beamable.UI.Buss;
using System;
using UnityEditor;
using UnityEditor.EventSystems;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Components
{
	public class BussStyleCardVisualElement : BeamableVisualElement
	{
		public new class UxmlFactory : UxmlFactory<BussStyleCardVisualElement, UxmlTraits> { }

		public BussStyleCardVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/{nameof(BussStyleCardVisualElement)}/{nameof(BussStyleCardVisualElement)}") { }

		private VisualElement _styleIdParent;
		private TextElement _styleIdLabel;
		private TextField _styleIdEditField;
		
		private BussStyleRule _styleRule;
		private VisualElement _properties;

		public override void Refresh()
		{
			base.Refresh();

			_styleIdParent = Root.Q<VisualElement>("styleIdParent");
			_properties = Root.Q<VisualElement>("properties");

			CreateStyleIdLabel();
			CreateProperties();
		}

		private void CreateStyleIdLabel()
		{
			_styleIdLabel = new TextElement();
			_styleIdLabel.name = "styleId";
			_styleIdLabel.text = _styleRule.SelectorString;
			_styleIdParent.Add(_styleIdLabel);
			
			_styleIdLabel.RegisterCallback<MouseDownEvent>(StyleIdClicked);
		}

		private void RemoveStyleIdLabel()
		{
			if (_styleIdLabel == null)
			{
				return;
			}

			_styleIdLabel.UnregisterCallback<MouseDownEvent>(StyleIdClicked);
			_styleIdParent.Remove(_styleIdLabel);
			_styleIdLabel = null;
		}

		private void StyleIdClicked(MouseDownEvent evt)
		{
			RemoveStyleIdLabel();
			CreateStyleIdEditField();
		}

		private void CreateStyleIdEditField()
		{
			_styleIdEditField = new TextField();
			_styleIdEditField.name = "styleId";
			_styleIdEditField.value = _styleRule.SelectorString;
			_styleIdEditField.RegisterValueChangedCallback(StyleIdChanged);
			_styleIdParent.Add(_styleIdEditField);
		}

		private void RemoveStyleIdEditField()
		{
			if (_styleIdEditField == null)
			{
				return;
			}

			_styleIdEditField.UnregisterValueChangedCallback(StyleIdChanged);
			_styleIdParent.Remove(_styleIdEditField);
			_styleIdEditField = null;
		}

		private void StyleIdChanged(ChangeEvent<string> evt)
		{
			// TODO: apply change to property
		}

		public void Setup(BussStyleRule styleRule)
		{
			_styleRule = styleRule;
			Refresh();
		}

		private void CreateProperties()
		{
			foreach (BussPropertyProvider property in _styleRule.Properties)
			{
				BussStylePropertyVisualElement element = new BussStylePropertyVisualElement();
				element.Setup(property);
				_properties.Add(element);
			}
		}
	}
}
