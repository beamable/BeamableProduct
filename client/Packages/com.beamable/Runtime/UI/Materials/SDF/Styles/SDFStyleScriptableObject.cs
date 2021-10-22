using System;
using System.Collections.Generic;
using Beamable.Editor.UI.SDF;
using Beamable.UI.SDF.Styles;
using UnityEngine;

namespace Beamable.UI.SDF
{
    [CreateAssetMenu(fileName = "SDFStyle", menuName = "Beamable/Buss/Create SDF Style", order = 0)]
    public class SDFStyleScriptableObject : ScriptableObject
    {
        public Action OnUpdate;

        [SerializeField] private List<SingleStyleObject> _styles = new List<SingleStyleObject>();
        
        private void OnValidate()
        {
            OnUpdate?.Invoke();
        }

        public List<KeyWithProperty> GetProperties(string id)
        {
            SingleStyleObject styleObject = _styles.Find(style => style.Name == id);
            return styleObject != null ? styleObject.Properties : new List<KeyWithProperty>();
        }
    }

    [Serializable]
    public class SingleStyleObject
    {
        [SerializeField] private string _name;
        [SerializeField] private List<KeyWithProperty> _properties = new List<KeyWithProperty>();

        public string Name => _name;
        public List<KeyWithProperty> Properties => _properties;
        
        public SDFStyle GetStyle()
        {
             SDFStyle style = new SDFStyle();
        
            foreach (KeyWithProperty property in _properties)
            {
                style[property.key] = property.property.Get<ISDFProperty>();
            }
        
            return style;
        }
    }

    [Serializable]
    public class KeyWithProperty
    {
        public string key;

        [SerializableValueImplements(typeof(ISDFProperty))]
        public SerializableValueObject property;
    }
}