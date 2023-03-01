namespace cli;

public class SaveToEnvironmentOption : ConfigurableOptionFlag
{
	public SaveToEnvironmentOption() : base("save-to-environment", "Save login refresh token to environment variable") { }
}
