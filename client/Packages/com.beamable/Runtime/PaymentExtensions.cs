// unset

namespace Beamable.Api.Payments
{
	public static class PaymentExtensions
	{
		public static string GetLocalizedText(this Price self, BeamContext ctx = null)
		{
			ctx = ctx ?? BeamContext.Default;
			if (ctx.ServiceProvider.GetService<IBeamablePurchaser>()
			       .TryGetLocalizedPrice(self.symbol, out string price))
				return price;
			return string.Empty;
		}
	}
}
