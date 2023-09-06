namespace cli;

public class PlainOutputOption : ConfigurableOptionFlag
{
	public PlainOutputOption() : base("plain-output",
		"Make command returns plain text without custom colors and formatting")
	{ }
}
