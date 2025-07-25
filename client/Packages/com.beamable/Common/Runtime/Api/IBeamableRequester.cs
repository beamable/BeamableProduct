// this file was copied from nuget package Beamable.Common@5.1.0
// https://www.nuget.org/packages/Beamable.Common/5.1.0

using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Serialization.SmallerJSON;
using System.Text;
using UnityEngine.Networking;


namespace Beamable.Common.Api
{
	public interface IPlatformTimeObserver
	{
		/// <summary>
		/// The most recent time included on a response from Beamable.
		/// Use <see cref="PlatformTimeObserverExtensions.GetLatestServerTimestamp"/> to see a UNIX timestamp.
		/// </summary>
		public DateTimeOffset LatestServerTime { get; }
		
		/// <summary>
		/// The most recent time a response from Beamable was received. 
		/// </summary>
		public DateTimeOffset LatestReceiveTime { get; }
	}

	public static class PlatformTimeObserverExtensions
	{
		/// <summary>
		/// A UTC based timestamp (number of milliseconds) representing the most recent DateTimeOffset received from Beamable
		/// </summary>
		/// <param name="timeObserver"></param>
		/// <returns></returns>
		public static long GetLatestServerTimestamp(this IPlatformTimeObserver timeObserver)
		{
			return timeObserver.LatestServerTime.ToUniversalTime().ToUnixTimeMilliseconds();
		}

		/// <summary>
		/// Get the number of milliseconds between the <see cref="IPlatformTimeObserver.LatestServerTime"/> and <see cref="IPlatformTimeObserver.LatestReceiveTime"/>.
		/// This represents a loose notion of latency between the Beamable servers and the device. 
		/// </summary>
		/// <param name="timeObserver"></param>
		/// <returns></returns>
		public static double GetLatestServerTimeDifferenceMilliseconds(this IPlatformTimeObserver timeObserver)
		{
			return (timeObserver.LatestReceiveTime - timeObserver.LatestServerTime).TotalMilliseconds;
		}
	}
	
	public interface IRequester : IBeamableRequester
	{
		/// <summary>
		/// Make a request to the Beamable API
		/// </summary>
		/// <param name="req">A <see cref="SDKRequesterOptions{T}"/> that defines the request</param>
		/// <typeparam name="T">the return type of the data</typeparam>
		/// <returns>A promise that will complete with your data</returns>
		Promise<T> BeamableRequest<T>(SDKRequesterOptions<T> req);
	}

	public interface ISDKRequesterOptionData
	{
		Method Method { get; }
		string Uri { get; }
	}

	public struct SDKRequesterOptionData : ISDKRequesterOptionData
	{
		public Method Method { get; set; }
		public string Uri { get; set; }
	}

	public interface ISDKRequesterOptionDataView : ISDKRequesterOptionData
	{
		public object Body { get; }
		public bool UseCache { get; }
		public bool IncludeAuthHeader { get; }
		public bool UseConnectivityPreCheck { get; }
		public bool DisableScopeHeader { get; }
	}

	public static class SDKRequesterOptionDataViewExtensions
	{ 
		public static string ConvertToJson(this ISDKRequesterOptionDataView view)
		{
			return Json.Serialize(view, new StringBuilder());
		}
	}

	public struct SDKRequesterOptions<T> : ISDKRequesterOptionDataView
	{
		/// <summary>
		/// The HTTP method to use
		/// </summary>
		public Method method;

		/// <summary>
		/// The C# instance body of the request
		/// </summary>
		public object body;

		/// <summary>
		/// The request path, this should be relative to the domain. 
		/// </summary>
		public string uri;

		/// <summary>
		/// When true, if there is no network connectivity, the response will be created from the last successful request that matched
		/// the method and uri.
		/// </summary>
		public bool useCache;

		/// <summary>
		/// When true, the request will include an Authorization header
		/// </summary>
		public bool includeAuthHeader;

		/// <summary>
		/// When true, the request will check for internet connectivity before sending the request. 
		/// </summary>
		public bool useConnectivityPreCheck;

		/// <summary>
		/// When true, the requester should not send any scope headers on the request.
		/// </summary>
		public bool disableScopeHeaders;

		/// <summary>
		/// A function that takes a JSON string and should output the response of the method. 
		/// </summary>
		public Func<string, T> parser;

