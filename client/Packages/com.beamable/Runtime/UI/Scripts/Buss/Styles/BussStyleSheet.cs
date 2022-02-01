using Beamable.Editor.UI.Buss;
using Beamable.UI.Sdf.Styles;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Beamable.UI.Buss
{
	[CreateAssetMenu(fileName = "BUSSStyleConfig", menuName = "Beamable/Buss/Create BUSS Style", order = 0)]
	public class BussStyleSheet : ScriptableObject, ISerializationCallbackReceiver
	{
		public event Action Change;

#pragma warning disable CS0649
		[SerializeField] private List<BussStyleRule> _styles = new List<BussStyleRule>();
		[SerializeField, HideInInspector] private List<Object> _assetReferences = new List<Object>();
#pragma warning restore CS0649

		public List<BussStyleRule> Styles => _styles;

		[SerializeField]
		private bool _isReadOnly;

		public bool IsReadOnly => _isReadOnly;

		private void OnValidate()
		{
			TriggerChange();
		}

		public void TriggerChange()
		{
			BussConfiguration.UseConfig(conf => conf.UpdateStyleSheet(this));
			Change?.Invoke();
		}

		public void RemoveStyle(BussStyleRule styleRule)
		{
			if (_styles.Remove(styleRule))
			{
				TriggerChange();
			}
		}

		public void RemoveStyleProperty(IBussProperty property, string selectorString)
		{
			BussStyleRule bussStyleRule = _styles.Find(style => style.SelectorString == selectorString);
			if (bussStyleRule.RemoveProperty(property))
			{
				TriggerChange();
			}
		}

		public void RemoveAllProperties(BussStyleRule styleRule)
		{
			styleRule.Properties.Clear();
			TriggerChange();
		}

		public void OnBeforeSerialize()
		{
			PutAssetReferencesInReferenceList();
		}

		public void OnAfterDeserialize()
		{
			AssignAssetReferencesFromReferenceList();
		}

		private void AssignAssetReferencesFromReferenceList()
		{
			foreach (BussStyleRule style in Styles)
			{
				style.AssignAssetReferencesFromReferenceList(_assetReferences);
			}
		}

		private void PutAssetReferencesInReferenceList()
		{
			_assetReferences.Clear();
			foreach (BussStyleRule style in Styles)
			{
				style.PutAssetReferencesInReferenceList(_assetReferences);
			}
		}
	}

	[Serializable]
	public class BussStyleRule : BussStyleDescription
	{
#pragma warning disable CS0649
		// TODO: can we remove that FormerlySerializedAs attribute before release??
		[FormerlySerializedAs("_name")]
		[SerializeField]
		private string _selector;

		[HideInInspector] [SerializeField] private bool _editMode;
		[HideInInspector] [SerializeField] private bool _showAllMode;
#pragma warning restore CS0649

		public BussSelector Selector => BussSelectorParser.Parse(_selector);

		public string SelectorString
		{
			get => _selector;
			set => _selector = value;
		}

		public bool EditMode
		{
			get => _editMode;
			set => _editMode = value;
		}

		public bool ShowAllMode
		{
			get => _showAllMode;
			set => _showAllMode = value;
		}

		public static BussStyleRule Create(string selector, List<BussPropertyProvider> properties)
		{
			return new BussStyleRule { _selector = selector, _properties = properties };
		}

		public bool RemoveProperty(IBussProperty bussProperty)
		{
			BussPropertyProvider provider = _properties.Find(property => property.GetProperty() == bussProperty);
			return _properties.Remove(provider);
		}
	}

	[Serializable]
	public class BussStyleDescription
	{
#pragma warning disable CS0649
		[SerializeField] protected List<BussPropertyProvider> _properties = new List<BussPropertyProvider>();
#pragma warning restore CS0649
		public List<BussPropertyProvider> Properties => _properties;
	}

	[Serializable]
	public class BussPropertyProvider
	{
#pragma warning disable CS0649
		[SerializeField] private string key;

		[SerializeField, SerializableValueImplements(typeof(IBussProperty))]
		private SerializableValueObject property;
#pragma warning restore CS0649

		public string Key => key;

		public bool IsVariable => BussStyleSheetUtility.IsValidVariableName(Key);
		public bool HasVariableReference => GetProperty() is VariableProperty;

		public static BussPropertyProvider Create(string key, IBussProperty property)
		{
			var propertyProvider = new SerializableValueObject();
			propertyProvider.Set(property);
			return new BussPropertyProvider() { key = key, property = propertyProvider };
		}

		public IBussProperty GetProperty()
		{
			return property.Get<IBussProperty>();
		}

		public void SetProperty(IBussProperty bussProperty)
		{
			property.Set(bussProperty);
		}
	}
}
