using Beamable.Common.BeamCli;
using Beamable.Common.Dependencies;
using Beamable.Server.Common;
using cli.Options;
using cli.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Json;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.Text;

namespace cli;

public interface IResultProvider
{
	public IDataReporterService Reporter { get; set; }
}

/// <summary>
/// when a command implements this interface, the CLI app framework will not attempt to initialize beamoLocal.
/// If this happens, the command SHOULD NOT use _ANYTHING_ in the beamoLocalManifest; otherwise nulls ahoy.
/// </summary>
public interface ISkipManifest
{
	
}

public interface IResultSteam<TChannel, TData> : IResultProvider
	where TChannel : IResultChannel, new()
{
}

public interface IReportException<T> : IResultSteam<IReportException<T>.ErrorStream, T>
	where T : ErrorOutput
{
	public class ErrorStream : IResultChannel
	{
		public string ChannelName => CliException<T>.GetChannelName();
	}
}

public interface IEmptyResult : IResultProvider
{

}

/// <summary>
/// Specifies that command does not require config to work correctly.
/// </summary>
public interface IStandaloneCommand { }


public class DefaultStreamResultChannel : IResultChannel
{
	public string ChannelName { get; } = "stream";
}

public class ProgressStreamResultChannel : IResultChannel
{
	public string ChannelName { get; } = "progressStream";
}

public class ExtraStreamResultChannel : IResultChannel
{
	public string ChannelName { get; } = "extraStream";
}

public static class ResultStreamExtensions
{
	public static void SendResults<TChannel, TData>(this IResultSteam<TChannel, TData> self, TData data)
		where TChannel : IResultChannel, new()
	{
		var channel = new TChannel(); // TODO: cache.
		self.Reporter?.Report(channel.ChannelName, data);
	}

	[Obsolete("AtomicCommands should not use this function")]
	public static void SendResults<TArgs, TResult>(this AtomicCommand<TArgs, TResult> self, TResult _)
		where TArgs : CommandArgs
		where TResult : new()
	{
		throw new InvalidOperationException(
			"An atomic command cannot use the SendResults function directly. Instead, only 1 value can be returned via the return statement");
	}
}

public interface ISingleResult
{
	Type ResultType { get; }
	object CreateEmptyInstance();
	bool IsSingleReturn { get; }
}

public abstract class StreamCommand<TArgs, TResult> : AppCommand<TArgs>,
	IResultSteam<DefaultStreamResultChannel, TResult>, ISingleResult
	where TArgs : CommandArgs
	where TResult : new()
{
	private DefaultStreamResultChannel _channel;
	public override bool AutoLogOutput => false;
	
	protected StreamCommand(string name, string description = null) : base(name, description)
	{
		_channel = new DefaultStreamResultChannel();
	}

	protected void SendResults(TResult result)
	{
		IResultSteam<DefaultStreamResultChannel, TResult> self = this;
		self.Reporter.Report(_channel.ChannelName, result);
		if (AutoLogOutput)
		{
			LogResult(result);
		}
	}

	public Type ResultType => typeof(TResult);
	public object CreateEmptyInstance()
	{
		return GetHelpInstance() ?? new TResult();
	}

	public bool IsSingleReturn => false;

	protected virtual TResult GetHelpInstance()
	{
		return default;
	}
	
	protected virtual void LogResult(object result)
	{
		var json = JsonConvert.SerializeObject(result, UnitySerializationSettings.Instance);
		AnsiConsole.Write(
			new Panel(new JsonText(json))
				.Collapse()
				.NoBorder());
	}
}

public abstract class AtomicCommand<TArgs, TResult> : AppCommand<TArgs>, IResultSteam<DefaultStreamResultChannel, TResult>, ISingleResult
	where TArgs : CommandArgs
	where TResult : new()
{
	private DefaultStreamResultChannel _channel;

	protected AtomicCommand(string name, string description = null) : base(name, description)
	{
		_channel = new DefaultStreamResultChannel();
	}

	public override async Task Handle(TArgs args)
	{
		try
		{
			var result = await GetResult(args);
			if (result == null) return;

			var reporter = args.Provider.GetService<IDataReporterService>();
			reporter.Report(_channel.ChannelName, result);
		
			if (AutoLogOutput)
			{
				LogResult(result);
			}
		}
		catch (CliException cliException)
		{
			if (!cliException.GetType().GetGenericArguments().Any(t => t.IsAssignableTo(typeof(ErrorOutput))))
			{
				throw;
			}

			LogResult(cliException.GetPayload(cliException.NonZeroOrOneExitCode, ""));
		}
	}

	protected virtual void LogResult(object result)
	{
		var json = JsonConvert.SerializeObject(result, UnitySerializationSettings.Instance);
		AnsiConsole.Write(
			new Panel(new JsonText(json))
				.Collapse()
				.NoBorder());
	}

	public abstract Task<TResult> GetResult(TArgs args);
	public Type ResultType => typeof(TResult);
	public bool IsSingleReturn => true;
	public object CreateEmptyInstance()
	{
		return GetHelpInstance() ?? new TResult();
	}

	protected virtual TResult GetHelpInstance()
	{
		return default;
	}
}

