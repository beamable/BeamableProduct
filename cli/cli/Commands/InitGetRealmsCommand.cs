using Beamable.Common.Api.Realms;
using Beamable.Common.BeamCli;
using Beamable.Server;

namespace cli;

public class InitGetRealmsCommandArgs : LoginCommandArgs { }

[CliContractType]
public class InitGetRealmsResult
{
	public GameWithRealms[] Games;
}

[CliContractType]
public class GameWithRealms
{
	public string GameName;
	public string Pid;
	public RealmOption[] Realms;
}

[CliContractType]
public class RealmOption
{
	public string RealmName;
	public string Pid;
	public bool IsDev;
	public bool IsStaging;
	public bool IsProduction;
}

public class InitGetRealmsCommand : AtomicCommand<InitGetRealmsCommandArgs, InitGetRealmsResult>,
	IStandaloneCommand,
	ISkipManifest
{
	private readonly LoginCommand _loginCommand;

	public InitGetRealmsCommand(LoginCommand loginCommand)
		: base("get-realms", "List all games and realms available for the given CID and credentials — use this before running init so an AI model can present the options to the user")
	{
		_loginCommand = loginCommand;
	}

	public override void Configure()
	{
		AddOption(new UsernameOption(), (args, i) => args.username = i);
		AddOption(new PasswordOption(), (args, i) => args.password = i);
		AddOption(new RefreshTokenOption(), (args, i) => args.refreshToken = i);
	}

	protected override InitGetRealmsResult GetHelpInstance()
	{
		return new InitGetRealmsResult
		{
			Games = new[]
			{
				new GameWithRealms
				{
					GameName = "My Game",
					Pid = "DE_1234567890",
					Realms = new[]
					{
						new RealmOption { RealmName = "My Game", Pid = "DE_1234567890", IsProduction = true },
						new RealmOption { RealmName = "My Game Staging", Pid = "DE_1234567891", IsStaging = true },
						new RealmOption { RealmName = "Dev - My Game", Pid = "DE_1234567892", IsDev = true },
					}
				}
			}
		};
	}

	public override async Task<InitGetRealmsResult> GetResult(InitGetRealmsCommandArgs args)
	{
		await _loginCommand.Handle(args);
		if (!_loginCommand.Successful)
			throw new CliException("Login failed — check your credentials and CID");

		var realmsApi = args.RealmsApi;
		var games = await realmsApi.GetGames();

		var gameResults = new List<GameWithRealms>();
		foreach (var game in games)
		{
			var realms = await realmsApi.GetRealms(game);
			var realmOptions = realms
				.Where(r => !r.Archived)
				.Select(r => new RealmOption
				{
					RealmName = r.DisplayName,
					Pid = r.Pid,
					IsDev = r.IsDev,
					IsStaging = r.IsStaging,
					IsProduction = r.IsProduction,
				})
				.ToArray();

			gameResults.Add(new GameWithRealms
			{
				GameName = game.ProjectName,
				Pid = game.Pid,
				Realms = realmOptions,
			});
		}

		return new InitGetRealmsResult { Games = gameResults.ToArray() };
	}
}
