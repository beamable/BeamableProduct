using Beamable.Common;
using Beamable.Common.Dependencies;

namespace Beamable.Api.Payments
{
	/// <summary>
	/// Dummy Purchaser that is used when there is no `IBeamablePurchaser` registered in BeamContext Service Provider.
	/// </summary>
	public class DummyPurchaser : IBeamablePurchaser
	{
		public PurchasingInitializationStatus InitializationStatus { get; } = PurchasingInitializationStatus.ErrorPurchasingUnavailable;
		public Promise<Unit> Initialize(IDependencyProvider provider = null)
		{
			return Promise<Unit>.Successful(new Unit());
		}
		public string GetLocalizedPrice(string skuSymbol)
		{
			return string.Empty;
		}

		public bool TryGetLocalizedPrice(string skuSymbol, out string localizedPrice)
		{
			localizedPrice = string.Empty;
			return false;
		}

		public Promise<CompletedTransaction> StartPurchase(string listingSymbol, string skuSymbol)
		{
			return Promise<CompletedTransaction>.Failed(InitializationStatus.StatusToErrorCode());
		}
	}
}