		/// <summary>
		/// A function that receives request headers and returns a finalized set of headers.
		/// This can be useful to inject or modify headers. 
		/// </summary>
		public Func<Dictionary<string, string>, Dictionary<string, string>> headerInterceptor;

		public Method Method => method;
		public string Uri => uri;

		public SDKRequesterOptions(SDKRequesterOptions<T> clone)
		{
			method = clone.method;
			uri = clone.uri;
			body = clone.body;
			useCache = clone.useCache;
			includeAuthHeader = clone.includeAuthHeader;
			useConnectivityPreCheck = clone.useConnectivityPreCheck;
			parser = clone.parser;
			disableScopeHeaders = clone.disableScopeHeaders;
			headerInterceptor = clone.headerInterceptor;
		}
		
		public object Body => body;
		public bool UseCache => useCache;
		public bool IncludeAuthHeader => includeAuthHeader;
		public bool UseConnectivityPreCheck => useConnectivityPreCheck;
		public bool DisableScopeHeader => disableScopeHeaders;
	}

	public enum BeamableBackendType
	{
		Scala,
		ProtoActor
	}
	public enum Method
	{
		GET = 1,
		POST = 2,
		PUT = 3,
		DELETE = 4
	}

	public static class MethodUtil
	{

		/// <summary>
		/// Convert the <see cref="Method"/> into a readable string.
		/// </summary>
		/// <param name="method">The <see cref="Method"/></param>
		/// <returns>A string version the method that mirrors its value</returns>
		public static string ToReadableString(this Method method)
		{
			switch (method)
			{
				case Method.GET:
					return "get";
				case Method.DELETE:
					return "delete";
				case Method.PUT:
					return "put";
				case Method.POST:
					return "post";
				default:
					return "unknown";
			}
		}

		/// <summary>
		/// Given a string, try to parse it into a <see cref="Method"/> enum.
		/// Valid values are case-insensitive version of Get, Post, Put, and Delete.
		/// </summary>
		/// <param name="str">The string to parse</param>
		/// <param name="res">The <see cref="Method"/> value associated with the <paramref name="str"/> if it was parsable. If the return of the method is false, do not use the value of this out parameter.</param>
		/// <returns>True if the <paramref name="res"/> out variable has been set, and the <paramref name="str"/> was a valid <see cref="Method"/>. False otherwise.</returns>
		public static bool TryParseMethod(string str, out Method res)
		{
			res = Method.GET;
			switch (str?.ToLower())
			{
				case "get":
					res = Method.GET;
					return true;
				case "post":
					res = Method.POST;
					return true;
				case "delete":
					res = Method.DELETE;
					return true;
				case "put":
					res = Method.PUT;
					return true;
				default:
					return false;
			}
		}
	}

	[Serializable]
	public class EmptyResponse
	{
		public static readonly EmptyResponse Unit = new EmptyResponse();
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
	public interface IBeamableRequester : IUrlEscaper
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
		/// If the type of the <paramref name="body"/> is an object, it will be serialized to JSON.
		/// If the type of the <paramref name="body"/> is a string, no serialization will occur.
		/// </param>
		/// <param name="includeAuthHeader">
		/// When <paramref name="includeAuthHeader"/> is true, the <see cref="IAccessToken.Token"/> value will be automatically
		/// passed in as the Authorization Header for the request. When the <paramref name="includeAuthHeader"/> is false, no
		/// Authorization Header will be sent with the network request.
		/// </param>
		/// <param name="parser">
		/// By default, the network response will be assumed JSON and deserialized as such. However, if a <paramref name="parser"/>
		/// is provided, the network response will be given the parser as a string. The parser can convert the string into the
		/// expected result type, <typeparamref name="T"/>
		/// </param>
		/// <param name="useCache">
		/// When <paramref name="useCache"/> is enabled, the network response will be written to disk and indexed by the
		/// <paramref name="uri"/>, <paramref name="method"/>, and <paramref name="includeAuthHeader"/>.
		/// If the same request is sent later when the player is offline, the disk value will be read and given as a valid response.
		/// </param>
		/// <typeparam name="T">
		/// The type of the network response. The network response will be deserialized into an instance of <typeparamref name="T"/>.
		/// You can override the parsing by passing a custom <paramref name="parser"/>
		/// </typeparam>
		/// <returns>
		/// A <see cref="Promise{T}"/> of type <typeparamref name="T"/> when the network request completes.
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
	}

