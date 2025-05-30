using Beamable.Api.Autogenerated.Auth;
using Beamable.Api.Autogenerated.Models;
using Beamable.Common.Content;
using Beamable.Server;
using System.CommandLine;

namespace cli.TokenCommands;

public class GetTokenViaRefreshCommandArgs : CommandArgs
{
	public string refreshToken;
}

public class GetTokenViaRefreshCommandOutput
{
	public string accessToken;
	public string challengeToken;
	public long expiresIn;
	public string refreshToken;
	public string[] scopes;
	public string tokenType;
}


public class GetTokenViaRefreshCommand : AtomicCommand<GetTokenViaRefreshCommandArgs, GetTokenViaRefreshCommandOutput>, ISkipManifest
{
	public GetTokenViaRefreshCommand() : base("from-refresh", "Get an access token from a refresh token")
	{
	}

	public override void Configure()
	{
		var tokenOpt = new Option<string>("--token",
			"The token that you want to get information for. This must be a refresh token. By default, the current refresh token of the .beamable context is used");
		tokenOpt.AddAlias("-t");
		AddOption(tokenOpt, (args, context, value) =>
		{
			if (!string.IsNullOrEmpty(value))
			{
				args.refreshToken = value;
				return;
			}
			
			var provider = context.GetService<AppServices>();
			var ctx = provider.GetService<IAppContext>();
			args.refreshToken = ctx.Token.RefreshToken;
			return;
		});
	}

	public override async Task<GetTokenViaRefreshCommandOutput> GetResult(GetTokenViaRefreshCommandArgs args)
	{
		var api = args.Provider.GetService<IAuthApi>();
		TokenResponse  res = await api.PostToken(new TokenRequestWrapper
		{
			grant_type = "refresh_token", 
			refresh_token = args.refreshToken
		});
		return new GetTokenViaRefreshCommandOutput
		{
			scopes = res.scopes,
			refreshToken = res.refresh_token,
			accessToken = res.access_token,
			challengeToken = res.challenge_token,
			expiresIn = res.expires_in,
			tokenType = res.token_type
		};
	}
}
