using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.UI.Buss
{
	public partial class BussStyle
	{
		private static Dictionary<Type, BussComputeOperatorBinding> _typeToOperators = new Dictionary<Type, BussComputeOperatorBinding>();

		public static BussOperatorBinding<FloatBussProperty> FloatOperators
			= new BussOperatorBinding<FloatBussProperty>()
			  .AddOperator<FloatMaxOperation>()
			  .AddOperator<FloatMinOperation>()
			  .AddOperator<FloatAddOperation>();
		
		public static BussOperatorBinding<VertexColorBussProperty> VertexColorBussOperators
			= new BussOperatorBinding<VertexColorBussProperty>()
			  .AddOperator<ColorFadeOperation>()
			  .AddOperator<ColorDesaturateOperation>()
			  .AddOperator<ColorSaturateOperation>()
			  .AddOperator<ColorLightenOperation>()
			  .AddOperator<ColorDarkenOperation>()
			  .AddOperator<ColorSpinOperation>()
			  .AddOperator<SingleColorToMultiColorOperation>();


		public static BussOperatorBinding<SingleColorBussProperty> ColorBussOperators
			= new BussOperatorBinding<SingleColorBussProperty>()
			  .AddOperator<ColorFadeOperation>()
			  .AddOperator<ColorDesaturateOperation>()
			  .AddOperator<ColorSaturateOperation>()
			  .AddOperator<ColorLightenOperation>()
			  .AddOperator<ColorDarkenOperation>()
			  .AddOperator<ColorSpinOperation>();

		public static bool TryGetOperatorBinding(Type propertyType, out BussComputeOperatorBinding operatorBinding)
		{
			return _typeToOperators.TryGetValue(propertyType, out operatorBinding);
		}


		public class BussComputeOperatorBinding
		{
			protected Dictionary<Type, BussOperatorDescriptor> _opTypeToDescriptors = new Dictionary<Type, BussOperatorDescriptor>();
			
			public List<BussOperatorDescriptor> Descriptors => _opTypeToDescriptors.Values.ToList();
			public bool HasAnyFactories => _opTypeToDescriptors.Count > 0;

			public bool TryGetDescriptorForOperatorType(Type operatorType, out BussOperatorDescriptor descriptor)
			{
				return _opTypeToDescriptors.TryGetValue(operatorType, out descriptor);
			}
			
			public IBussProperty Create() => Create(_opTypeToDescriptors.Keys.FirstOrDefault());
			public IBussProperty Create<TOp>() where TOp : IComputedProperty => Create(typeof(TOp));
			public IBussProperty Create(Type operatorType)
			{
				var factory = _opTypeToDescriptors[operatorType];
				var instance = factory.factory?.Invoke();
				return instance;
			}
		}

		public class BussOperatorDescriptor
		{
			public Func<IBussProperty> factory;
			public string name;
			public Type operatorType;
		}
		
		public class BussOperatorBinding<TProp> : BussComputeOperatorBinding 
			where TProp : IBussProperty
		{

			public BussOperatorBinding()
			{
				_typeToOperators.Add(typeof(TProp), this);
			}

			public BussOperatorBinding<TProp> AddOperator<TOp>() 
				where TOp : IComputedProperty<TProp>, new()
			{
				_opTypeToDescriptors[typeof(TOp)] = new BussOperatorDescriptor
				{
					name = typeof(TOp).Name
					                  .Replace("Operation", "")
					                  .Replace("Float", "")
					                  .Replace("Color", "")
				   ,
					factory = () => new TOp(),
					operatorType = typeof(TOp)
				};
				return this;
			}
		}
	}

}
