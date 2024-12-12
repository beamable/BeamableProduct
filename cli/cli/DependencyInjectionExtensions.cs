using Beamable.Common;
using Beamable.Common.BeamCli;
using Beamable.Common.Dependencies;
using Beamable.Serialization.SmallerJSON;
using cli.Services;
using cli.Utils;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.Text;

namespace cli;

public static class DependencyInjectionExtensions
{
	public static IDependencyBuilder AddRootCommand<TCommand, TArgs>(this IDependencyBuilder collection)
		where TArgs : CommandArgs
		where TCommand : AppCommand<TArgs>
	{
		return AddSubCommandWithHandler<TCommand, TArgs, RootCommand>(collection);
	}

	public static IDependencyBuilder AddRootCommand<TCommand>(this IDependencyBuilder collection)
		where TCommand : CommandGroup
	{
		return AddSubCommandWithHandler<TCommand, CommandGroupArgs, RootCommand>(collection);
	}

	public static IDependencyBuilder AddSubCommand<TCommand, TArgs, TBaseCommand>(
		this IDependencyBuilder collection)
		where TArgs : CommandArgs
		where TCommand : AppCommand<TArgs>
		where TBaseCommand : CommandGroup
	{
		return collection.AddSubCommandWithHandler<TCommand, TArgs, TBaseCommand>();
	}

	public static IDependencyBuilder AddSubCommandWithHandler<TCommand, TArgs, TBaseCommand>(this IDependencyBuilder collection)
		where TArgs : CommandArgs
		where TCommand : AppCommand<TArgs>
		where TBaseCommand : Command
	{
		collection.AddSingleton<TCommand>();
		if (!collection.Has<TArgs>())
		{
			collection.AddTransient<TArgs>();
		}

		collection.AddSingleton<ICommandFactory<TCommand>>(commandProvider =>
		{
			// TODO: Benchmark this init. Even if its 2ms, thats too slow for when the CLI grows to cover the entire Beamable backend. (2ms * 100 commands = too long)
			var factory = new CommandFactory<TCommand>();
			var root = commandProvider.GetRequiredService<TBaseCommand>();
			var command = commandProvider.GetRequiredService<TCommand>();

			command.CommandProvider = commandProvider;
			command.Configure();
			var binder = new AppCommand<TArgs>.Binder(command, commandProvider);
			command.SetHandler(async (TArgs args) =>
			{
				using( CliSerilogProvider.LogContext.Value.BeginScope(new Dictionary<string,object>
				      {
					      {"Token", args.AppContext.Token},
					      {"Command", command}
				      })){
					BeamableLogger.LogVerbose("Starting command");
					BeamableLogger.LogVerbose($"app context= {JsonConvert.SerializeObject(args.AppContext, Formatting.Indented, new JsonSerializerSettings())}");
					BeamableLogger.LogVerbose("Running command");
					if (command is IResultProvider resultProvider)
					{
						resultProvider.Reporter = args.Provider.GetService<IDataReporterService>();
					}
				}


				if (!args.IgnoreStandaloneValidation && command is not IStandaloneCommand && args.ConfigService.DirectoryExists.GetValueOrDefault(false) != true)
				{
					throw CliExceptions.CONFIG_DOES_NOT_EXISTS;
				}
				
				if (ConfigService.IsRedirected && command is IHaveRedirectionConcerns<TArgs> redirectionValidation)
				{
					var tw = new StringBuilder();
					var ctx = args.Provider.GetService<InvocationContext>();
					redirectionValidation.ValidationRedirection(ctx, command, args, tw, out var valid);
					if (!valid)
					{
						Log.Warning($"Cannot redirect command \n{tw}");
						throw new CliException($"Cannot redirect command");
					}
				}

				await command.Handle(args);
				
			}, binder);
			root.AddCommand(command);
			return factory;
		});

		return collection;
	}
}
