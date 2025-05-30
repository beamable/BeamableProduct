namespace cli;

public class RealmScopedOption : ConfigurableOptionFlag
{
	public RealmScopedOption() : base("realm-scoped", "Makes the resulting access/refresh token pair be realm scoped instead of the default customer scoped one") { }
}
