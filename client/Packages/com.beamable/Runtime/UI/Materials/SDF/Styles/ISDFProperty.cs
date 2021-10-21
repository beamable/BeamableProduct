using System;
using Beamable.Common.Content;
using UnityEngine;

namespace Beamable.UI.SDF.Styles {
    public interface ISDFProperty {
        
    }

    public interface IColorProperty : ISDFProperty {
        Color Color { get; }
    }
    
    public interface IVertexColorProperty : ISDFProperty {
        ColorRect ColorRect { get; }
    }

    public interface IFloatProperty : ISDFProperty {
        float FloatValue { get; }
    }

    public interface IFloatFromFloatProperty : ISDFProperty {
        float GetFloatValue(float input);
    }

    public interface IVector2Property : ISDFProperty {
        Vector2 Vector2Value { get; }
    }
}