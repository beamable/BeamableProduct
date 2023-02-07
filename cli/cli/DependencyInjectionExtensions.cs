using Beamable.Common.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

namespace cli;

public static class DependencyInjectionExtensions
{
	public static IDependencyBuilder AddRootCommand<TCommand, TArgs>(this IDependencyBuilder collection)
		where TArgs : CommandArgs
		where TCommand : AppCommand<TArgs>
	{
		return AddCommand<TCommand, TArgs, RootCommand>(collection);
	}

	public static IDependencyBuilder AddCommand<TCommand, TArgs, TBaseCommand>(this IDependencyBuilder collection)
		where TArgs : CommandArgs
		where TCommand : AppCommand<TArgs>
		where TBaseCommand : Command
	{
		collection.AddSingleton<TCommand>();
		if (!collection.Has<TArgs>())
		{
			collection.AddTransient<TArgs>();
		}

		collection.AddSingleton<ICommandFactory<TCommand>>(provider =>
		{
			// TODO: Benchmark this init. Even if its 2ms, thats too slow for when the CLI grows to cover the entire Beamable backend. (2ms * 100 commands = too long)
			var factory = new CommandFactory<TCommand>();
			var root = provider.GetRequiredService<TBaseCommand>();
			var command = provider.GetRequiredService<TCommand>();
			
			command.Configure();
			var binder = new AppCommand<TArgs>.Binder(command, provider);
			command.SetHandler((TArgs args) => command.Handle(args), binder);
			root.AddCommand(command);
			return factory;
		});

		return collection;
	}
}
