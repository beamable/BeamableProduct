namespace cli;

public class VerboseOption : ConfigurableOptionFlag
{
	public VerboseOption() : base("verbose", "should extra logs get printed out")
	{
	}
}