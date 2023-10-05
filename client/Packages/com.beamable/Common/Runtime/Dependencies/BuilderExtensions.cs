using System;
using System.Linq;

namespace Beamable.Common.Dependencies
{
	public static class BuilderExtensions
	{
	
		public static IDependencyBuilder AddSingletonDecorator<TImpl, TInterface, TDecorator>(this IDependencyBuilder builder, Func<TInterface, TDecorator> instanceFactory)
			where TImpl : TInterface
			where TDecorator : TInterface
		{
			if (!builder.Has<TImpl>())
				builder.AddSingleton<TImpl>();

			var singletons = builder.GetSingletonServices();
			var entry = singletons.FirstOrDefault(x => x.Interface == typeof(TInterface));

			builder.AddSingleton<TDecorator>(p =>
			{
				return instanceFactory((TInterface)entry.Factory(p));
			});
			
			builder.RemoveIfExists<TInterface>();
			builder.AddSingleton<TInterface>(p => p.GetService<TDecorator>());
			return builder;
		}

	}
}
