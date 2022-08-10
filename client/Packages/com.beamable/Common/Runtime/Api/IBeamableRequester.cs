using Beamable.Common.Api.Auth;
using System;
using System.Collections.Generic;
using System.Linq;

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
	public class EmptyResponse
	{
	}

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
		/// <summary>
		/// The <see cref="IAccessToken"/> the <see cref="IBeamableRequester"/> will use when making network calls
		/// with the <see cref="Request{T}"/> method.
		/// </summary>
		IAccessToken AccessToken { get; }

		/// <summary>
		/// The customer id associated with this requester
		/// </summary>
		string Cid { get; }

		/// <summary>
		/// The project id associated with this requester
		/// </summary>
		string Pid { get; }

		/// <summary>
		/// Make an authorized request to the Beamable API.
		/// </summary>
		/// <param name="method">One of the common HTTP methods represented through the <see cref="Method"/> enum</param>
		/// <param name="uri">A Beamable API relative uri. The uri may contain URL parameters. </param>
		/// <param name="body">
		/// The body of the network request.
		/// If the type of the <see cref="body"/> is an object, it will be serialized to JSON.
		/// If the type of the <see cref="body"/> is a string, no serialization will occur.
		/// </param>
		/// <param name="includeAuthHeader">
		/// When <see cref="includeAuthHeader"/> is true, the <see cref="IAccessToken.Token"/> value will be automatically
		/// passed in as the Authorization Header for the request. When the <see cref="includeAuthHeader"/> is false, no
		/// Authorization Header will be sent with the network request.
		/// </param>
		/// <param name="parser">
		/// By default, the network response will be assumed JSON and deserialized as such. However, if a <see cref="parser"/>
		/// is provided, the network response will be given the parser as a string. The parser can convert the string into the
		/// expected result type, <see cref="T"/>
		/// </param>
		/// <param name="useCache">
		/// When <see cref="useCache"/> is enabled, the network response will be written to disk and indexed by the
		/// <see cref="uri"/>, <see cref="method"/>, and <see cref="includeAuthHeader"/>.
		/// If the same request is sent later when the player is offline, the disk value will be read and given as a valid response.
		/// </param>
		/// <typeparam name="T">
		/// The type of the network response. The network response will be deserialized into an instance of <see cref="T"/>.
		/// You can override the parsing by passing a custom <see cref="parser"/>
		/// </typeparam>
		/// <returns>
		/// A <see cref="Promise{T}"/> of type <see cref="T"/> when the network request completes.
		/// </returns>
		Promise<T> Request<T>(
		   Method method,
		   string uri,
		   object body = null,
		   bool includeAuthHeader = true,
		   Func<string, T> parser = null,
		   bool useCache = false
		);

		/// <summary>
		/// Create a new <see cref="IBeamableRequester"/> that will authenticate all requests using the given <see cref="TokenResponse"/> argument.
		/// <b>WARNING</b>, this method is not supported inside Microservices.
		/// </summary>
		/// <param name="tokenResponse">A <see cref="TokenResponse"/> that will be used to create the <see cref="AccessToken"/> for the resulting requester.</param>
		/// <returns>A new <see cref="IBeamableRequester"/></returns>
		IBeamableRequester WithAccessToken(TokenResponse tokenResponse);

		/// <summary>
		/// A utility method that will url escape a string.
		/// </summary>
		/// <param name="url">a url string</param>
		/// <returns>the given url string, but with character escaping.</returns>
		string EscapeURL(string url);
	}

	public interface IHttpRequester
	{
		/// <summary>
		/// Make an HTTP request to any address with no preconfigured authorization
		/// </summary>
		/// <param name="method">One of the common HTTP methods represented through the <see cref="Method"/> enum</param>
		/// <param name="url"></param>
		/// <param name="body">
		/// The body of the network request.
		/// If the type of the <see cref="body"/> is an object, it will be serialized to JSON.
		/// If the type of the <see cref="body"/> is a string, no serialization will occur.
		/// </param>
		/// <param name="headers">
		/// A dictionary where the keys are header names, and the values are the header values.
		/// </param>
		/// <param name="contentType">
		/// A MIME type for the request. For example, "application/json".
		/// If the <see cref="body"/> is serialized to JSON, the <see cref="contentType"/> will become "application/json" by default.
		/// </param>
		/// <param name="parser">
		/// By default, the network response will be assumed JSON and deserialized as such. However, if a <see cref="parser"/>
		/// is provided, the network response will be given the parser as a string. The parser can convert the string into the
		/// expected result type, <see cref="T"/>
		/// </param>
		/// <typeparam name="T">
		/// The type of the network response. The network response will be deserialized into an instance of <see cref="T"/>.
		/// You can override the parsing by passing a custom <see cref="parser"/>
		/// </typeparam>
		/// <returns>
		/// A <see cref="Promise{T}"/> of type <see cref="T"/> when the network request completes.
		/// </returns>
		Promise<T> ManualRequest<T>(Method method,
		   string url,
		   object body = null,
		   Dictionary<string, string> headers = null,
		   string contentType = "application/json",
		   Func<string, T> parser = null);

		/// <summary>
		/// A utility method that will url escape a string.
		/// </summary>
		/// <param name="url">a url string</param>
		/// <returns>the given url string, but with character escaping.</returns>
		string EscapeURL(string url);
	}

	/// <summary>
	/// An exception that comes from an HTTP request
	/// </summary>
	public class HttpRequesterException : Exception
	{
		public HttpRequesterException(string msg) : base(msg)
		{

		}
	}

	/// <summary>
	/// An exception that comes from a request made through the <see cref="IHttpRequester"/> or the <see cref="IBeamableRequester"/>
	/// </summary>
	public class RequesterException : Exception, IRequestErrorWithStatus
	{
		public long Status => _responseCode;
		private readonly long _responseCode;

		public RequesterException(string prefix, string method, string uri, long responseCode, string responsePayload)
		   : base(GenerateMessage(prefix, method, uri, responseCode, responsePayload))
		{
			_responseCode = responseCode;
		}
		static string GenerateMessage(string prefix, string method, string uri, long responseCode, string responsePayload)
		{
			return $"{prefix}. method=[{method}] uri=[{uri}] code=[{responseCode}] payload=[{responsePayload}]";
		}
	}

	/// <summary>
	/// An error that comes from the <see cref="IBeamableRequester"/> or the <see cref="IHttpRequester"/>
	/// when there is no internet connectivity.
	/// </summary>
	public class NoConnectivityException : Exception
	{
		public NoConnectivityException(string message) : base(message) { }
	}

	public interface IRequestErrorWithStatus
	{
		/// <summary>
		/// The HTTP status code.
		/// Successful codes are in the 200 range.
		/// See <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Status"> this MDN article</a> for more HTTP status codes.
		/// </summary>
		long Status { get; }
	}

	public static class PromiseRequesterExtensions
	{
		/// <summary>
		/// Add a way to safely recover from a promise that has failed specifically due to a
		/// <see cref="RequesterException"/> with a 404 <see cref="IRequestErrorWithStatus.Status"/> code.
		/// </summary>
		/// <param name="self">The promise that has failed.</param>
		/// <param name="recovery">A recovery lambda that accepts the original <see cref="RequesterException"/> and produces a valid
		/// successful <see cref="T"/> instance.</param>
		/// <typeparam name="T">
		/// The promise's inner type
		/// </typeparam>
		/// <returns>
		/// A promise chain that will use the given <see cref="recovery"/> method in the event of the 404 error.
		/// </returns>
		public static Promise<T> RecoverFrom404<T>(this Promise<T> self, System.Func<RequesterException, T> recovery)
		   => RecoverFromStatus(self, 404, recovery);

		/// <summary>
		/// Add a way to safely recover from a promise that has failed specifically due to a
		/// <see cref="RequesterException"/> with a 401, 403, or 404 <see cref="IRequestErrorWithStatus.Status"/> code.
		/// </summary>
		/// <param name="self">The promise that has failed.</param>
		/// <param name="recovery">A recovery lambda that accepts the original <see cref="RequesterException"/> and produces a valid
		/// successful <see cref="T"/> instance.</param>
		/// <typeparam name="T">
		/// The promise's inner type
		/// </typeparam>
		/// <returns>
		/// A promise chain that will use the given <see cref="recovery"/> method in the event of the 401, 403, or 404 error.
		/// </returns>
		public static Promise<T> RecoverFrom40x<T>(this Promise<T> self, System.Func<RequesterException, T> recovery)
			=> RecoverFromStatus(self, new long[] { 401, 403, 404 }, recovery);

		/// <summary>
		/// Add a way to safely recover from a promise that has failed specifically due to a
		/// <see cref="RequesterException"/> with a given set of <see cref="IRequestErrorWithStatus.Status"/> codes
		/// </summary>
		/// <param name="self">The promise that has failed.</param>
		/// <param name="status">A set of HTTP status codes that the recovery will run for.</param>
		/// <param name="recovery">A recovery lambda that accepts the original <see cref="RequesterException"/> and produces a valid
		/// successful <see cref="T"/> instance.</param>
		/// <typeparam name="T">
		/// The promise's inner type
		/// </typeparam>
		/// <returns>
		/// A promise chain that will use the given <see cref="recovery"/> method in the event of the error.
		/// </returns>
		public static Promise<T> RecoverFromStatus<T>(this Promise<T> self, long[] status, System.Func<RequesterException, T> recovery)
		{
			return self.Recover(err =>
			{
				if (err is RequesterException platformErr && status.Contains(platformErr.Status))
				{
					return recovery(platformErr);
				}
				throw err;
			});
		}

		/// <summary>
		/// Add a way to safely recover from a promise that has failed specifically due to a
		/// <see cref="RequesterException"/> with a given <see cref="IRequestErrorWithStatus.Status"/> code
		/// </summary>
		/// <param name="self">The promise that has failed.</param>
		/// <param name="status">An HTTP status code that the recovery will run for.</param>
		/// <param name="recovery">A recovery lambda that accepts the original <see cref="RequesterException"/> and produces a valid
		/// successful <see cref="T"/> instance.</param>
		/// <typeparam name="T">
		/// The promise's inner type
		/// </typeparam>
		/// <returns>
		/// A promise chain that will use the given <see cref="recovery"/> method in the event of the error.
		/// </returns>
		public static Promise<T> RecoverFromStatus<T>(this Promise<T> self, long status, System.Func<RequesterException, T> recovery)
		{
			return RecoverFromStatus(self, new long[status], recovery);
		}
	}
}
