using System;
using System.Collections.Generic;
using Beamable.Editor.UI.SDF;
using Beamable.UI.Buss;
using Beamable.UI.SDF.Styles;
using UnityEngine;

namespace Beamable.UI.BUSS
{
    [CreateAssetMenu(fileName = "BUSSStyleConfig", menuName = "Beamable/Buss/Create BUSS Style", order = 0)]
    public class BUSSStyleConfig : ScriptableObject
    {
        public event Action OnChange;

#pragma warning disable CS0649
        [SerializeField] private List<BUSSStyleDescriptionWithSelector> _styles = new List<BUSSStyleDescriptionWithSelector>();
#pragma warning restore CS0649

        public List<BUSSStyleDescriptionWithSelector> Styles => _styles;
        
        private void OnValidate()
        {
            OnChange?.Invoke();
        }
    }

    [Serializable]
    public class BUSSStyleDescriptionWithSelector : BUSSStyleDescription
    {
        [SerializeField] private string _name;

        // TODO: maybe we could create selector by invoking some parent method in OnValidate callback?
        public Selector Selector => SelectorParser.Parse(_name);
        public string Name => _name;
    }

    [Serializable]
    public class BUSSStyleDescription {
        [SerializeField] private List<BussPropertyProvider> _properties = new List<BussPropertyProvider>();
        public List<BussPropertyProvider> Properties => _properties;
    }

    [Serializable]
    public class BussPropertyProvider
    {
        [SerializeField]
        private string key;
        [SerializeField, SerializableValueImplements(typeof(IBUSSProperty))]
        private SerializableValueObject property;

        public string Key {
            get => key;
        }

        public IBUSSProperty GetProperty() {
            return property.Get<IBUSSProperty>();
        }
    }
}