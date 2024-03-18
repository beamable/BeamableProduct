using Beamable.Common;
using Beamable.Common.Api.Auth;
using cli.Utils;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Json;
using UnityEngine;

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
}

[Serializable]
public class AccountMeExternalIdentity
{
	public string providerNamespace;
	public string providerService;
	public string userId;
}

public class AccountMeCommand : AtomicCommand<AccountMeCommandArgs, AccountMeCommandOutput>
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
			external = new List<AccountMeExternalIdentity>
			{
			}
		};
	}

	public override async Task<AccountMeCommandOutput> GetResult(AccountMeCommandArgs args)
	{
		try
		{
			var response = await args.AuthApi.GetUser().ShowLoading("Sending Request...");
			return new AccountMeCommandOutput
			{
				id = response.id,
				deviceIds = response.deviceIds,
				external = response.external?.Select(x => new AccountMeExternalIdentity
				{
					userId = x.userId,
					providerNamespace = x.providerNamespace,
					providerService = x.providerService
				}).ToList(),
				scopes = response.scopes,
				email = response.email,
				language = response.language,
				thirdPartyAppAssociations = response.thirdPartyAppAssociations
			};
		}
		catch (Exception e)
		{
			throw new CliException($"Failed to get user data due to error: {e.Message}");
		}
	}
}
