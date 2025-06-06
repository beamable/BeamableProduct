using Beamable.Api.Autogenerated.Accounts;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.BeamCli;
using Beamable.Server;
using cli.Utils;
using PromiseExtensions = cli.Utils.PromiseExtensions;

namespace cli;

public class AccountMeCommandArgs : CommandArgs
{
}

public class AccountMeCommandOutput
{
	/// <summary>
	/// The unique id of the player, sometimes called a "dbid".
	/// </summary>
	public long id;

	/// <summary>
	/// If the player has associated an email with their account, the email will appear here. Null otherwise.
	/// The email can be associated with the <see cref="IAuthApi.RegisterDBCredentials"/> method
	/// </summary>
	public string email;

	/// <summary>
	/// If the player has chosen a language for their account, the language code will appear here. EN by default.
	/// </summary>
	public string language;

	/// <summary>
	/// Scopes are permissions that the player has over the Beamable ecosystem.
	/// Most players will have no scopes.
	/// Players with the role of "tester" will have some "read" based scopes,
	/// Players with the role of "developer" will have most all scopes except those relating to team management, and
	/// Players with the role of "admin" will have single scope with the value of "*", which indicates ALL scopes.
	/// </summary>
	public List<string> scopes;

	/// <summary>
	/// If the player has associated any third party accounts with their account, those will appear here.
	/// The values of the strings will be taken from the <see cref="AuthThirdPartyMethods.GetString"/> method.
	/// Third parties can be associated with the <see cref="IAuthApi.RegisterThirdPartyCredentials"/> method.
	/// </summary>
	public List<string> thirdPartyAppAssociations;

	/// <summary>
	/// If the player has associated any device Ids with their account, those will appear here.
	/// </summary>
	public List<string> deviceIds;

	/// <summary>
	/// If the player has associated any external identities with their account, they will appear here.
	/// </summary>
	public List<AccountMeExternalIdentity> external;

	/// <summary>
	/// Current CID for the token of the active user.
	/// </summary>
	public string tokenCid;

	/// <summary>
	/// Current PID for the token of the active user.
	/// </summary>
	public string tokenPid;

	/// <summary>
	/// Current Access Token for the token of the active user.
	/// </summary>
	public string accessToken;

	/// <summary>
	/// Current Refresh Token for the token of the active user.
	/// </summary>
	public string refreshToken;

	/// <summary>
	/// Current Expiration Time for the token of the active user.
	/// </summary>
	public DateTime tokenExpiration;

	/// <summary>
	/// The time when the token was issued.
	/// </summary>
	public DateTime tokenIssuedAt;

	/// <summary>
	/// The duration of validity for the access token.
	/// </summary>
	public long tokenExpiresIn;
}

[Serializable]
public class AccountMeExternalIdentity
{
	public string providerNamespace;
	public string providerService;
	public string userId;
}

public class AccountMeCommand : AtomicCommand<AccountMeCommandArgs, AccountMeCommandOutput>, ISkipManifest
{
	public override int Order => 200;
	public AccountMeCommand() : base("me", "Fetch the current account") { }

	public override void Configure()
	{
	}

	protected override AccountMeCommandOutput GetHelpInstance()
	{
		return new AccountMeCommandOutput
		{
			email = "user@example.com",
			deviceIds = new List<string>(),
			scopes = new List<string>(),
			language = "en",
			thirdPartyAppAssociations = new List<string>(),
			external = new List<AccountMeExternalIdentity> { },
		};
	}

	public override async Task<AccountMeCommandOutput> GetResult(AccountMeCommandArgs args)
	{
		try
		{
			// Get the Account Me admin command if the flag has been set in the args
			var token = args.AppContext.Token;
			return await AdminGetAccountMeCommand(args.Provider.GetService<IAccountsApi>(), token);
		}
		catch (Exception e)
		{
			throw new CliException($"Failed to get user data due to error: {e.Message}");
		}
	}

	private static async Task<AccountMeCommandOutput> AdminGetAccountMeCommand(IAccountsApi accountsApi, IAccessToken token)
	{
		var response = await accountsApi.GetAdminMe();
		//Get the token and fill it out args.AppContext.Token.
		return new AccountMeCommandOutput
		{
			id = response.id,
			deviceIds = new List<string>(),
			external =
				response.external.HasValue
					? response.external.Value?.Select(x => new AccountMeExternalIdentity { userId = x.userId, providerNamespace = x.providerNamespace, providerService = x.providerService }).ToList()
					: new List<AccountMeExternalIdentity>(),
			scopes = response.scopes.ToList(),
			email = response.email,
			language = response.language,
			thirdPartyAppAssociations = response.thirdPartyAppAssociations.ToList(),
			tokenCid = token.Cid,
			tokenPid = token.Pid,
			accessToken = token.Token,
			refreshToken = token.RefreshToken,
			tokenExpiration = token.ExpiresAt,
			tokenIssuedAt = token.IssuedAt,
			tokenExpiresIn = token.ExpiresIn,
		};
	}
}
