using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.UI.Buss
{
	public class FloatMaxOperation : 
		IFloatBussProperty, 
		IFloatFromFloatBussProperty,
		IComputedProperty
	{
		public float FloatValue => 1;//Mathf.Max(a.FloatValue, b.FloatValue);

		public IFloatBussProperty a, b;

		public BussPropertyValueType ValueType { get; set; } = BussPropertyValueType.Value;

		public IBussProperty CopyProperty()
		{
			return new FloatMaxOperation {a = a, b = b};
		}

		public event Action OnValueChanged = null;

		public void NotifyValueChange()
		{
			OnValueChanged?.Invoke();
			// throw new NotImplementedException();
		}

		public IBussProperty Interpolate(IBussProperty other, float value)
		{
			return this;
		}

		public float GetFloatValue(float input)
		{
			return 1;
		}

		public IEnumerable<IBussProperty> Members => new[] {a, b};
	}
}
