using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.UI.SDF.Styles {
    public class SDFStyle {
        
        #region Property Binders
        
        internal static Dictionary<string, IPropertyBiding> _bidings = new Dictionary<string, IPropertyBiding>();
        
        // Background
        public static readonly PropertyBiding<IVertexColorProperty> BackgroundColor = 
            new PropertyBiding<IVertexColorProperty>("backgroundColor", new SingleColorProperty(Color.white));
        public static readonly PropertyBiding<IFloatFromFloatProperty> RoundCorners =
            new PropertyBiding<IFloatFromFloatProperty>("roundCorners", new FloatProperty());
        
        // Border
        public static readonly PropertyBiding<BorderModeProperty> BorderMode =
            new PropertyBiding<BorderModeProperty>("borderMode", new BorderModeProperty());
        public static readonly PropertyBiding<IFloatProperty> BorderWidth = 
            new PropertyBiding<IFloatProperty>("borderWidth", new FloatProperty());
        public static readonly PropertyBiding<IColorProperty> BorderColor = 
            new PropertyBiding<IColorProperty>("borderColor", new SingleColorProperty());
        
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
        
        private readonly Dictionary<string, ISDFProperty> _properties = new Dictionary<string, ISDFProperty>();
        
        public ISDFProperty this[string key] {
            get {
                if (_bidings.TryGetValue(key, out var biding)) {
                    return biding.GetProperty(this);
                }

                return null;
            }
            
            set {
                if (_bidings.TryGetValue(key, out var biding)) {
                    biding.SetProperty(this, value);
                }
            }
        }

        public static IEnumerable<string> Keys => _bidings.Keys;

        public static Type GetBaseType(string key) {
            if (_bidings.TryGetValue(key, out var biding)) {
                return biding.PropertyType;
            }
            return null;
        }

        public static ISDFProperty GetDefaultValue(string key) {
            if (_bidings.TryGetValue(key, out var biding)) {
                return biding.GetDefaultValue();
            }
            return null;
        }

        public void Clear() {
            _properties.Clear();
        }

        internal interface IPropertyBiding {
            string Key { get; }
            Type PropertyType { get; }
            ISDFProperty GetProperty(SDFStyle style);
            void SetProperty(SDFStyle style, ISDFProperty property);
            ISDFProperty GetDefaultValue();
        }
        
        public sealed class PropertyBiding<T> : IPropertyBiding where T : ISDFProperty {
            public string Key { get; }

            public T DefaultValue { get; }
            public Type PropertyType => typeof(T);

            internal PropertyBiding(string key, T defaultValue) {
                Key = key;
                DefaultValue = defaultValue;
                _bidings[key] = this;
            }
            
            ISDFProperty IPropertyBiding.GetProperty(SDFStyle style) => Get(style);

            void IPropertyBiding.SetProperty(SDFStyle style, ISDFProperty property) {
                if (property is T t) {
                    Set(style, t);
                }
            }

            public ISDFProperty GetDefaultValue() {
                return DefaultValue;
            }

            public T Get(SDFStyle style) {
                if (style._properties.TryGetValue(Key, out var property)) {
                    return (T) property;
                }

                return DefaultValue;
            }

            public void Set(SDFStyle style, T property) {
                style._properties[Key] = property.Clone();
            }
        }
    }
}