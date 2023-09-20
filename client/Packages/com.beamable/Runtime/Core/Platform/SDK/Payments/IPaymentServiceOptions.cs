using UnityEngine;

namespace Beamable.Api.Payments
{
	public interface IPaymentServiceOptions
	{
		/// <summary>
		/// The <see cref="ProviderId"/> is used to signal which storefront is being used.
		/// </summary>
		string ProviderId { get; }
	}

	public class DefaultPaymentServiceOptions : IPaymentServiceOptions
	{
		public string ProviderId
		{
			get
			{
				switch (Application.platform)
				{
					case RuntimePlatform.IPhonePlayer:
						return "itunes";
					case RuntimePlatform.Android:
						return "googleplay";
					default:
#if UNITY_EDITOR
						return "test";
#elif USE_STEAMWORKS
						return "steam";
#else
						return "unknown";
#endif
				}
			}
		}

	}
}
