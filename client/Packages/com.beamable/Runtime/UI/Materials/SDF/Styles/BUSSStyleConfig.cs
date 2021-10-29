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
        [SerializeField] private List<BUSSStyleDescription> _styles = new List<BUSSStyleDescription>();
#pragma warning restore CS0649

        public List<BUSSStyleDescription> Styles => _styles;
        
        private void OnValidate()
        {
            OnChange?.Invoke();
        }
    }

    [Serializable]
    public class BUSSStyleDescription
    {
        [SerializeField] private string _name;
        [SerializeField] private List<BussPropertyProvider> _properties = new List<BussPropertyProvider>();

        // TODO: maybe we could create selector by invoking some parent method in OnValidate callback?
        public Selector Selector => SelectorParser.Parse(_name);
        public string Name => _name;
        public List<BussPropertyProvider> Properties => _properties;

        public BUSSStyle GetStyle()
        {
            BUSSStyle style = new BUSSStyle();

            foreach (BussPropertyProvider property in Properties)
            {
                style[property.Key] = property.GetProperty();
            }

            return style;
        }
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