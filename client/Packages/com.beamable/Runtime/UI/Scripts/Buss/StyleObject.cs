using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beamable.UI.Buss.Properties;
using UnityEngine;

namespace Beamable.UI.Buss
{
    /// <summary>
    /// StyleObject is a class. When it is new'd, we'll create a blank style object with null properties.
    /// </summary>
    [Serializable]
    public class StyleObject : IVariableScope
    {
        public ColorBussProperty Color = new ColorBussProperty();
        public BackgroundBussProperty Background = new BackgroundBussProperty();
        public FontBussProperty Font = new FontBussProperty();
//        public TextBussProperty Text = new TextBussProperty();
//        public VerticalBussProperty Vertical = new VerticalBussProperty();

        public BorderBussProperty Border = new BorderBussProperty();

        public VariableScope Scope = new VariableScope();

        private List<BUSSProperty> AllProperties => new List<BUSSProperty>
        {
            Color, Background, Font, //Text, Vertical
        };

        public bool AnyDefinition => AllProperties.Any(p => p.HasAnyStyles) || Scope.AnyDefinition;

        public virtual StyleObject Clone()
        {
            var next = new StyleObject();
            next.Scope = Scope.Clone();

            next.Color = Color.Clone();
            next.Background = Background.Clone();
            next.Font = Font.Clone();
//            next.Text = Text.Clone();
//            next.Vertical = Vertical.Clone();
            next.Border = Border.Clone();
            return next;
        }

        /// <summary>
        /// Prefer the values in the other StyleObject over the existing ones
        /// </summary>
        /// <param name="other"></param>
        /// <returns>a brand new StyleObject. No modification should have happened to the invocation target's data</returns>
        public virtual StyleObject Merge(StyleObject other)
        {
            var next = new StyleObject();

            T PreferOther<T>(Func<StyleObject, T> getter) where T : BUSSProperty, IBUSSProperty<T>
            {
                var otherProp = getter(other);
                var selfProp = getter(this);
                return
                    // is the other property enabled, and does is it not null?
                    (otherProp?.Enabled ?? false)

                    // if so, return our prop, but with overrides from the other prop
                    ? selfProp.OverrideWith(otherProp)

                    // else, return our prop, but cloned so that we don't leak
                    : selfProp.Clone();
            }

            next.Color = PreferOther(x => x.Color);
            next.Background = PreferOther(x => x.Background);
            next.Font = PreferOther(x => x.Font);
//            next.Text = PreferOther(x => x.Text);
//            next.Vertical = PreferOther(x => x.Vertical);
            next.Border = PreferOther(x => x.Border);
            next.Scope = Scope.Merge(other.Scope);
            return next;
        }

        public T Resolve<T>(string variable)
        {
            return Scope.Resolve<T>(variable);
        }
    }
}