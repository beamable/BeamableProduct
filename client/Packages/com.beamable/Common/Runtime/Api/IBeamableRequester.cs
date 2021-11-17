using System;
using System.Collections.Generic;
using Beamable.Common.Api.Auth;

namespace Beamable.Common.Api
{
	public enum Method
	{
		GET = 1,
		POST = 2,
		PUT = 3,
		DELETE = 4
	}

	[Serializable]
	public class EmptyResponse { }

	/// <summary>
	/// This type defines the %IBeamableRequester.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.API script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public interface IBeamableRequester
	{
		IAccessToken AccessToken
		{
			get;
		}

		Promise<T> Request<T>(Method method,
		                      string uri,
		                      object body = null,
		                      bool includeAuthHeader = true,
		                      Func<string, T> parser = null,
		                      bool useCache = false);

		IBeamableRequester WithAccessToken(TokenResponse tokenResponse);

		string EscapeURL(string url);
	}

	public interface IHttpRequester
	{
		Promise<T> ManualRequest<T>(Method method,
		                            string url,
		                            object body = null,
		                            Dictionary<string, string> headers = null,
		                            string contentType = "application/json",
		                            Func<string, T> parser = null);

		string EscapeURL(string url);
	}

	public class HttpRequesterException : Exception
	{
		public HttpRequesterException(string msg) : base(msg) { }
	}

	public class RequesterException : Exception, IRequestErrorWithStatus
	{
		public long Status => _responseCode;
		private readonly long _responseCode;

		public RequesterException(string prefix, string method, string uri, long responseCode, string responsePayload)
			: base(GenerateMessage(prefix, method, uri, responseCode, responsePayload))
		{
			_responseCode = responseCode;
		}

		static string GenerateMessage(string prefix,
		                              string method,
		                              string uri,
		                              long responseCode,
		                              string responsePayload)
		{
			return $"{prefix}. method=[{method}] uri=[{uri}] code=[{responseCode}] payload=[{responsePayload}]";
		}
	}

	public interface IRequestErrorWithStatus
	{
		long Status
		{
			get;
		}
	}

	public static class PromiseRequesterExtensions
	{
		public static Promise<T> RecoverFrom404<T>(this Promise<T> self, System.Func<RequesterException, T> recovery) =>
			RecoverFromStatus(self, 404, recovery);

		public static Promise<T> RecoverFromStatus<T>(this Promise<T> self,
		                                              long status,
		                                              System.Func<RequesterException, T> recovery)
		{
			return self.Recover(err =>
			{
				if (err is RequesterException platformErr && platformErr.Status == status)
				{
					return recovery(platformErr);
				}

				throw err;
			});
		}
	}
}
