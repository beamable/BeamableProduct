namespace cli;

public class AccessTokenOption : ConfigurableOption
{
	public AccessTokenOption() : base(
		Constants.CONFIG_ACCESS_TOKEN,
		"The access token to use for the requests")
	{
	}
}
