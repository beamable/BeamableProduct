using System;
using Beamable.UI.Sdf.Styles;
using TMPro;
using UnityEngine;

namespace Beamable.UI.Buss
{
    public interface IBussProperty
    {
        IBussProperty Clone();
    }

    public interface IInterpolatedProperty : IBussProperty {
        IBussProperty Interpolate(IBussProperty other, float value);
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

    public interface IVector2Property : IBussProperty
    {
        Vector2 Vector2Value { get; }
    }

    public interface IEnumProperty : IBussProperty
    {
        Enum EnumValue { get; }
        T CastEnumValue<T>() where T : Enum;
    }

    public interface ISpriteProperty : IBussProperty {
        Sprite SpriteValue { get; }
    }

    public interface IFontProperty : IBussProperty {
        TMP_FontAsset FontAsset { get; }
    }
}