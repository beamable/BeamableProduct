namespace cli;

public class RefreshTokenOption : ConfigurableOption
{
	public RefreshTokenOption() : base(Constants.CONFIG_REFRESH_TOKEN, $"A Refresh Token to use for the requests. " +
	                                                                   $"It overwrites the logged in user stored in {Constants.CONFIG_TOKEN_FILE_NAME} for THIS INVOCATION ONLY")
	{
	}
}