	public interface IUrlEscaper
	{
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
		/// If the type of the <paramref name="body"/> is an object, it will be serialized to JSON.
		/// If the type of the <paramref name="body"/> is a string, no serialization will occur.
		/// </param>
		/// <param name="headers">
		/// A dictionary where the keys are header names, and the values are the header values.
		/// </param>
		/// <param name="contentType">
		/// A MIME type for the request. For example, "application/json".
		/// If the <paramref name="body"/> is serialized to JSON, the <paramref name="contentType"/> will become "application/json" by default.
		/// </param>
		/// <param name="parser">
		/// By default, the network response will be assumed JSON and deserialized as such. However, if a <paramref name="parser"/>
		/// is provided, the network response will be given the parser as a string. The parser can convert the string into the
		/// expected result type, <typeparamref name="T"/>
		/// </param>
		/// <typeparam name="T">
		/// The type of the network response. The network response will be deserialized into an instance of <typeparamref name="T"/>.
		/// You can override the parsing by passing a custom <paramref name="parser"/>
		/// </typeparam>
		/// <returns>
		/// A <see cref="Promise{T}"/> of type <typeparamref name="T"/> when the network request completes.
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
		/// <summary>
		/// The HTTP status code of the request.
		/// </summary>
		public long Status => _responseCode;
		private readonly long _responseCode;

		/// <summary>
		/// A <see cref="BeamableRequestError"/> structure that contains details about the error.
		/// The <see cref="BeamableRequestError.status"/> should be equal to the <see cref="Status"/> property.
		/// </summary>
		public BeamableRequestError RequestError { get; private set; }

		/// <summary>
		/// The HTTP <see cref="Method"/> of the request that failed.
		/// </summary>
		public Method Method { get; private set; }

		/// <summary>
		/// The HTTP Uri of the method that failed.
		/// </summary>
		public string Uri { get; private set; }

		/// <summary>
		/// The raw JSON body of the response that failed the request.
		/// </summary>
		public string Payload { get; private set; }

		/// <summary>
		/// A string that indicates what type of failure this error represents.
		/// It will one of the following
		/// <list type="bullet">
		/// <item>
		///  <see cref="Constants.Requester.ERROR_PREFIX_UNITY_SDK"/> which indicates the request originated from the HTTP requester, likely from the Unity SDK.
		/// </item>
		/// <item>
		///  <see cref="Constants.Requester.ERROR_PREFIX_WEBSOCKET_RES"/> which indicates the request originated came from the WSS requester, likely form the Microservice SDK.
		/// </item>
		/// <item>
		///  <see cref="Constants.Requester.ERROR_PREFIX_MICROSERVICE"/> which indicates the error originated came from the Microservice.
		/// </item>
		/// </list>
		/// </summary>
		public string Prefix { get; private set; }

		public RequesterException(string prefix, string method, string uri, long responseCode, BeamableRequestError err)
			: this(prefix, method, uri, responseCode, err == null ? null : JsonSerializable.ToJson(err))
		{

		}

		public RequesterException(string prefix, string method, string uri, long responseCode, string responsePayload)
		   : base(GenerateMessage(prefix, method, uri, responseCode, responsePayload))
		{
			if (string.IsNullOrEmpty(responsePayload))
			{
				RequestError = null;
			}
			else
			{
				try
				{
					RequestError =
						JsonSerializable.FromJson<BeamableRequestError>(responsePayload, throwOnInvalidJson: true);
				}
				catch (ArgumentException ex)
				{
					RequestError = new BeamableRequestError
					{
						error = "invalid json",
						message = ex.Message,
						service = "",
						status = responseCode
					};
				}
			}


			if (MethodUtil.TryParseMethod(method, out var meth))
			{
				Method = meth;
			}
			Uri = uri;
			Prefix = prefix;
			Payload = responsePayload;
			_responseCode = responseCode;
		}
		static string GenerateMessage(string prefix, string method, string uri, long responseCode, string responsePayload)
		{
			return $"{prefix}. method=[{method}] uri=[{uri}] code=[{responseCode}] payload=[{responsePayload}]";
		}
	}

	[Serializable]
	public class BeamableRequestError : JsonSerializable.ISerializable
	{
		/// <summary>
		/// The http status code
		/// </summary>
		public long status;

		/// <summary>
		/// The Beamable service where the error originated
		/// </summary>
		public string service;