public class DefaultErrorStream : IResultChannel
{
	public const string CHANNEL = "error";
	public string ChannelName => CHANNEL;
}

public abstract class CommandGroup<TArgs> : AppCommand<TArgs>
	where TArgs : CommandArgs
{
	protected CommandGroup([NotNull] string name, [CanBeNull] string description = null) : base(name, description)
	{
	}

	public override void Configure()
	{

	}

	public override Task Handle(TArgs args)
	{
		var helpBuilder = args.Provider.GetService<HelpBuilder>();
		helpBuilder.Write(this, Console.Error);
		return Task.CompletedTask;
	}
}

public class CommandGroupArgs : CommandArgs
{

}

public abstract class CommandGroup : CommandGroup<CommandGroupArgs>
{
	protected CommandGroup([NotNull] string name, [CanBeNull] string description = null) : base(name, description)
	{
	}
}


public interface IAppCommand
{
	bool IsForInternalUse { get; }
	int Order { get; }

	public static string GetModifiedDescription(bool isForInternalUse, string description)
	{
		if (isForInternalUse)
		{
			return $"[INTERNAL] {description}";
		}

		return description;
	}
}

public interface IHasArgs<TArgs> where TArgs : CommandArgs
{

}

public interface IHaveRedirectionConcernMessage
{
	void WriteValidationMessage(Command command, TextWriter writer)
	{
		List<Option> requiredOptions = new List<Option>();
		foreach (var option in command.Options)
		{
			if (option is IAmRequiredForRedirection)
			{
				requiredOptions.Add(option);
						
			}
		}

		if (requiredOptions.Count > 0)
		{
			writer.WriteLine("The following options must be included when using CLI redirection.");
			foreach (var requiredOption in requiredOptions)
			{
				writer.WriteLine($"  {requiredOption.Name}");
			}
		}
		else
		{
			writer.WriteLine("Unknown");
		}
	}
}

public interface IAmRequiredForRedirection
{
}

public interface IAmRequiredForRedirection<T>
{
	bool IsValid(InvocationContext ctx, T value);
}

/// <summary>
/// Specifies that a command cannot be proxied.
/// When there is a local version of BEAM, and the user executes the global version of BEAM,
///   the global version will invoke a sub process to call the local version.
/// However, STD-INPUT cannot be channeled easily, and so many commands will break.
/// For now, we need to maintain this list declaratively. 
/// </summary>
public interface IHaveRedirectionConcerns<TArgs> : IHaveRedirectionConcernMessage
	where TArgs : CommandArgs 
{
	void ValidationRedirection(InvocationContext context, Command command, TArgs args, StringBuilder errorStream, out bool isValid)
	{
		DefaultValidationRedirection(context, command, args, errorStream, out isValid);
	}

	public static void DefaultValidationRedirection(InvocationContext context, Command command, TArgs args, StringBuilder errorStream, out bool isValid)
	{
		isValid = true;
		foreach (var option in command.Options)
		{
			if (option is IAmRequiredForRedirection)
			{
				var value = context.ParseResult.GetValueForOption(option);
				if (value == null)
				{
					errorStream.AppendLine("Missing value for " + option.Name);
					isValid = false;
				}
			}
		}
	}
}


