using System;
using System.Collections.Generic;
using Beamable.UI.Tweening;
using UnityEngine;

namespace Beamable.UI.Buss {
    public partial class BussStyle {
        #region Property Binders
        
        internal static Dictionary<string, IPropertyBiding> _bindings = new Dictionary<string, IPropertyBiding>();
        
        
        protected readonly Dictionary<string, IBussProperty> _properties = new Dictionary<string, IBussProperty>();
        
        // Shape
        public static readonly PropertyBiding<IFloatProperty> Threshold = 
            new PropertyBiding<IFloatProperty>("threshold", new FloatProperty());
        public static readonly PropertyBiding<ISpriteProperty> SdfImage =
            new PropertyBiding<ISpriteProperty>("sdfImage", new SpriteProperty());
        public static readonly PropertyBiding<SdfModeProperty> SdfMode =
            new PropertyBiding<SdfModeProperty>("sdfMode", new SdfModeProperty());
        public static readonly PropertyBiding<ImageTypeProperty> ImageType =
            new PropertyBiding<ImageTypeProperty>("imageType", new ImageTypeProperty());
        
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
        public static readonly PropertyBiding<IVertexColorProperty> BorderColor = 
            new PropertyBiding<IVertexColorProperty>("borderColor", new SingleColorProperty());
        
        // Shadow
        public static readonly PropertyBiding<IVector2Property> ShadowOffset = 
            new PropertyBiding<IVector2Property>("shadowOffset", new Vector2Property());
        public static readonly PropertyBiding<IFloatProperty> ShadowThreshold = 
            new PropertyBiding<IFloatProperty>("shadowThreshold", new FloatProperty());
        public static readonly PropertyBiding<IVertexColorProperty> ShadowColor = 
            new PropertyBiding<IVertexColorProperty>("shadowColor", new SingleColorProperty());
        public static readonly PropertyBiding<IFloatProperty> ShadowSoftness = 
            new PropertyBiding<IFloatProperty>("shadowSoftness", new FloatProperty());
        public static readonly PropertyBiding<ShadowModeProperty> ShadowMode =
            new PropertyBiding<ShadowModeProperty>("shadowMode", new ShadowModeProperty());
        
        // Font
        public static readonly PropertyBiding<IFontProperty> Font =
            new PropertyBiding<IFontProperty>("font", new FontAssetProperty());
        public static readonly PropertyBiding<IFloatProperty> FontSize =
            new PropertyBiding<IFloatProperty>("fontSize", new FloatProperty(18f));
        
        // Transitions
        public static readonly PropertyBiding<IFloatProperty> TransitionDuration =
            new PropertyBiding<IFloatProperty>("transitionDuration", new FloatProperty(2f));
        public static readonly PropertyBiding<EasingProperty> TransitionEasing =
            new PropertyBiding<EasingProperty>("transitionEasing", new EasingProperty(Easing.InOutQuad));

        #endregion
        internal interface IPropertyBiding {
            string Key { get; }
            Type PropertyType { get; }
            IBussProperty GetProperty(BussStyle style);
            void SetProperty(BussStyle style, IBussProperty property);
            IBussProperty GetDefaultValue();
        }

        public sealed class PropertyBiding<T> : IPropertyBiding where T : class, IBussProperty {
            public string Key { get; }

            public T DefaultValue { get; }
            public Type PropertyType => typeof(T);

            private static HashSet<string> _keyControler = new HashSet<string>();

            internal PropertyBiding(string key, T defaultValue) {
                Key = key;
                DefaultValue = defaultValue;
                _bindings[key] = this;
            }

            IBussProperty IPropertyBiding.GetProperty(BussStyle style) => Get(style);

            void IPropertyBiding.SetProperty(BussStyle style, IBussProperty property) {
                if (property is T t) {
                    Set(style, t);
                }
                else if (property is VariableProperty v) {
                    Set(style, v);
                }
            }

            public IBussProperty GetDefaultValue() {
                return DefaultValue;
            }

            public T Get(BussStyle style) {
                if (style is BussPseudoStyle pseudoStyle) {
                    return GetFromPseudoStyle(pseudoStyle);
                }

                return GetFromStyle(style);
            }

            private T GetFromStyle(BussStyle style) {
                if (style._properties.TryGetValue(Key, out var property)) {
                    if (property is VariableProperty variable) {
                        return GetFromVariable(style, variable.VariableName);
                    }
                    else {
                        return (T) property;
                    }
                }

                return DefaultValue;
            }

            private T GetFromPseudoStyle(BussPseudoStyle style) {
                if (style._properties.ContainsKey(Key)) {
                    var pseudoProperty = GetFromStyle(style);
                    if (Get(style.BaseStyle) is IInterpolatedProperty interpolatedProperty) {
                        return (T) interpolatedProperty.Interpolate(pseudoProperty, style.BlendValue);
                    }

                    return pseudoProperty;
                }

                return Get(style.BaseStyle);
            }

            private T GetFromVariable(BussStyle style, string variableName) {
                if (_keyControler.Contains(variableName)) return DefaultValue;
                _keyControler.Add(variableName);
                var result = DefaultValue;
                if (style._properties.TryGetValue(variableName, out var property)) {
                    if (property is VariableProperty variableProperty) {
                        result = GetFromVariable(style, variableProperty.VariableName);
                    }
                    else {
                        result = (property as T) ?? DefaultValue;
                    }
                }
                _keyControler.Clear();
                return result;
            }

            public void Set(BussStyle style, T property) {
                style._properties[Key] = property.Clone();
            }

            public void Set(BussStyle style, VariableProperty property) {
                style._properties[Key] = property.Clone();
            }
        }
    }
}