		/// <summary>
		/// The Beamable error code.
		/// </summary>
		public string error;

		/// <summary>
		/// A more detailed message about the error.
		/// </summary>
		public string message;

		public void Serialize(JsonSerializable.IStreamSerializer s)
		{
			s.Serialize("status", ref status);
			s.Serialize("service", ref service);
			s.Serialize("error", ref error);
			s.Serialize("message", ref message);
		}
	}

	/// <summary>
	/// The <see cref="NoConnectivityExceptionSource"/> captures the possible reasons why a <see cref="NoConnectivityException"/> can exist
	/// </summary>
	public enum NoConnectivityExceptionSource
	{
		/// <summary>
		/// Represents a request that was attempted and failed due a http-status=0 code.
		/// </summary>
		FAILED_REQUEST,
		
		/// <summary>
		/// Represents a request that attempted to use the offlineCache, but failed due to a cache-miss.
		/// These errors do not re-trigger an offline state. 
		/// </summary>
		CACHE_MISS,
		
		/// <summary>
		/// Represents a request that never even attempted to transmit, because the system was already found to be in an offline-state.
		/// </summary>
		EXISTING_OUTAGE
	}

	/// <summary>
	/// The <see cref="BeamableConnectionNotEstablishedException"/> occurs when
	/// a Beamable request is attempted, but the system believes it is in a no-internet state.
	/// Internet will not be re-established until the ConnectivityChecker runs again and succeeds. 
	/// </summary>
	public class BeamableConnectionNotEstablishedException : NoConnectivityException
	{
		public BeamableConnectionNotEstablishedException(ISDKRequesterOptionDataView requestData) : base(requestData)
		{
			
		}
	}
	
	/// <summary>
	/// The <see cref="BeamableConnectionFailedException"/> occurs when
	/// a Beamable request actively fails due to a lack of Internet. 
	/// </summary>
	public class BeamableConnectionFailedException : NoConnectivityException
	{
		public BeamableConnectionFailedException(ISDKRequesterOptionDataView requestData, UnityWebRequest failedRequest) : base(
			requestData, failedRequest)
		{
			
		}
	}

	/// <summary>
	/// The <see cref="OfflineCacheMissException"/> occurs when a
	/// Beamable request attempts to use the offlineCache, and no cache-key is found.
	/// </summary>
	public class OfflineCacheMissException : NoConnectivityException
	{
		public OfflineCacheMissException(string key, string description) : base(key, description)
		{
			
		}
	}
	
	
	/// <summary>
	/// An error that comes from the <see cref="IBeamableRequester"/> or the <see cref="IHttpRequester"/>
	/// when there is no internet connectivity.
	/// </summary>
	public class NoConnectivityException : Exception
	{
		/// <summary>
		/// The <see cref="NoConnectivityExceptionSource"/> explains why the exception exists
		/// </summary>
		public NoConnectivityExceptionSource source;
		
		/// <summary>
		/// This field only exists if the <see cref="source"/> is <see cref="NoConnectivityExceptionSource.FAILED_REQUEST"/>.
		/// Then, this field captures the failed Unity Web Request. Otherwise, this field is null.
		/// </summary>
		public UnityWebRequest failedRequest;
		
		/// <summary>
		/// This field only exists when the <see cref="source"/> is not <see cref="NoConnectivityExceptionSource.CACHE_MISS"/>.
		/// Then, this field captures the request data that would spawn a Unity Web Request. 
		/// </summary>
		public ISDKRequesterOptionDataView requestData;
		
		/// <summary>
		/// This field only exists when the <see cref="source"/> is <see cref="NoConnectivityExceptionSource.CACHE_MISS"/>.
		/// Then, this field represents the cache-key that was missed. 
		/// </summary>
		public string CacheKey;
		
		/// <summary>
		/// This field only exists when the <see cref="source"/> is <see cref="NoConnectivityExceptionSource.CACHE_MISS"/>.
		/// Then, this field describes the cache-key that was missed. 
		/// </summary>
		public string CacheKeyDescription;

		/// <summary>
		/// true when the <see cref="source"/> is <see cref="NoConnectivityExceptionSource.CACHE_MISS"/>
		/// </summary>
		public bool CausedByCacheMiss => source == NoConnectivityExceptionSource.CACHE_MISS;
		
