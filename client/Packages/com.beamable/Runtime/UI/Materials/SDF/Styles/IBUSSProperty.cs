using System;
using Beamable.UI.SDF.Styles;
using TMPro;
using UnityEngine;

namespace Beamable.UI.BUSS
{
    public interface IBUSSProperty
    {
        IBUSSProperty Clone();
    }

    public interface IInterpolatedProperty : IBUSSProperty {
        IBUSSProperty Interpolate(IBUSSProperty other, float value);
    }

    public interface IColorProperty : IInterpolatedProperty
    {
        Color Color { get; }
    }

    public interface IVertexColorProperty : IInterpolatedProperty
    {
        ColorRect ColorRect { get; }
    }

    public interface IFloatProperty : IInterpolatedProperty
    {
        float FloatValue { get; }
    }

    public interface IFloatFromFloatProperty : IInterpolatedProperty
    {
        float GetFloatValue(float input);
    }

    public interface IVector2Property : IBUSSProperty
    {
        Vector2 Vector2Value { get; }
    }

    public interface IEnumProperty : IBUSSProperty
    {
        Enum EnumValue { get; }
        T CastEnumValue<T>() where T : Enum;
    }

    public interface ISpriteProperty : IBUSSProperty {
        Sprite SpriteValue { get; }
    }

    public interface IFontProperty : IBUSSProperty {
        TMP_FontAsset FontAsset { get; }
    }
}