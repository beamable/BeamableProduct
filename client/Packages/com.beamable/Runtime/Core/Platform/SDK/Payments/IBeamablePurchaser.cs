using Beamable.Common;
using Beamable.Common.Dependencies;
using System;

namespace Beamable.Api.Payments
{
	/// <summary>
	/// This type defines the %Client main entry point for the %In-App %Purchasing feature.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See the <a target="_blank" href="https://help.beamable.com/Unity-4.0/unity/user-reference/beamable-services/game-economy/stores-overview/">Store</a> feature documentation
	/// - See Beamable.API script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public interface IBeamablePurchaser
	{
		/// <summary>
		/// Purchaser initialization status.
		/// </summary>
		public PurchasingInitializationStatus InitializationStatus { get; }

		/// <summary>
		/// Begin initialization of Beamable purchasing.
		/// </summary>
		/// <param name="provider">A <see cref="IDependencyProvider"/> that will be used to grant Beamable dependencies to the purchaser.</param>
		/// <returns>A <see cref="Promise"/> representing the completion of IAP initialization.</returns>
		Promise<Unit> Initialize(IDependencyProvider provider = null);

		/// <summary>
		/// Get the localized price string for a given SKU.
		/// <param name="skuSymbol">
		/// The purchase symbol for the item. This is the skuSymbol for the offer.
		/// </param>
		/// </summary>
		[Obsolete("Use " + nameof(TryGetLocalizedPrice) + " Instead")]
		string GetLocalizedPrice(string skuSymbol);

		/// <summary>
		/// Tries to get the localized price string for a given SKU.
		/// <param name="skuSymbol">
		/// The purchase symbol for the item. This is the skuSymbol for the offer.
		/// </param>
		/// <param name="localizedPrice">
		/// Localized price value output.
		/// </param>
		/// <returns>bool value whether it found localized price for the given sku symbol</returns>
		/// </summary>
		bool TryGetLocalizedPrice(string skuSymbol, out string localizedPrice);

		/// <summary>
		/// Start a purchase through the chosen IAP implementation.
		/// </summary>
		/// <param name="listingSymbol">Listing symbol to buy.</param>
		/// <param name="skuSymbol">SKU within the mobile platform.</param>
		/// <returns>Promise with a completed transaction data structure.</returns>
		Promise<CompletedTransaction> StartPurchase(string listingSymbol, string skuSymbol);
	}
}
