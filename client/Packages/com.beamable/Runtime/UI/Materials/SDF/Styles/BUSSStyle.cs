using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Beamable.UI.BUSS
{
    public class BUSSStyle {
        
        #region Property Binders
        
        internal static Dictionary<string, IPropertyBiding> _bidings = new Dictionary<string, IPropertyBiding>();
        
        // Shape
        public static readonly PropertyBiding<IFloatProperty> Threshold = 
            new PropertyBiding<IFloatProperty>("threshold", new FloatProperty());
        public static readonly PropertyBiding<ISpriteProperty> SdfImage =
            new PropertyBiding<ISpriteProperty>("sdfImage", new SpriteProperty());
        
        // Background
        public static readonly PropertyBiding<IVertexColorProperty> BackgroundColor = 
            new PropertyBiding<IVertexColorProperty>("backgroundColor", new SingleColorProperty(Color.white));
        public static readonly PropertyBiding<IFloatFromFloatProperty> RoundCorners =
            new PropertyBiding<IFloatFromFloatProperty>("roundCorners", new FloatProperty());
        public static readonly PropertyBiding<ISpriteProperty> BackgroundImage =
            new PropertyBiding<ISpriteProperty>("backgroundImage", new SpriteProperty());
        
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
        public static readonly PropertyBiding<IColorProperty> ShadowColor = 
            new PropertyBiding<IColorProperty>("shadowColor", new SingleColorProperty());
        public static readonly PropertyBiding<IFloatProperty> ShadowSoftness = 
            new PropertyBiding<IFloatProperty>("shadowSoftness", new FloatProperty());
        
        // Font
        public static readonly PropertyBiding<IFontProperty> Font =
            new PropertyBiding<IFontProperty>("font", new FontAssetProperty(TMP_Settings.defaultFontAsset));
        public static readonly PropertyBiding<IFloatProperty> FontSize =
            new PropertyBiding<IFloatProperty>("fontSize", new FloatProperty(18f));

        #endregion
        
        private readonly Dictionary<string, IBUSSProperty> _properties = new Dictionary<string, IBUSSProperty>();
        
        public IBUSSProperty this[string key] {
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

        public static IBUSSProperty GetDefaultValue(string key) {
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
            IBUSSProperty GetProperty(BUSSStyle style);
            void SetProperty(BUSSStyle style, IBUSSProperty property);
            IBUSSProperty GetDefaultValue();
        }
        
        public sealed class PropertyBiding<T> : IPropertyBiding where T : IBUSSProperty {
            public string Key { get; }

            public T DefaultValue { get; }
            public Type PropertyType => typeof(T);

            internal PropertyBiding(string key, T defaultValue) {
                Key = key;
                DefaultValue = defaultValue;
                _bidings[key] = this;
            }
            
            IBUSSProperty IPropertyBiding.GetProperty(BUSSStyle style) => Get(style);

            void IPropertyBiding.SetProperty(BUSSStyle style, IBUSSProperty property) {
                if (property is T t) {
                    Set(style, t);
                }
            }

            public IBUSSProperty GetDefaultValue() {
                return DefaultValue;
            }

            public T Get(BUSSStyle style) {
                if (style._properties.TryGetValue(Key, out var property)) {
                    return (T) property;
                }

                return DefaultValue;
            }

            public void Set(BUSSStyle style, T property) {
                style._properties[Key] = property.Clone();
            }
        }
    }
}