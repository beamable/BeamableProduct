using Beamable.Common.BeamCli;
using Beamable.Common.Dependencies;
using cli.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Spectre.Console;
using System.CommandLine;
using System.CommandLine.Binding;
using UnityEngine;

namespace cli;

public interface IResultProvider
{
	public DataReporterService Reporter { get; set; }
}

public interface IResultSteam<TChannel, TData> : IResultProvider
	where TChannel : IResultChannel, new()
{
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

public static class ResultStreamExtensions
{
	public static void SendResults<TChannel, TData>(this IResultSteam<TChannel, TData> self, TData data)
		where TChannel : IResultChannel, new()
	{
		var channel = new TChannel(); // TODO: cache.
		self.Reporter?.Report(channel.ChannelName, data);
	}
}


public abstract partial class AppCommand<TArgs> : Command, IResultProvider
	where TArgs : CommandArgs
{
	private List<Action<BindingContext, TArgs>> _bindingActions = new List<Action<BindingContext, TArgs>>();

	DataReporterService IResultProvider.Reporter { get; set; }
	public IDependencyProvider CommandProvider { get; set; }

	protected AppCommand(string name, string description = null) : base(name, description)
	{
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
	protected Argument<T> AddArgument<T>(Argument<T> arg, Action<TArgs, T> binder)
	{
		ArgValidator<T> validator = CommandProvider.CanBuildService<ArgValidator<T>>()
			? CommandProvider.GetService<ArgValidator<T>>()
			: null;

		var set = new Action<BindingContext, TArgs>((ctx, args) =>
		{
			if (validator != null)
			{
				binder(args, validator.GetValue(ctx.ParseResult.FindResultFor(arg)));
			}
			else
			{
				var res = ctx.ParseResult.GetValueForArgument(arg);
				binder(args, res);
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

	protected Option<T> AddOption<T>(Option<T> arg, Action<TArgs, T> binder)
	{
		ArgValidator<T> validator = CommandProvider.CanBuildService<ArgValidator<T>>()
			? CommandProvider.GetService<ArgValidator<T>>()
			: null;

		var set = new Action<BindingContext, TArgs>((ctx, args) =>
		{
			if (validator != null)
			{
				binder(args, validator.GetValue(ctx.ParseResult.FindResultFor(arg)));
			}
			else
			{
				var res = ctx.ParseResult.GetValueForOption(arg);
				binder(args, res);
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
				action?.Invoke(bindingContext, args);
			}
			return args;
		}
	}

}

public interface ICommandFactory
{

}

public interface ICommandFactory<T>
{

}
public class CommandFactory<T> : ICommandFactory<T> { }

public class CommandFactory : ICommandFactory
{

}

