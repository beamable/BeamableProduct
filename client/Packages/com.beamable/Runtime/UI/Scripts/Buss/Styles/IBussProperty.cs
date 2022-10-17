using Beamable.UI.Sdf;
using System;
using TMPro;
using UnityEngine;

namespace Beamable.UI.Buss
{

	public interface IBussPropertyBase
	{
		BussPropertyValueType ValueType { get; set; }
	}

	public interface IBussProperty : IBussPropertyBase
	{

		IBussProperty CopyProperty();

	}

	public enum BussPropertyValueType //https://developer.mozilla.org/en-US/docs/Web/CSS/inherit#see_also
	{
		// use the given value for the property
		Value,

		// inherit the property value from the parent
		Inherited,

		// use the initial value of the property binding type
		Initial
	}

	public abstract class DefaultBussProperty : IBussPropertyBase
	{
		// this needs to be serialized so that all sub properties can have a persistent value type.
		[SerializeField]
		private BussPropertyValueType _valueType;

		public BussPropertyValueType ValueType { get => _valueType; set => _valueType = value; }

	}

	public enum BussPropertyValueType
	{
		Value,
		Initial,
		Inherited
	}

	public abstract class IBussProperty<T> : IBussProperty
	{


		T Value { get; }

		public abstract IBussProperty CopyProperty();
	}

	public interface IInterpolatedBussProperty : IBussProperty
	{
		IBussProperty Interpolate(IBussProperty other, float value);
	}

	public interface IColorBussProperty : IInterpolatedBussProperty
	{
		Color Color { get; }
	}

	public interface IVertexColorBussProperty : IInterpolatedBussProperty
	{
		ColorRect ColorRect { get; }
	}

	public interface IFloatBussProperty : IInterpolatedBussProperty
	{
		float FloatValue { get; }
	}

	public interface IFloatFromFloatBussProperty : IInterpolatedBussProperty
	{
		float GetFloatValue(float input);
	}

	public interface IVector2BussProperty : IBussProperty
	{
		Vector2 Vector2Value { get; }
	}

	public interface IEnumBussProperty : IBussProperty
	{
		Enum EnumValue { get; }
		T CastEnumValue<T>() where T : Enum;
	}

	public interface ISpriteBussProperty : IBussProperty
	{
		Sprite SpriteValue { get; }
	}

	public interface IFontBussProperty : IBussProperty
	{
		TMP_FontAsset FontAsset { get; }
	}
}
