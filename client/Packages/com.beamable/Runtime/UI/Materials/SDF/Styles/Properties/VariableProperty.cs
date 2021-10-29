using System;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Beamable.UI.SDF.Styles {
    [Serializable]
    public class VariableProperty : IUniversalProperty {
        [SerializeField]
        private string _variableName;
        public string VariableName {
            get => _variableName;
            set => _variableName = value;
        }

        public IBUSSProperty Property => throw new NotImplementedException("TODO: variable system");
        
        public VariableProperty() { }
        
        public VariableProperty(string variableName) {
            this._variableName = variableName;
        }

        public IBUSSProperty Clone() {
            return new VariableProperty(VariableName);
        }

        public float GetFloatValue(float input) => ((IFloatFromFloatProperty) Property).GetFloatValue(input);

        public Color Color => ((IColorProperty) Property).Color;
        public ColorRect ColorRect => ((IVertexColorProperty) Property).ColorRect;
        public float FloatValue => ((IFloatProperty) Property).FloatValue;
        public Vector2 Vector2Value => ((IVector2Property) Property).Vector2Value;
        public Enum EnumValue => ((IEnumProperty) Property).EnumValue;
        public T CastEnumValue<T>() where T : Enum  => ((IEnumProperty) Property).CastEnumValue<T>();
        public Sprite SpriteValue => ((ISpriteProperty) Property).SpriteValue;
        public TMP_FontAsset FontAsset => ((IFontProperty) Property).FontAsset;
    }
}