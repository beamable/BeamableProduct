namespace cli;

public class SaveToEnvironmentOption : ConfigurableOptionFlag
{
	public SaveToEnvironmentOption() : base("saveToEnvironment", "save login refresh token to environment variable") { }
}
