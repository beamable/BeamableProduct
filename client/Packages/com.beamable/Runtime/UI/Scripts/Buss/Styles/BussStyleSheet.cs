using System;
using System.Collections.Generic;
using Beamable.Editor.UI.Buss;
using Beamable.UI.Buss;
using Beamable.UI.Sdf.Styles;
using UnityEngine;
using UnityEngine.Serialization;

namespace Beamable.UI.Buss
{
	[CreateAssetMenu(fileName = "BUSSStyleConfig", menuName = "Beamable/Buss/Create BUSS Style", order = 0)]
	public class BussStyleSheet : ScriptableObject
	{
		public event Action OnChange;

#pragma warning disable CS0649
		[SerializeField] private List<BussStyleRule> _styles = new List<BussStyleRule>();
#pragma warning restore CS0649

		public List<BussStyleRule> Styles => _styles;

		private void OnValidate()
		{
			BussConfiguration.Instance.UpdateStyleSheet(this);
			OnChange?.Invoke();
		}
	}

	[Serializable]
	public class BussStyleRule : BussStyleDescription
	{
#pragma warning disable CS0649
		[FormerlySerializedAs("_name")] [SerializeField]
		private string _selector;
#pragma warning restore CS0649

		// TODO: maybe we could create selector by invoking some parent method in OnValidate callback?
		public BussSelector Selector => _parsedSelector ?? (_parsedSelector = BussSelectorParser.Parse(_selector));
		private BussSelector _parsedSelector;
		public string SelectorString => _selector;
	}

	[Serializable]
	public class BussStyleDescription
	{
#pragma warning disable CS0649
		[SerializeField] private List<BussPropertyProvider> _properties = new List<BussPropertyProvider>();
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

		public IBussProperty GetProperty()
		{
			return property.Get<IBussProperty>();
		}
	}
}
