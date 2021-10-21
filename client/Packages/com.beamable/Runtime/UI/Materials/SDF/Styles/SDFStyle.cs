using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.UI.SDF.Styles {
    public class SDFStyle {
        public static readonly PropertyBiding<IVertexColorProperty> BackgroundColor = 
            new PropertyBiding<IVertexColorProperty>("backgroundColor", new SingleColorProperty());
        public static readonly PropertyBiding<IFloatFromFloatProperty> BorderWidth = 
            new PropertyBiding<IFloatFromFloatProperty>("borderWidth", new FloatProperty());
        public static readonly PropertyBiding<IVertexColorProperty> BorderColor = 
            new PropertyBiding<IVertexColorProperty>("borderColor", new SingleColorProperty(new Color(0f, 0f, 0f, 0f)));

        private Dictionary<string, ISDFProperty> _properties = new Dictionary<string, ISDFProperty>();
        
        public sealed class PropertyBiding<T> where T : ISDFProperty {
            public string Key { get; }
            public T DefaultValue { get; }
            public Type PropertyType => typeof(T);

            internal PropertyBiding(string key, T defaultValue) {
                Key = key;
                DefaultValue = defaultValue;
            }
            
            public T Get(SDFStyle style) {
                if (style._properties.TryGetValue(Key, out var property)) {
                    return (T) property;
                }

                return DefaultValue;
            }
        }
    }
}