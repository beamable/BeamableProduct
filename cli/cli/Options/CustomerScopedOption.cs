namespace cli;

public class CustomerScopedOption : ConfigurableOptionFlag
{
	public CustomerScopedOption() : base("customer-scoped", "Make request customer scoped instead of product only") { }
}
