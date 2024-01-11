namespace cli;

public class RealmConfigKeyValueOption : ConfigurableOptionList
{
	public RealmConfigKeyValueOption() : base("key-values", "A list of realm config key/value pairs in a 'namespace|key::value' format")
	{ }
}
