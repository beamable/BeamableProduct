namespace cli;

public class LoginCommandArgs : CommandArgs { }

public class LoginCommand : AppCommand<LoginCommandArgs>
{
	private readonly IFakeService _fake;
	
	public LoginCommand(IFakeService fake) : base("login", "save credentials to file") { }
	public override void Configure() {}

	public override async Task Handle(LoginCommandArgs args)
	{
		// write the token that should exists in app context to a file
		await Task.Delay(54);
		Console.WriteLine("Writing");
	}
}