public abstract partial class AppCommand<TArgs> : Command, IResultProvider, IAppCommand, IHasArgs<TArgs>
	where TArgs : CommandArgs
{
	private List<Action<BindingContext, BindingContext, TArgs>> _bindingActions = new List<Action<BindingContext, BindingContext, TArgs>>();
	private string _description;

	public virtual bool IsForInternalUse => false;
	public virtual bool AutoLogOutput => true;
	public virtual int Order => 100;


	IDataReporterService IResultProvider.Reporter { get; set; }
	public IDependencyProvider CommandProvider { get; set; }

	protected AppCommand(string name, string description = null) : base(name)
	{
		_description = description;
	}

	public override string Description { get => IAppCommand.GetModifiedDescription(IsForInternalUse, _description); set => _description = value; }


	protected Argument<T> AddArgument<T>(Argument<T> arg, Action<TArgs, T> binder)
	{
		return AddArgument<T>(arg, (args, _, i) => binder(args, i));
	}
	/// <summary>
	/// Add an argument to the current command
	/// </summary>
	/// <param name="arg">The <see cref="Argument{T}"/> to add</param>
	/// <param name="binder">
	/// A binding method to configure the <see cref="TArgs"/> instance.
	/// When this command runs, and this argument is parsed, the output value will be in the
	/// second argument in this binding method.
	/// The first argument is the instance of <see cref="TArgs"/> that will be given
	/// to the <see cref="Handle"/> method.
	/// </param>
	/// <typeparam name="T"></typeparam>
	protected Argument<T> AddArgument<T>(Argument<T> arg, Action<TArgs, BindingContext, T> binder)
	{
		ArgValidator<T> validator = CommandProvider.CanBuildService<ArgValidator<T>>()
			? CommandProvider.GetService<ArgValidator<T>>()
			: null;

		var set = new Action<BindingContext, BindingContext, TArgs>((ctx, parse, args) =>
		{
			if (validator != null)
			{
				binder(args, parse, validator.GetValue(ctx.ParseResult.FindResultFor(arg)));
			}
			else
			{
				var res = ctx.ParseResult.GetValueForArgument(arg);
				binder(args, parse, res);
			}
		});
		_bindingActions.Add(set);

		if (validator != null)
		{
			arg.AddValidator(res =>
			{
				var _ = validator.GetValue(res);
			});
		}

		base.AddArgument(arg);
		return arg;
	}

	public Option<T> AddOption<T>(Option<T> arg, Action<TArgs, T> binder, string[] aliases=null)
	{
		return AddOption<T>(arg, (args, _, b) => binder(args, b), aliases);
	}
	
	public Option<T> AddOption<T>(Option<T> arg, Action<TArgs, BindingContext, T> binder, string[] aliases=null)
	{
		ArgValidator<T> validator = CommandProvider.CanBuildService<ArgValidator<T>>()
			? CommandProvider.GetService<ArgValidator<T>>()
			: null;

		if (aliases != null)
		{
			foreach (var alias in aliases)
			{
				arg.AddAlias(alias);
			}
		}
		
		var set = new Action<BindingContext, BindingContext, TArgs>((ctx, parse, args) =>
		{
			if (validator != null)
			{
				binder(args, parse, validator.GetValue(ctx.ParseResult.FindResultFor(arg)));
			}
			else
			{
				var res = ctx.ParseResult.GetValueForOption(arg);
				binder(args, parse, res);
			}
		});

		if (validator != null)
		{
			arg.AddValidator(res =>
			{
				var _ = validator.GetValue(res);
			});
		}

		_bindingActions.Add(set);
		base.AddOption(arg);
		return arg;
	}

	/// <summary>
	/// Use this to add all arguments that will be used for this command.
	/// Please use the <see cref="AddArgument{T}"/> method from within this function.
	/// </summary>
	public abstract void Configure();

	/// <summary>
	/// This method will be invoked when the user requests this command.
	/// </summary>
	/// <param name="args">A set of arguments as configured by the binding methods used from
	/// <see cref="AddArgument{T}"/>
	/// </param>
	/// <returns>A task representing completion</returns>
	public abstract Task Handle(TArgs args);

	/// <summary>
	/// Extract options and arguments from the binding context into the base argument class.
	/// If we need to extend the binding configuration of the <see cref="TArgs"/>, or the base <see cref="CommandArgs"/> data,
	/// each command can override this behaviour.
	/// </summary>
	/// <param name="provider"></param>
	/// <param name="args"></param>
	/// <param name="bindingContext"></param>
	protected virtual void BindBaseContext(IServiceProvider provider, TArgs args, BindingContext bindingContext)
	{
		args.Dryrun = bindingContext.ParseResult.GetValueForOption(provider.GetRequiredService<DryRunOption>());
		args.Quiet = bindingContext.ParseResult.GetValueForOption(provider.GetRequiredService<QuietOption>());
		args.IgnoreStandaloneValidation =
			bindingContext.ParseResult.GetValueForOption(provider.GetRequiredService<SkipStandaloneValidationOption>());
	}

	public class Binder : BinderBase<TArgs>
	{
		private readonly AppCommand<TArgs> _command;
		private readonly IDependencyProvider _provider;

		public Binder(AppCommand<TArgs> command, IDependencyProvider provider)
		{
			_command = command;
			_provider = provider;
		}
		protected override TArgs GetBoundValue(BindingContext bindingContext)
		{
			var args = _provider.GetRequiredService<TArgs>();
			// extract the service layer and add it to the arg's execution scope.
			args.Provider = bindingContext.GetService(typeof(AppServices)) as AppServices;

			_command.BindBaseContext(_provider, args, bindingContext);
			foreach (var action in _command._bindingActions)
			{
				action?.Invoke(bindingContext, bindingContext, args);
			}
			return args;
		}
	}

}

public interface ICommandFactory
{

}

public interface ICommandFactory<T> where T : Command
{

}

public class CommandFactory<T> : ICommandFactory<T> where T : Command
{
}

public class CommandFactory : ICommandFactory
{

}

