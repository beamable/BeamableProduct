using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using System;
using UnityEngine;

namespace Beamable.Api.Auth
{
	/// <summary>
	/// This type defines the %Client main entry point for the %Auth feature.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/accounts-feature">Accounts</a> feature documentation
	/// - See Beamable.API script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public interface IAuthService : IAuthApi
	{
		Promise<bool> IsThisDeviceIdAvailable();
		Promise<User> RegisterDeviceId();
		Promise<User> RemoveDeviceId();
		Promise<TokenResponse> LoginDeviceId();
	}

	/// <summary>
	/// This type defines the %Client main entry point for the %Auth feature.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/accounts-feature">Accounts</a> feature documentation
	/// - See Beamable.API script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public class AuthService : AuthApi, IAuthService
	{
		const string DEVICE_ID_URI = ACCOUNT_URL + "/me";

		public AuthService(IBeamableRequester requester, IAuthSettings settings = null) : base(requester, settings) { }

		public Promise<bool> IsThisDeviceIdAvailable()
		{
			var encodedDeviceId = Requester.EscapeURL(SystemInfo.deviceUniqueIdentifier);
			return Requester.Request<AvailabilityResponse>(Method.GET,
			                                               $"{ACCOUNT_URL}/available/device-id?deviceId={encodedDeviceId}",
			                                               null, false)
			                .Map(resp => resp.available);
		}

		public Promise<TokenResponse> LoginDeviceId()
		{
			var req = new LoginDeviceIdRequest {grant_type = "device", device_id = SystemInfo.deviceUniqueIdentifier};
			return Requester.Request<TokenResponse>(Method.POST, TOKEN_URL, req);
		}

		public class LoginDeviceIdRequest
		{
			public string grant_type;
			public string device_id;
		}

		public Promise<User> RegisterDeviceId()
		{
			return UpdateDeviceId(RegisterDeviceIdRequest.Create());
		}

		public Promise<User> RemoveDeviceId()
		{
			return UpdateDeviceId(null);
		}

		private Promise<User> UpdateDeviceId(object requestBody)
		{
			return Requester.Request<User>(Method.PUT, DEVICE_ID_URI, requestBody);
		}

		[Serializable]
		private class RegisterDeviceIdRequest
		{
			public string deviceId;

			public static RegisterDeviceIdRequest Create()
			{
				var req = new RegisterDeviceIdRequest {deviceId = SystemInfo.deviceUniqueIdentifier};
				return req;
			}
		}
	}
}
