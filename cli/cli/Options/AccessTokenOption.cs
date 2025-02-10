namespace cli;

public class AccessTokenOption : ConfigurableOption
{
	public AccessTokenOption() : base(Constants.CONFIG_ACCESS_TOKEN, "The access token to use for the requests." +
	                                                                 $" It overwrites the logged in user stored in {Constants.CONFIG_TOKEN_FILE_NAME} for THIS INVOCATION ONLY")
	{
	}
}
