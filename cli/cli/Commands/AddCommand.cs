using System.CommandLine;

namespace cli;


public class AddCommandArgs : CommandArgs
{
	public int a;
	public int b;
}

public class AddCommand : AppCommand<AddCommandArgs>
{
	private readonly IAppContext _ctx;

	public AddCommand(IAppContext ctx)
		: base("add", "add 2 numbers")
	{
		_ctx = ctx;
	}

	public override void Configure()
	{
		var a = new Argument<int>(nameof(AddCommandArgs.a));
		var b = new Argument<int>(nameof(AddCommandArgs.b));
		a.Description = "the first number to add";
		b.Description = "the second number to add";

		AddArgument(a, (args, i) => args.a = i);
		AddArgument(b, (args, i) => args.b = i);
	}

	public override Task Handle(AddCommandArgs args)
	{
		Console.WriteLine(_ctx.Cid + "/" + _ctx.Pid + "/" + _ctx.Host);
		return Task.CompletedTask;
	}
}
