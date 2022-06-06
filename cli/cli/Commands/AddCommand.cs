using System.CommandLine;

namespace cli;


public class AddCommandArgs : CommandArgs
{
	public int a;
	public int b;
}

public class AddCommand : AppCommand<AddCommandArgs>
{
	private readonly IFakeService _fake;

	public AddCommand(IFakeService fake)
		: base("add", "add 2 numbers")
	{
		_fake = fake;
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

	public override async Task Handle(AddCommandArgs args)
	{
		var sum = await _fake.AddAsync(args.a, args.b);
		Console.WriteLine("sum is " + sum);
	}
}
