using System;
using UnityEngine;

namespace Beamable.UI.Buss {
    [Serializable]
    public class FloatProperty : IFloatProperty, IFloatFromFloatProperty {
        [SerializeField]
        private float _floatValue;
        
        public float FloatValue {
            get => _floatValue;
            set => _floatValue = value;
        }

        public FloatProperty() { }

        public FloatProperty(float floatValue) {
            _floatValue = floatValue;
        }

        public float GetFloatValue(float input) => FloatValue;
        public IBussProperty Clone() {
            return new FloatProperty(FloatValue);
        }

        public IBussProperty Interpolate(IBussProperty other, float value) {
            if (other is IFloatProperty fl) {
                return new FloatProperty(Mathf.Lerp(FloatValue, fl.FloatValue, value));
            }

            if (other is FractionFloatProperty frac) {
                return new FractionFloatProperty(Mathf.Lerp(0f, frac.Fraction, value), Mathf.Lerp(FloatValue, frac.Offset, value));
            }

            return Clone();
        }
    }

    [Serializable]
    public class FractionFloatProperty : IFloatFromFloatProperty {
        public float Fraction;
        public float Offset;
        public FractionFloatProperty() { }

        public FractionFloatProperty(float fraction, float offset) {
            Fraction = fraction;
            Offset = offset;
        }

        public float GetFloatValue(float input) {
            return input * Fraction + Offset;
        }

        public IBussProperty Clone() {
            return new FractionFloatProperty(Fraction, Offset);
        }

        public IBussProperty Interpolate(IBussProperty other, float value) {
            if (other is IFloatProperty fp) {
                return new FractionFloatProperty(Mathf.Lerp(Fraction, 0f, value), Mathf.Lerp(Offset, fp.FloatValue, value));
            }

            if (other is FractionFloatProperty frac) {
                return new FractionFloatProperty(Mathf.Lerp(Fraction, frac.Fraction, value), Mathf.Lerp(Offset, frac.Offset, value));
            }

            return Clone();
        }
    }
}