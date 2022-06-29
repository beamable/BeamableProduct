namespace cli;

public class RefreshTokenOption : ConfigurableOption
{
	public RefreshTokenOption() : base(
		Constants.CONFIG_REFRESH_TOKEN,
		"The refresh token to use for the requests")
	{
	}
}
