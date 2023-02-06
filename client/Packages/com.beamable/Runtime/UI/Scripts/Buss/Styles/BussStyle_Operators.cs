using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.UI.Buss
{
	public partial class BussStyle
	{
		private static Dictionary<Type, BussComputeOperatorBinding> typeToOperators = new Dictionary<Type, BussComputeOperatorBinding>();

		public static BussOperatorBinding<FloatBussProperty> FloatOperators
			= new BussOperatorBinding<FloatBussProperty>()
			  .AddOperator<FloatMaxOperation>()
			  .AddOperator<FloatMinOperation>()
			  .AddOperator<FloatAddOperation>();
		
		public static BussOperatorBinding<IFloatBussProperty> IFloatOperators
			= new BussOperatorBinding<IFloatBussProperty>()
				.AddOperator<FloatMaxOperation>()
				.AddOperator<FloatMinOperation>()
				.AddOperator<FloatAddOperation>()
			;
		
		public static BussOperatorBinding<IFloatFromFloatBussProperty> IFloatFromFloatOperators
				= new BussOperatorBinding<IFloatFromFloatBussProperty>()
				  .AddOperator<FloatMaxOperation>()
				  .AddOperator<FloatMinOperation>()
				  .AddOperator<FloatAddOperation>()
			;

		public static BussOperatorBinding<IVertexColorBussProperty> IColorBussOperators
			= new BussOperatorBinding<IVertexColorBussProperty>()
			  .AddOperator<ColorFadeOperation>()
			  .AddOperator<ColorDesaturateOperation>()
			  .AddOperator<ColorSaturateOperation>()
			  .AddOperator<ColorLightenOperation>()
			  .AddOperator<ColorDarkenOperation>()
			  .AddOperator<ColorSpinOperation>()
			  .AddOperator<SingleColorToMultiColorOperation>();

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
			return typeToOperators.TryGetValue(propertyType, out operatorBinding);
		}


		public abstract class BussComputeOperatorBinding
		{
			protected Dictionary<Type, BussOperatorFactory> opTypeToFactory = new Dictionary<Type, BussOperatorFactory>();

			public abstract IBussProperty Create(Type operatorType);
			public List<BussOperatorFactory> Factories => opTypeToFactory.Values.ToList();
			public bool HasAnyFactories => opTypeToFactory.Count > 0;

			public bool TryGetFactoryForOperatorType(Type operatorType, out BussOperatorFactory factory)
			{
				return opTypeToFactory.TryGetValue(operatorType, out factory);
			}
			
			public IBussProperty Create() => Create(opTypeToFactory.Keys.FirstOrDefault());
			public IBussProperty Create<TOp>() where TOp : IComputedProperty => Create(typeof(TOp));
		}

		public class BussOperatorFactory
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
				typeToOperators.Add(typeof(TProp), this);
			}

			public BussOperatorBinding<TProp> AddOperator<TOp>() 
				where TOp : IComputedProperty<TProp>, new()
			{
				opTypeToFactory[typeof(TOp)] = new BussOperatorFactory
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

			public override IBussProperty Create(Type operatorType)
			{
				var factory = opTypeToFactory[operatorType];
				var instance = factory.factory?.Invoke();
				return instance;
			}
		}
	}

}
