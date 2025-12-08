namespace cli;

public class RefreshTokenOption : ConfigurableOption
{
	public RefreshTokenOption() : base(ConfigService.CFG_TOKEN_JSON_FIELD_REFRESH_TOKEN, $"A Refresh Token to use for the requests. " +
	                                                                                    $"It overwrites the logged in user stored in {ConfigService.CFG_TOKEN_FILE_NAME} for THIS INVOCATION ONLY")
	{
	}
}
