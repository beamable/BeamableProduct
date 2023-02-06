using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.UI.Buss
{
	
	[Serializable]
	public class FloatMaxOperation : Float2Operation
	{
		protected override float Compute(float a, float b)
		{
			return Mathf.Max(a, b);
		}
	}
	
	[Serializable]
	public class FloatMinOperation : Float2Operation
	{
		protected override float Compute(float a, float b)
		{
			return Mathf.Min(a, b);
		}
	}

	[Serializable]
	public class FloatAddOperation : Float2Operation
	{
		protected override float Compute(float a, float b)
		{
			return a + b;
		}
	}


	public abstract class Float2Operation : 
		IFloatBussProperty, 
		IFloatFromFloatBussProperty,
		IComputedProperty<FloatBussProperty>,
		IComputedProperty<IFloatBussProperty>,
		IComputedProperty<IFloatFromFloatBussProperty>
	{
		public float FloatValue => 0;

		public ComputedPropertyArg a = ComputedPropertyArg.Create<FloatBussProperty>(nameof(a));
		public ComputedPropertyArg b = ComputedPropertyArg.Create<FloatBussProperty>(nameof(b));

		private Dictionary<BussStyle, FloatBussProperty> _styleToPropertyCache =
			new Dictionary<BussStyle, FloatBussProperty>();

		private FloatBussProperty AddOrUpdate(BussStyle style, float val)
		{
			if (!_styleToPropertyCache.TryGetValue(style, out var existing))
			{
				_styleToPropertyCache[style] = existing = new FloatBussProperty(val);
			}
			else
			{
				existing.FloatValue = val;
			}
	
			return existing;
		}

		public FloatBussProperty GetComputedFloat(BussStyle style)
		{
			var aVal = 0f;
			var bVal = 0f;
			if (a.TryGetProperty<IFloatBussProperty>(style, out var aProp))
			{
				aVal = aProp.FloatValue;
			}
			if (b.TryGetProperty<IFloatBussProperty>(style, out var bProp))
			{
				bVal = bProp.FloatValue;
			}

			var val = Compute(aVal, bVal);

			return AddOrUpdate(style, val); // TODO: replace with an object pool? I think this could cause excessive GC
		}

		protected abstract float Compute(float a, float b);
		
		IFloatBussProperty IComputedProperty<IFloatBussProperty>.GetComputedValue(BussStyle style)
		{
			return GetComputedFloat(style);
		}

		IFloatFromFloatBussProperty IComputedProperty<IFloatFromFloatBussProperty>.GetComputedValue(BussStyle style)
		{
			return GetComputedFloat(style);
		}
		
		public FloatBussProperty GetComputedValue(BussStyle style)
		{
			return GetComputedFloat(style);
		}

		public BussPropertyValueType ValueType { get; set; } = BussPropertyValueType.Value;

		public IBussProperty CopyProperty()
		{
			return new FloatMaxOperation {a = a, b = b};
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
			return this;
		}

		public float GetFloatValue(float input)
		{
			return FloatValue;
		}

		public IEnumerable<ComputedPropertyArg> Members => new[]
		{
			a, b
		};

		public IBussProperty GetComputedProperty(BussStyle style) => GetComputedFloat(style);
	}
}
