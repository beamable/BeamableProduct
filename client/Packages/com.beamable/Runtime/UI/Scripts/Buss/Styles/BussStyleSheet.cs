using Beamable.Editor.UI.Buss;
using Beamable.UI.Sdf;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using static Beamable.Common.Constants.MenuItems.Assets;
using Object = UnityEngine.Object;

namespace Beamable.UI.Buss
{
	[CreateAssetMenu(fileName = "BUSSStyleConfig", menuName = "Beamable/BUSS Style",
					 order = Orders.MENU_ITEM_PATH_ASSETS_BEAMABLE_ORDER_2)]
	public class BussStyleSheet : ScriptableObject, ISerializationCallbackReceiver
	{
		public event Action Change;

#pragma warning disable CS0649
		[SerializeField] private List<BussStyleRule> _styles = new List<BussStyleRule>();
		[SerializeField, HideInInspector] private List<Object> _assetReferences = new List<Object>();
#pragma warning restore CS0649

		public List<BussStyleRule> Styles => _styles;

#pragma warning disable CS0649
		[SerializeField] private bool _isReadOnly;
#pragma warning restore CS0649

		public bool IsReadOnly => _isReadOnly;

		public bool IsWritable
		{
			get
			{
#if BEAMABLE_DEVELOPER
				return true;
#else
				return !IsReadOnly;
#endif
			}
		}

		private void OnValidate()
		{
			TriggerChange();
		}

		public void TriggerChange()
		{
			if (!IsWritable) return;

			BussConfiguration.UseConfig(conf => conf.UpdateStyleSheet(this));
			Change?.Invoke();
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}

		public void RemoveStyle(BussStyleRule styleRule)
		{
			if (_styles.Remove(styleRule))
			{
				TriggerChange();
			}
		}

		public void RemoveStyleProperty(IBussProperty property, BussStyleRule styleRule)
		{
			BussStyleRule bussStyleRule = _styles.Find(style => style == styleRule);
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

#if BEAMABLE_DEVELOPER
		public void SetReadonly(bool value)
		{
			_isReadOnly = value;
		}
#endif
	}

	[Serializable]
	public class BussStyleRule : BussStyleDescription
	{
#pragma warning disable CS0649
		// TODO: can we remove that FormerlySerializedAs attribute before release??
		[FormerlySerializedAs("_name")]
		[SerializeField]
		private string _selector;
#pragma warning restore CS0649

		public BussSelector Selector => BussSelectorParser.Parse(_selector);

		public string SelectorString
		{
			get => _selector;
			set => _selector = value;
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
			return new BussPropertyProvider { key = key, property = propertyProvider };
		}

		public IBussProperty GetProperty()
		{
			return property.Get<IBussProperty>();
		}

		public void SetProperty(IBussProperty bussProperty)
		{
			property.Set(bussProperty);
		}

		public bool IsPropertyOfType(Type type)
		{
			return type.IsInstanceOfType(GetProperty());
		}
	}
}
