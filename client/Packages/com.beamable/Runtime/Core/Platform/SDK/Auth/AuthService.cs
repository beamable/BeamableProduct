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
		private readonly IDeviceIdResolver _deviceIdResolver;
		const string DEVICE_ID_URI = ACCOUNT_URL + "/me";
		const string DEVICE_DELETE_URI = ACCOUNT_URL + "/me/device";

		public AuthService(IBeamableRequester requester, IDeviceIdResolver deviceIdResolver=null, IAuthSettings settings = null) : base(requester, settings)
		{
			_deviceIdResolver = deviceIdResolver ?? new DefaultDeviceIdResolver();
		}

		public async Promise<bool> IsThisDeviceIdAvailable()
		{
			var deviceId = await _deviceIdResolver.GetDeviceId();
			var encodedDeviceId = Requester.EscapeURL(deviceId);
			return await Requester.Request<AvailabilityResponse>(Method.GET,
					$"{ACCOUNT_URL}/available/device-id?deviceId={encodedDeviceId}", null, false)
				.Map(resp => resp.available);
		}

		public async Promise<TokenResponse> LoginDeviceId()
		{
			var deviceId = await _deviceIdResolver.GetDeviceId();

			var req = new LoginDeviceIdRequest
			{
				grant_type = "device",
				device_id = deviceId
			};
			return await Requester.Request<TokenResponse>(Method.POST, TOKEN_URL, req);
		}

		public class LoginDeviceIdRequest
		{
			public string grant_type;
			public string device_id;
		}

		public async Promise<User> RegisterDeviceId()
		{
			var deviceId = await _deviceIdResolver.GetDeviceId();

			return await UpdateDeviceId(new RegisterDeviceIdRequest
			{
				deviceId = deviceId
			});
		}

		private Promise<User> UpdateDeviceId(RegisterDeviceIdRequest requestBody)
		{
			return Requester.Request<User>(Method.PUT, DEVICE_ID_URI, requestBody);
		}

		public async Promise<User> RemoveDeviceId()
		{
			var deviceId = await _deviceIdResolver.GetDeviceId();

			var ids = new string[] { deviceId };
			return await RemoveDeviceIds(ids);
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
