// using System;
// using UnityEngine;
//
// namespace Beamable.UI.Buss
// {
// 	public partial class BussStyle
// 	{
// 		public interface IBussOperation
// 		{
// 			
// 		}
// 		
// 		
// 		public class FloatMaxOperation : IFloatBussProperty, IBussOperation
// 		{
// 			public float FloatValue => Mathf.Max(a.FloatValue, b.FloatValue);
//
// 			public IFloatBussProperty a, b;
//
// 			public BussPropertyValueType ValueType { get; set; } = BussPropertyValueType.Value;
// 			
// 			public IBussProperty CopyProperty()
// 			{
// 				return new FloatMaxOperation {a = a, b = b};
// 			}
//
// 			public event Action OnValueChanged;
// 			
// 			public void NotifyValueChange()
// 			{
// 				// throw new NotImplementedException();
// 			}
//
// 			public IBussProperty Interpolate(IBussProperty other, float value)
// 			{
// 				return this;
// 			}
//
// 		}
// 	}
// }