		/// <summary>
		/// true when the <see cref="source"/> is <see cref="NoConnectivityExceptionSource.FAILED_REQUEST"/>
		/// </summary>
		public bool CausedByFailedRequest => source == NoConnectivityExceptionSource.FAILED_REQUEST;
		
		/// <summary>
		/// true when the <see cref="source"/> is <see cref="NoConnectivityExceptionSource.EXISTING_OUTAGE"/>
		/// </summary>
		public bool CausedByExistingOutage => source == NoConnectivityExceptionSource.EXISTING_OUTAGE;
		
		protected NoConnectivityException(ISDKRequesterOptionDataView requestData, UnityWebRequest failedRequest)
			: base($"NoConnectivity detected through failed web request. " +
			       $"opts-json=[{requestData?.ConvertToJson()}] " +
			       $"request-error=[{failedRequest?.error}] " +
			       $"request-url=[{failedRequest?.url}] " +
			       $"request-status=[{failedRequest?.responseCode}] ")
		{
			source = NoConnectivityExceptionSource.FAILED_REQUEST;
			this.requestData = requestData;
			this.failedRequest = failedRequest;
		}

		protected NoConnectivityException(string key, string description)
			: base("NoConnectivity detected through cache miss. " +
			       $"cache-key=[{key}] " +
			       $"cache-key-desc=[{description}]")
		{
			source = NoConnectivityExceptionSource.CACHE_MISS;
			CacheKey = key;
			CacheKeyDescription = description;
		}

		protected NoConnectivityException(ISDKRequesterOptionDataView requestData)
			: base("NoConnectivity detected because Beamable is still not reachable. " +
			       $"opts-json=[{requestData?.ConvertToJson()}]" )
		{
			this.requestData = requestData;
			source = NoConnectivityExceptionSource.EXISTING_OUTAGE;
		}
		
		[Obsolete("As of 1.19.22, this should only be used for testing. Instead, use one of the sub-types.")]
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
		/// successful <typeparamref name="T"/> instance.</param>
		/// <typeparam name="T">
		/// The promise's inner type
		/// </typeparam>
		/// <returns>
		/// A promise chain that will use the given <paramref name="recovery"/> method in the event of the 404 error.
		/// </returns>
		public static Promise<T> RecoverFrom404<T>(this Promise<T> self, System.Func<RequesterException, T> recovery)
		   => RecoverFromStatus(self, 404, recovery);

		/// <summary>
		/// Add a way to safely recover from a promise that has failed specifically due to a
		/// <see cref="RequesterException"/> with a 401, 403, or 404 <see cref="IRequestErrorWithStatus.Status"/> code.
		/// </summary>
		/// <param name="self">The promise that has failed.</param>
		/// <param name="recovery">A recovery lambda that accepts the original <see cref="RequesterException"/> and produces a valid
		/// successful <typeparamref name="T"/> instance.</param>
		/// <typeparam name="T">
		/// The promise's inner type
		/// </typeparam>
		/// <returns>
		/// A promise chain that will use the given <paramref name="recovery"/> method in the event of the 401, 403, or 404 error.
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
		/// successful <typeparamref name="T"/> instance.</param>
		/// <typeparam name="T">
		/// The promise's inner type
		/// </typeparam>
		/// <returns>
		/// A promise chain that will use the given <paramref name="recovery"/> method in the event of the error.
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
		/// successful <typeparamref name="T"/> instance.</param>
		/// <typeparam name="T">
		/// The promise's inner type
		/// </typeparam>
		/// <returns>
		/// A promise chain that will use the given <paramref name="recovery"/> method in the event of the error.
		/// </returns>
		public static Promise<T> RecoverFromStatus<T>(this Promise<T> self, long status, System.Func<RequesterException, T> recovery)
		{
			return RecoverFromStatus(self, new long[] { status }, recovery);
		}
	}
}

namespace Beamable.Api
{
	public static class ConnectivityExceptionExtensions
	{
		public static Promise<T> RecoverFromNoConnectivity<T>(this Promise<T> self, Func<T> recovery) =>
			self.RecoverFromNoConnectivity(_ => recovery());

		public static Promise<T> RecoverFromNoConnectivity<T>(this Promise<T> self, Func<NoConnectivityException, T> recovery)
		{
			return self.Recover(ex =>
			{
				if (ex is NoConnectivityException err) return recovery(err);
				throw ex;
			});
		}
	}
}

