using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.UI.SDF.Styles {
    public class SDFStyle {
        
        #region Property Binders
        
        // Background
        public static readonly PropertyBiding<IVertexColorProperty> BackgroundColor = 
            new PropertyBiding<IVertexColorProperty>("backgroundColor", new SingleColorProperty(Color.white));
        public static readonly PropertyBiding<IFloatFromFloatProperty> RoundCorners =
            new PropertyBiding<IFloatFromFloatProperty>("roundCorners", new FloatProperty());
        
        // Border
        public static readonly PropertyBiding<IFloatFromFloatProperty> BorderWidth = 
            new PropertyBiding<IFloatFromFloatProperty>("borderWidth", new FloatProperty());
        public static readonly PropertyBiding<IVertexColorProperty> BorderColor = 
            new PropertyBiding<IVertexColorProperty>("borderColor", new SingleColorProperty());
        
        // Shadow
        public static readonly PropertyBiding<IVector2Property> ShadowOffset = 
            new PropertyBiding<IVector2Property>("shadowOffset", new Vector2Property());
        public static readonly PropertyBiding<IFloatProperty> ShadowThreshold = 
            new PropertyBiding<IFloatProperty>("shadowThreshold", new FloatProperty());
        public static readonly PropertyBiding<IVertexColorProperty> ShadowColor = 
            new PropertyBiding<IVertexColorProperty>("shadowColor", new SingleColorProperty());
        
        // Font
        // TODO

        #endregion

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