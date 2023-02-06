using Beamable.UI.Sdf;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.UI.Buss
{

	[Serializable]
	public class ColorFadeOperation : SingleColorAndFloatOperation
	{
		protected override Color Compute(Color a, float b)
		{
			a.a *= (1-b);
			return a;
		}
	}
	
	[Serializable]
	public class ColorDesaturateOperation : SingleColorAndFloatOperation
	{
		protected override Color Compute(Color a, float b)
		{
			Color.RGBToHSV(a, out var h, out var s, out var v);
			s *= (1 - b);
			return Color.HSVToRGB(h, s, v);
		}
	}
	
	[Serializable]
	public class ColorSaturateOperation : SingleColorAndFloatOperation
	{
		protected override Color Compute(Color a, float b)
		{
			Color.RGBToHSV(a, out var h, out var s, out var v);
			s *= (1 + b);
			return Color.HSVToRGB(h, s, v);
		}
	}
	
	[Serializable]
	public class ColorDarkenOperation : SingleColorAndFloatOperation
	{
		protected override Color Compute(Color a, float b)
		{
			Color.RGBToHSV(a, out var h, out var s, out var v);
			v *= (1 - b);
			return Color.HSVToRGB(h, s, v);
		}
	}
	
	[Serializable]
	public class ColorLightenOperation : SingleColorAndFloatOperation
	{
		protected override Color Compute(Color a, float b)
		{
			Color.RGBToHSV(a, out var h, out var s, out var v);
			v *= (1 + b);
			return Color.HSVToRGB(h, s, v);
		}
	}
	
	[Serializable]
	public class ColorSpinOperation : SingleColorAndFloatOperation
	{
		protected override Color Compute(Color a, float b)
		{
			Color.RGBToHSV(a, out var h, out var s, out var v);
			h += b;
			return Color.HSVToRGB(h, s, v);
		}
	}

	[Serializable]
	public class SingleColorToMultiColorOperation : 
		IVertexColorBussProperty,
		IComputedProperty<IVertexColorBussProperty>,
		IComputedProperty<VertexColorBussProperty>
	{
		
		public ComputedPropertyArg topLeft = ComputedPropertyArg.Create<SingleColorBussProperty>(nameof(topLeft));
		public ComputedPropertyArg topRight = ComputedPropertyArg.Create<SingleColorBussProperty>(nameof(topRight));
		public ComputedPropertyArg lowLeft = ComputedPropertyArg.Create<SingleColorBussProperty>(nameof(lowLeft));
		public ComputedPropertyArg lowRight = ComputedPropertyArg.Create<SingleColorBussProperty>(nameof(lowRight));

		
		public IVertexColorBussProperty GetComputedValue(BussStyle style)
		{
			return ComputeVertexColors(style);
		}

		VertexColorBussProperty IComputedProperty<VertexColorBussProperty>.GetComputedValue(BussStyle style)
		{
			return ComputeVertexColors(style);
		}
		
		VertexColorBussProperty ComputeVertexColors(BussStyle style)
		{
			var topLeftColor = Color.white;
			var topRightColor = Color.white;
			var lowLeftColor = Color.white;
			var lowRightColor = Color.white;
			if (topLeft.TryGetProperty<IColorBussProperty>(style, out var topLeftProp))
			{
				topLeftColor = topLeftProp.Color;
			}
			if (topRight.TryGetProperty<IColorBussProperty>(style, out var topRightProp))
			{
				topRightColor = topRightProp.Color;
			}
			if (lowLeft.TryGetProperty<IColorBussProperty>(style, out var lowLeftProp))
			{
				lowLeftColor = lowLeftProp.Color;
			}
			if (lowRight.TryGetProperty<IColorBussProperty>(style, out var lowRightProp))
			{
				lowRightColor = lowRightProp.Color;
			}
			var vertexProp = new VertexColorBussProperty(lowLeftColor, lowRightColor, topLeftColor, topRightColor);
			return vertexProp;
		}

		public BussPropertyValueType ValueType { get; set; } = BussPropertyValueType.Value;

		public IBussProperty CopyProperty()
		{
			throw new InvalidOperationException("Copy not supported for color operations");
		}

		public event Action OnValueChanged = null;

		public void NotifyValueChange()
		{
			var delegates = OnValueChanged;
			OnValueChanged = () => { };
			delegates?.Invoke();
		}

		public IBussProperty Interpolate(IBussProperty other, float value)
		{
			throw new InvalidOperationException("Interp not supported for color operations");
		}


		public Color Color { get; }

		public IEnumerable<ComputedPropertyArg> Members =>
			new ComputedPropertyArg[] {topLeft, topRight, lowLeft, lowRight};

		public ColorRect ColorRect { get; }
	}
	
	public abstract class SingleColorAndFloatOperation : 
		IColorBussProperty, 
		IVertexColorBussProperty,
		IComputedProperty<IColorBussProperty>,
		IComputedProperty<IVertexColorBussProperty>,
		IComputedProperty<SingleColorBussProperty>,
		IComputedProperty<VertexColorBussProperty>
	{
		public float FloatValue => 0;

		public ComputedPropertyArg a = ComputedPropertyArg.Create<SingleColorBussProperty>(nameof(a));
		public ComputedPropertyArg b = ComputedPropertyArg.Create<FloatBussProperty>(nameof(b));
		
		protected abstract Color Compute(Color a, float b);

		/// <summary>
		/// The color control float value is allowed to be go from domain-start, to domain-end.
		/// </summary>
		protected virtual float DomainStart => 0;
		protected virtual float DomainEnd => 100;
		
		/// <summary>
		/// And the output color control float value is mapped linearlly between range-start and range-end
		/// </summary>
		protected virtual float RangeStart => 0;
		protected virtual float RangeEnd => 1;

		protected float MapDomainToRange(float x)
		{
			var ratio = (x - DomainStart) / (DomainEnd - DomainStart);
			var mapped = DomainStart + ratio * (RangeEnd - RangeStart);
			return mapped;
		}

		SingleColorBussProperty ComputeSingle(BussStyle style)
		{
			var aVal = Color.white;
			var bVal = 0f;
			if (a.TryGetProperty<IColorBussProperty>(style, out var aProp))
			{
				aVal = aProp.Color;
			}
			if (b.TryGetProperty<IFloatBussProperty>(style, out var bProp))
			{
				bVal = bProp.FloatValue;
			}

			var val = Compute(aVal, MapDomainToRange(bVal));
			
			return new SingleColorBussProperty(val); 
		}
		
		
		IColorBussProperty IComputedProperty<IColorBussProperty>.GetComputedValue(BussStyle style)
		{
			return ComputeSingle(style);
		}

		public IVertexColorBussProperty GetComputedValue(BussStyle style)
		{
			var aVal = Color.white;
			var bVal = 0f;
			if (a.TryGetProperty<IColorBussProperty>(style, out var aProp))
			{
				aVal = aProp.Color;
			}
			if (b.TryGetProperty<IFloatBussProperty>(style, out var bProp))
			{
				bVal = bProp.FloatValue;
			}

			var val = Compute(aVal, MapDomainToRange(bVal));
			
			return new VertexColorBussProperty(val); 
		}

		SingleColorBussProperty IComputedProperty<SingleColorBussProperty>.GetComputedValue(BussStyle style)
		{
			return ComputeSingle(style);
		}

		VertexColorBussProperty IComputedProperty<VertexColorBussProperty>.GetComputedValue(BussStyle style)
		{
			return GetComputedValue(style) as VertexColorBussProperty;
		}

		public BussPropertyValueType ValueType { get; set; } = BussPropertyValueType.Value;

		public IBussProperty CopyProperty()
		{
			throw new InvalidOperationException("Copy not supported for color operations");
		}

		public event Action OnValueChanged = null;

		public void NotifyValueChange()
		{
			var delegates = OnValueChanged;
			OnValueChanged = () => { };
			delegates?.Invoke();
		}

		public IBussProperty Interpolate(IBussProperty other, float value)
		{
			throw new InvalidOperationException("Interp not supported for color operations");
		}

		public IEnumerable<ComputedPropertyArg> Members => new[]
		{
			a, b
		};

		public Color Color { get; }
		public ColorRect ColorRect { get; }
	}
}
