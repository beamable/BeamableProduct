using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace cli;

public static class DependencyInjectionExtensions
{
	public static ServiceCollection AddRootCommand<TCommand, TArgs>(this ServiceCollection collection)
		where TArgs : CommandArgs
		where TCommand : AppCommand<TArgs>
	{
		return AddCommand<TCommand, TArgs, RootCommand>(collection);
	}

	public static ServiceCollection AddCommand<TCommand, TArgs, TBaseCommand>(this ServiceCollection collection)
		where TArgs : CommandArgs
		where TCommand : AppCommand<TArgs>
		where TBaseCommand : Command
	{
		collection.AddSingleton<TCommand>();
		collection.AddTransient<TArgs>();
		collection.AddSingleton<ICommandFactory>(provider =>
		{
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
