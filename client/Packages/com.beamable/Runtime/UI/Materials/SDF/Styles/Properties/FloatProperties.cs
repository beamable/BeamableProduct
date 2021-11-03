using System;
using UnityEngine;

namespace Beamable.UI.BUSS {
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
        public IBUSSProperty Clone() {
            return new FloatProperty(FloatValue);
        }
    }

    [Serializable]
    public class FractionFloatProperty : IFloatFromFloatProperty {
        public float Fraction;
        public FractionFloatProperty() { }

        public FractionFloatProperty(float fraction) {
            Fraction = fraction;
        }

        public float GetFloatValue(float input) {
            return input * Fraction;
        }

        public IBUSSProperty Clone() {
            return new FractionFloatProperty(Fraction);
        }
    }
}