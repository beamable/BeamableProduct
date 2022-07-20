namespace cli;

public class CustomerScopedOption : ConfigurableOptionFlag
{
	public CustomerScopedOption() : base("customer-scoped", "make request customer scoped instead of product only") { }
}
