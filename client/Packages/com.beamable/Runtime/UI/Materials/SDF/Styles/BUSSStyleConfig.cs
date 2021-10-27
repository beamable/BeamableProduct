using System;
using System.Collections.Generic;
using Beamable.Editor.UI.SDF;
using Beamable.UI.SDF.Styles;
using UnityEngine;

namespace Beamable.UI.SDF
{
    [CreateAssetMenu(fileName = "SDFStyle", menuName = "Beamable/Buss/Create SDF Style", order = 0)]
    public class BUSSStyleConfig : ScriptableObject
    {
        [SerializeField] private List<BUSSStyleDescription> _styles = new List<BUSSStyleDescription>();

        public List<BUSSStyleDescription> Styles => _styles;
        public Action OnChange { get; set; }

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