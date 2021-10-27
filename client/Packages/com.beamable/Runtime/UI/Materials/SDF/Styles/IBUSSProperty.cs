using System;
using UnityEngine;

namespace Beamable.UI.SDF.Styles
{
    public interface IBUSSProperty
    {
        IBUSSProperty Clone();
    }

    public interface IColorProperty : IBUSSProperty
    {
        Color Color { get; }
    }

    public interface IVertexColorProperty : IBUSSProperty
    {
        ColorRect ColorRect { get; }
    }

    public interface IFloatProperty : IBUSSProperty
    {
        float FloatValue { get; }
    }

    public interface IFloatFromFloatProperty : IBUSSProperty
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
}