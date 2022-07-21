namespace cli;

public class HeaderOption : ConfigurableOptionList
{
	public HeaderOption() :
		base(Constants.CONFIG_HEADER, "Custom header") { }
}
