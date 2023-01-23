using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.CommandLine.Binding;

namespace cli;

public abstract class AppCommand<TArgs> : Command
	where TArgs : CommandArgs
{
	private List<Action<BindingContext, TArgs>> _bindingActions = new List<Action<BindingContext, TArgs>>();

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
	protected void AddArgument<T>(Argument<T> arg, Action<TArgs, T> binder)
	{
		var set = new Action<BindingContext, TArgs>((ctx, args) =>
		{
			var res = ctx.ParseResult.GetValueForArgument(arg);
			binder(args, res);
		});
		_bindingActions.Add(set);
		base.AddArgument(arg);
	}

	protected void AddOption<T>(Option<T> arg, Action<TArgs, T> binder)
	{
		var set = new Action<BindingContext, TArgs>((ctx, args) =>
		{
			var res = ctx.ParseResult.GetValueForOption(arg);
			binder(args, res);
		});
		_bindingActions.Add(set);
		base.AddOption(arg);
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
	}

	public class Binder : BinderBase<TArgs>
	{
		private readonly AppCommand<TArgs> _command;
		private readonly IServiceProvider _provider;

		public Binder(AppCommand<TArgs> command, IServiceProvider provider)
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
public class CommandFactory<T> : ICommandFactory<T> {}

public class CommandFactory : ICommandFactory
{

}

