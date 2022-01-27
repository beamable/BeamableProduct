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
		Promise<User> RemoveDeviceIds(string[] deviceIds);
		Promise<User> RemoveAllDeviceIds();
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
		const string DEVICE_DELETE_URI = ACCOUNT_URL + "/me/device";

		public AuthService(IBeamableRequester requester, IAuthSettings settings = null) : base(requester, settings)
		{
		}

		public Promise<bool> IsThisDeviceIdAvailable()
		{
			var encodedDeviceId = Requester.EscapeURL(SystemInfo.deviceUniqueIdentifier);
			return Requester.Request<AvailabilityResponse>(Method.GET,
					$"{ACCOUNT_URL}/available/device-id?deviceId={encodedDeviceId}", null, false)
				.Map(resp => resp.available);
		}

		public Promise<TokenResponse> LoginDeviceId()
		{
			var req = new LoginDeviceIdRequest
			{
				grant_type = "device",
				device_id = SystemInfo.deviceUniqueIdentifier
			};
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

		private Promise<User> UpdateDeviceId(RegisterDeviceIdRequest requestBody)
		{
			return Requester.Request<User>(Method.PUT, DEVICE_ID_URI, requestBody);
		}

		public Promise<User> RemoveDeviceId()
		{
			var ids = new string[] { SystemInfo.deviceUniqueIdentifier };
			return RemoveDeviceIds(ids);
		}

		public Promise<User> RemoveAllDeviceIds()
		{
			return RemoveDeviceIds(null);
		}

		public Promise<User> RemoveDeviceIds(string[] deviceIds)
		{
			object body = new EmptyResponse();
			if (deviceIds != null)
			{
				body = DeleteDevicesRequest.Create(deviceIds);
			}
			return Requester.Request<User>(Method.DELETE, DEVICE_DELETE_URI, body);
		}

		[Serializable]
		private class RegisterDeviceIdRequest
		{
			public string deviceId;

			public static RegisterDeviceIdRequest Create()
			{
				var req = new RegisterDeviceIdRequest
				{
					deviceId = SystemInfo.deviceUniqueIdentifier
				};
				return req;
			}
		}

		[Serializable]
		private class DeleteDevicesRequest
		{
			public string[] deviceIds;

			public static DeleteDevicesRequest Create(string[] ids)
			{
				var req = new DeleteDevicesRequest
				{
					deviceIds = ids
				};

				return req;
			}
		}
	}
}
