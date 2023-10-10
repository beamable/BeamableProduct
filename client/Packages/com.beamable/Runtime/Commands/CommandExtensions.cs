using Beamable.Common.Dependencies;

namespace Beamable.Commands
{
	public static class CommandExtensions
	{
		public static IDependencyBuilder AddCommand<T>(this IDependencyBuilder builder,
													string name,
													string description)
		{

			if (!builder.Has<T>())
			{
				builder.AddSingleton<T>();
			}

			return builder;
		}

		public static void GetCommand(this IDependencyProvider provider,
									  string name)
		{

		}
	}
}
