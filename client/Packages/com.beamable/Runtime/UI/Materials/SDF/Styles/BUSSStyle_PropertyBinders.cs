using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Beamable.UI.BUSS {
    public partial class BUSSStyle {
        #region Property Binders
        
        // Shape
        public static readonly PropertyBiding<IFloatProperty> Threshold = 
            new PropertyBiding<IFloatProperty>("threshold", new FloatProperty());
        public static readonly PropertyBiding<ISpriteProperty> SdfImage =
            new PropertyBiding<ISpriteProperty>("sdfImage", new SpriteProperty());
        public static readonly PropertyBiding<SdfModeProperty> SdfMode =
            new PropertyBiding<SdfModeProperty>("sdfMode", new SdfModeProperty());
        
        // Background
        public static readonly PropertyBiding<IVertexColorProperty> BackgroundColor = 
            new PropertyBiding<IVertexColorProperty>("backgroundColor", new SingleColorProperty(Color.white));
        public static readonly PropertyBiding<IFloatFromFloatProperty> RoundCorners =
            new PropertyBiding<IFloatFromFloatProperty>("roundCorners", new FloatProperty());
        public static readonly PropertyBiding<ISpriteProperty> BackgroundImage =
            new PropertyBiding<ISpriteProperty>("backgroundImage", new SpriteProperty());
        public static readonly PropertyBiding<BackgroundModeProperty> BackgroundMode =
            new PropertyBiding<BackgroundModeProperty>("backgroundMode", new BackgroundModeProperty());
        
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
        public static readonly PropertyBiding<ShadowModeProperty> ShadowMode =
            new PropertyBiding<ShadowModeProperty>("shadowMode", new ShadowModeProperty());
        
        // Font
        public static readonly PropertyBiding<IFontProperty> Font =
            new PropertyBiding<IFontProperty>("font", new FontAssetProperty(TMP_Settings.defaultFontAsset));
        public static readonly PropertyBiding<IFloatProperty> FontSize =
            new PropertyBiding<IFloatProperty>("fontSize", new FloatProperty(18f));

        #endregion
        internal interface IPropertyBiding {
            string Key { get; }
            Type PropertyType { get; }
            IBUSSProperty GetProperty(BUSSStyle style);
            void SetProperty(BUSSStyle style, IBUSSProperty property);
            IBUSSProperty GetDefaultValue();
        }

        public sealed class PropertyBiding<T> : IPropertyBiding where T : class, IBUSSProperty {
            public string Key { get; }

            public T DefaultValue { get; }
            public Type PropertyType => typeof(T);

            private static HashSet<string> _keyControler = new HashSet<string>();

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
                else if (property is VariableProperty v) {
                    Set(style, v);
                }
            }

            public IBUSSProperty GetDefaultValue() {
                return DefaultValue;
            }

            public T Get(BUSSStyle style) {
                if (style._properties.TryGetValue(Key, out var property)) {
                    if (property is VariableProperty variable) {
                        GetFromVariable(style, variable.VariableName);
                    }
                    else {
                        return (T) property;
                    }
                }

                return DefaultValue;
            }

            private T GetFromVariable(BUSSStyle style, string variableName) {
                if (_keyControler.Contains(Key)) return DefaultValue;
                _keyControler.Add(Key);
                var result = DefaultValue;
                if (_bidings.TryGetValue(variableName, out var variableBinder)) {
                    result = (variableBinder.GetProperty(style) as T) ?? DefaultValue;
                }
                _keyControler.Clear();
                return result;
            }

            public void Set(BUSSStyle style, T property) {
                style._properties[Key] = property.Clone();
            }

            public void Set(BUSSStyle style, VariableProperty property) {
                style._properties[Key] = property.Clone();
            }
        }
    }
}