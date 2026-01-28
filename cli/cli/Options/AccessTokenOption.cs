namespace cli;

public class AccessTokenOption : ConfigurableOption
{
	public AccessTokenOption() : base(ConfigService.CFG_TOKEN_JSON_FIELD_ACCESS_TOKEN, "The access token to use for the requests." +
	                                                                                   $" It overwrites the logged in user stored in {ConfigService.CFG_TOKEN_FILE_NAME} for THIS INVOCATION ONLY")
	{
	}
}
