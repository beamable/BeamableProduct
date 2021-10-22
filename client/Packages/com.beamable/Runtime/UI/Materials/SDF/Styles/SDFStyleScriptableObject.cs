using System;
using Beamable.Editor.UI.SDF;
using Beamable.UI.SDF.Styles;
using UnityEngine;

namespace Beamable.UI.SDF
{
    [CreateAssetMenu(fileName = "SDFStyle", menuName = "Beamable/Buss/Create SDF Style", order = 0)]
    public class SDFStyleScriptableObject : ScriptableObject
    {
        public Action OnUpdate;
        
        public KeyWithProperty[] styleSheet = Array.Empty<KeyWithProperty>();
        private SDFStyle _style;

        private void OnValidate()
        {
            OnUpdate?.Invoke();
        }

        public SDFStyle GetStyle()
        {
            if (_style == null)
            {
                _style = new SDFStyle();
            }

            _style.Clear();
            foreach (var keyWithProperty in styleSheet)
            {
                _style[keyWithProperty.key] = keyWithProperty.property.Get<ISDFProperty>();
            }

            return _style;
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