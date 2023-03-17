namespace cli;

public class RefreshTokenOption : ConfigurableOption
{
	public RefreshTokenOption() : base(
		Constants.CONFIG_REFRESH_TOKEN,
		"Refresh token to use for the requests")
	{
	}
}
