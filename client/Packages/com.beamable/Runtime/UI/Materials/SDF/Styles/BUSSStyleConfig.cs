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
        [SerializeField] private List<BUSSProperty> _properties = new List<BUSSProperty>();

        // TODO: maybe we could create selector by invoking some parent method in OnValidate callback?
        public Selector Selector => SelectorParser.Parse(_name);
        public string Name => _name;
        public List<BUSSProperty> Properties => _properties;

        public BUSSStyle GetStyle()
        {
            BUSSStyle style = new BUSSStyle();

            foreach (BUSSProperty property in Properties)
            {
                style[property.key] = property.property.Get<IBUSSProperty>();
            }

            return style;
        }
    }

    [Serializable]
    public class BUSSProperty
    {
        public string key;

        [SerializableValueImplements(typeof(IBUSSProperty))]
        public SerializableValueObject property;
    }
}