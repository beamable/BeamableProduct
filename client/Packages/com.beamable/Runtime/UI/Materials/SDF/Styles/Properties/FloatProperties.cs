using System;
using UnityEngine;

namespace Beamable.UI.SDF.Styles {
    [Serializable]
    public class FloatProperty : IFloatProperty, IFloatFromFloatProperty {
        [SerializeField]
        private float _floatValue;
        
        public float FloatValue {
            get => _floatValue;
            set => _floatValue = value;
        }

        public FloatProperty(float floatValue = default) {
            _floatValue = floatValue;
        }

        public float GetFloatValue(float input) => FloatValue;
    }

    [Serializable]
    public class FractionFloatProperty : IFloatFromFloatProperty {
        public float Fraction;
        
        public FractionFloatProperty(float fraction = default) {
            Fraction = fraction;
        }

        public float GetFloatValue(float input) {
            return input * Fraction;
        }
    }
}