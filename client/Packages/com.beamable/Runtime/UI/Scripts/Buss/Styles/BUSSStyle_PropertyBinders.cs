﻿using System;
using System.Collections.Generic;
using Beamable.UI.Tweening;
using UnityEngine;

namespace Beamable.UI.Buss {
    public partial class BussStyle {
        #region Property Binders
        
        internal static Dictionary<string, IPropertyBinding> _bindings = new Dictionary<string, IPropertyBinding>();
        
        
        protected readonly Dictionary<string, IBussProperty> _properties = new Dictionary<string, IBussProperty>();
        
        // Shape
        public static readonly PropertyBinding<IFloatBussProperty> Threshold = 
            new PropertyBinding<IFloatBussProperty>("threshold", new FloatBussProperty());
        public static readonly PropertyBinding<ISpriteBussProperty> SdfImage =
            new PropertyBinding<ISpriteBussProperty>("sdfImage", new SpriteBussProperty());
        public static readonly PropertyBinding<SdfModeBussProperty> SdfMode =
            new PropertyBinding<SdfModeBussProperty>("sdfMode", new SdfModeBussProperty());
        public static readonly PropertyBinding<ImageTypeBussProperty> ImageType =
            new PropertyBinding<ImageTypeBussProperty>("imageType", new ImageTypeBussProperty());
        
        // Background
        public static readonly PropertyBinding<IVertexColorBussProperty> BackgroundColor = 
            new PropertyBinding<IVertexColorBussProperty>("backgroundColor", new SingleColorBussProperty(Color.white));
        public static readonly PropertyBinding<IFloatFromFloatBussProperty> RoundCorners =
            new PropertyBinding<IFloatFromFloatBussProperty>("roundCorners", new FloatBussProperty());
        public static readonly PropertyBinding<ISpriteBussProperty> BackgroundImage =
            new PropertyBinding<ISpriteBussProperty>("backgroundImage", new SpriteBussProperty());
        public static readonly PropertyBinding<BackgroundModeBussProperty> BackgroundMode =
            new PropertyBinding<BackgroundModeBussProperty>("backgroundMode", new BackgroundModeBussProperty());
        
        // Border
        public static readonly PropertyBinding<BorderModeBussProperty> BorderMode =
            new PropertyBinding<BorderModeBussProperty>("borderMode", new BorderModeBussProperty());
        public static readonly PropertyBinding<IFloatBussProperty> BorderWidth = 
            new PropertyBinding<IFloatBussProperty>("borderWidth", new FloatBussProperty());
        public static readonly PropertyBinding<IVertexColorBussProperty> BorderColor = 
            new PropertyBinding<IVertexColorBussProperty>("borderColor", new SingleColorBussProperty());
        
        // Shadow
        public static readonly PropertyBinding<IVector2BussProperty> ShadowOffset = 
            new PropertyBinding<IVector2BussProperty>("shadowOffset", new Vector2BussProperty());
        public static readonly PropertyBinding<IFloatBussProperty> ShadowThreshold = 
            new PropertyBinding<IFloatBussProperty>("shadowThreshold", new FloatBussProperty());
        public static readonly PropertyBinding<IVertexColorBussProperty> ShadowColor = 
            new PropertyBinding<IVertexColorBussProperty>("shadowColor", new SingleColorBussProperty());
        public static readonly PropertyBinding<IFloatBussProperty> ShadowSoftness = 
            new PropertyBinding<IFloatBussProperty>("shadowSoftness", new FloatBussProperty());
        public static readonly PropertyBinding<ShadowModeBussProperty> ShadowMode =
            new PropertyBinding<ShadowModeBussProperty>("shadowMode", new ShadowModeBussProperty());
        
        // Font
        public static readonly PropertyBinding<IFontBussProperty> Font =
            new PropertyBinding<IFontBussProperty>("font", new FontBussAssetProperty());
        public static readonly PropertyBinding<IFloatBussProperty> FontSize =
            new PropertyBinding<IFloatBussProperty>("fontSize", new FloatBussProperty(18f));
        
        // Transitions
        public static readonly PropertyBinding<IFloatBussProperty> TransitionDuration =
            new PropertyBinding<IFloatBussProperty>("transitionDuration", new FloatBussProperty(2f));
        public static readonly PropertyBinding<EasingBussProperty> TransitionEasing =
            new PropertyBinding<EasingBussProperty>("transitionEasing", new EasingBussProperty(Easing.InOutQuad));

        #endregion
        internal interface IPropertyBinding {
            string Key { get; }
            Type PropertyType { get; }
            IBussProperty GetProperty(BussStyle style);
            void SetProperty(BussStyle style, IBussProperty property);
            IBussProperty GetDefaultValue();
        }

        public sealed class PropertyBinding<T> : IPropertyBinding where T : class, IBussProperty {
            public string Key { get; }

            public T DefaultValue { get; }
            public Type PropertyType => typeof(T);

            private static HashSet<string> _keyControler = new HashSet<string>();

            internal PropertyBinding(string key, T defaultValue) {
                Key = key;
                DefaultValue = defaultValue;
                _bindings[key] = this;
            }

            IBussProperty IPropertyBinding.GetProperty(BussStyle style) => Get(style);

            void IPropertyBinding.SetProperty(BussStyle style, IBussProperty property) {
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
                    if (Get(style.BaseStyle) is IInterpolatedBussProperty interpolatedProperty) {
                        return (T) interpolatedProperty.Interpolate(pseudoProperty, style.BlendValue);
                    }

                    return pseudoProperty;
                }

                return Get(style.BaseStyle);
            }

            private T GetFromVariable(BussStyle style, string variableName) {
                if (_keyControler.Contains(variableName)) {
                    Debug.LogWarning("Cyclical variable reference in BUSS properties.");
                    return DefaultValue;
                }
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
                style._properties[Key] = property.CopyProperty();
            }

            public void Set(BussStyle style, VariableProperty property) {
                style._properties[Key] = property.CopyProperty();
            }
        }
    }
}