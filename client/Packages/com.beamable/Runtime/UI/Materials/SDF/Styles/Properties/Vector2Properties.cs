using UnityEngine;

namespace Beamable.UI.SDF.Styles {
    public class Vector2Property : IVector2Property {
        [SerializeField]
        private Vector2 _vector2Value;

        public Vector2Property(Vector2 vector2Value = default) {
            _vector2Value = vector2Value;
        }

        public Vector2 Vector2Value {
            get => _vector2Value;
            set => _vector2Value = value;
        }
    }
}