namespace Beamable.Api.Payments
{
	/// <summary>
	/// Current status of <see cref="IBeamablePurchaser"/> initialization.
	/// </summary>
	public enum PurchasingInitializationStatus
	{
		/// <summary>
		/// Not initialized yet.
		/// </summary>
		NotInitialized,
		/// <summary>
		/// Initialization is in progress.
		/// </summary>
		InProgress,
		/// <summary>
		/// Up and running.
		/// </summary>
		Success,
		/// <summary>
		/// In-App Purchases disabled in device settings.
		/// </summary>
		ErrorPurchasingUnavailable,

		/// <summary>
		/// No products available for purchase,
		/// Typically indicates a configuration error.
		/// </summary>
		ErrorNoProductsAvailable,

		/// <summary>
		/// The store reported the app as unknown.
		/// Typically, indicates the app has not been created
		/// on the relevant developer portal, or the wrong
		/// identifier has been configured.
		/// </summary>
		ErrorAppNotKnown,
		/// <summary>
		/// There are no SKUs content configured.
		/// </summary>
		/// <remarks>
		/// This can be the issue if the content is configured, but was not published.
		/// During Purchaser initialization we ask backend to provide information about configured SKUs.
		/// That means that the content needs to be published to be detected.
		/// </remarks>
		CancelledNoSkusConfigured,
		/// <summary>
		/// Call to Beamable backend to get SKUs content failed.
		/// </summary>
		ErrorFailedToGetSkus,
		/// <summary>
		/// Unknown error not covered by other values.
		/// </summary>
		ErrorUnknown,
	}

	public static class UnityBeamablePurchaserUtilExtensions
	{
		public static ErrorCode StatusToErrorCode(this PurchasingInitializationStatus status)
		{
			return new ErrorCode(5000 + (long)status, GameSystem.GAME_CLIENT, "Not initialized correctly.",
			                     status.ToString());
		}
	}
}
