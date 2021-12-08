using System;
using System.Collections.Generic;
using Beamable.Editor.UI.Buss;
using Beamable.UI.Sdf.Styles;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Beamable.UI.Buss
{
	[CreateAssetMenu(fileName = "BUSSStyleConfig", menuName = "Beamable/Buss/Create BUSS Style", order = 0)]
	public class BussStyleSheet : ScriptableObject, ISerializationCallbackReceiver
	{
		public event Action OnChange;

#pragma warning disable CS0649
		[SerializeField] private List<BussStyleRule> _styles = new List<BussStyleRule>();
		[SerializeField, HideInInspector] private List<Object> _assetReferences = new List<Object>();
#pragma warning restore CS0649

		public List<BussStyleRule> Styles => _styles;

		private void OnValidate()
		{
			BussConfiguration.Instance.UpdateStyleSheet(this);
			OnChange?.Invoke();
		}

		public void AssignAssetReferencesFromReferenceList()
		{
			foreach (BussStyleRule style in Styles)
			{
				style.AssignAssetReferencesFromReferenceList(_assetReferences);
			}
		}

		public void PutAssetReferencesInReferenceList()
		{
			_assetReferences.Clear();
			foreach (BussStyleRule style in Styles)
			{
				style.PutAssetReferencesInReferenceList(_assetReferences);
			}
		}

		public void OnBeforeSerialize()
		{
			PutAssetReferencesInReferenceList();
		}

		public void OnAfterDeserialize()
		{
			AssignAssetReferencesFromReferenceList();
		}
	}

	[Serializable]
	public class BussStyleRule : BussStyleDescription
	{
#pragma warning disable CS0649
		[FormerlySerializedAs("_name")] [SerializeField]
		private string _selector;
#pragma warning restore CS0649

		public BussSelector Selector => BussSelectorParser.Parse(_selector);
		public string SelectorString => _selector;
		
	    public static BussStyleRule Create(string selector, List<BussPropertyProvider> properties)
	    {
		    return new BussStyleRule() {_selector = selector, _properties = properties};
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
		[SerializeField]
		private string key;

		[SerializeField, SerializableValueImplements(typeof(IBussProperty))]
		private SerializableValueObject property;
#pragma warning restore CS0649

		public string Key {
			get => key;
		}

	    public static BussPropertyProvider Create(string key, IBussProperty property)
	    {
		    var propertyProvider = new SerializableValueObject();
		    propertyProvider.Set(property);
		    return new BussPropertyProvider() {key = key, property = propertyProvider};
	    }

		public IBussProperty GetProperty()
		{
			return property.Get<IBussProperty>();
		}
	}
}
