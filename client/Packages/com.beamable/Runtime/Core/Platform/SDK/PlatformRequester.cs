#if !BEAMABLE_DISABLE_VERSION_HEADERS
#define BEAMABLE_ENABLE_VERSION_HEADERS
#else
#undef BEAMABLE_ENABLE_VERSION_HEADERS
#endif

using Beamable.Api.Analytics;
using Beamable.Api.Caches;
using Beamable.Api.Connectivity;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Common.Pooling;
using Beamable.Common.Scheduler;
using Beamable.Common.Spew;
using Beamable.Serialization;
using Core.Platform.SDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Beamable.Config;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace Beamable.Api
{

	public interface IPlatformRequesterErrorHandler
	{
		Promise<T> HandleError<T>(Exception ex,
								  string contentType,
								  byte[] body,
								  SDKRequesterOptions<T> opts);
	}

	public interface IPlatformRequesterHostResolver
	{
		string Host { get; }
		PackageVersion PackageVersion { get; }
	}

	public class DefaultPlatformRequesterHostResolver :IPlatformRequesterHostResolver
	{
		public string Host { get; set; }
		public PackageVersion PackageVersion { get; set; }
	}


	public interface IServiceRoutingResolution
	{
		Promise Init();
		string RoutingMap { get; }
	}

	public static class ServiceRoutingResolutionExtensions
	{
		public static Dictionary<string, string> ApplyRoutingHeaders(this IServiceRoutingResolution self,
		                                                             Dictionary<string, string> headers)
		{
			// if the routing map is empty, do not include the header
			if (string.IsNullOrEmpty(self.RoutingMap))
				return headers;
			
			headers[Constants.Requester.HEADER_ROUTINGKEY] = self.RoutingMap;
			return headers;
		}
	}

	public class DefaultServiceRoutingResolution : IServiceRoutingResolution
	{
		private IServiceRoutingStrategy _strategy;
		private string _routingMap;

		public DefaultServiceRoutingResolution(IServiceRoutingStrategy strategy)
		{
			_strategy = strategy;
		}

		public async Promise Init()
		{
			_routingMap = await _strategy.GetRoutingHeaderValue();
		}

		public string RoutingMap => _routingMap;
	}

	/// <summary>
	/// This type defines the %PlatformRequester.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.API script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public class PlatformRequester : IPlatformRequester, IHttpRequester, IRequester, IPlatformTimeObserver
	{
		private readonly IDependencyProvider _provider;
		private const string ACCEPT_HEADER = "application/json";
		private const string SSL_ERROR = "Unable to complete SSL connection";

		private readonly PackageVersion _beamableVersion;
		protected AccessTokenStorage accessTokenStorage;
		private IConnectivityService _connectivityService;
		private IServiceRoutingResolution _routingKeyResolution;
		private bool _disposed;
		private bool internetConnectivity;
		public string Host => _resolver.Host;
		public string Cid { get; set; }
		public string Pid { get; set; }

		public IAccessToken AccessToken => Token;
		public AccessToken Token { get; set; }
		public string Shard { get; set; }
		public string Language { get; set; }
		public string TimeOverride { get; set; }

		private IAuthApi _authService;

		public IAuthApi AuthService
		{
			private get
			{
				if (_authService == null)
				{
					if (_provider == null)
					{
						Debug.LogError("PlatformRequester does not have an authService, nor does it have a provider to get one."); // expect a null reference.
					}
					_authService = _provider.GetService<IAuthApi>();
				}

				return _authService;
			}
			set
			{
				_authService = value;
			}
		}

		private string _requestTimeoutMs = null;
		private int _timeoutSeconds = Constants.Requester.DEFAULT_APPLICATION_TIMEOUT_SECONDS;

		public IPlatformRequesterErrorHandler ErrorHandler { get; set; }

		public string RequestTimeoutMs
		{
			get => _requestTimeoutMs;
			set
			{
				_requestTimeoutMs = value;
				if (int.TryParse(value, out var ms) && ms > 0)
				{
					_timeoutSeconds = ms / 1000;
				}
				else
				{
					_timeoutSeconds = Constants.Requester.DEFAULT_APPLICATION_TIMEOUT_SECONDS;
				}
			}
		}

		private readonly OfflineCache _offlineCache;
		private IPlatformRequesterHostResolver _resolver;

		public PlatformRequester(IDependencyProvider provider)
		{
			_resolver = provider.GetService<IPlatformRequesterHostResolver>();
			_provider = provider;
			_beamableVersion = _resolver.PackageVersion;
			accessTokenStorage = provider.GetService<AccessTokenStorage>();
			_connectivityService = provider.GetService<IConnectivityService>();
			_offlineCache = provider.GetService<OfflineCache>();

			if (provider.CanBuildService<IServiceRoutingResolution>())
			{
				_routingKeyResolution = provider.GetService<IServiceRoutingResolution>();
			}
		}

		public PlatformRequester(string host, PackageVersion beamableVersion, AccessTokenStorage accessTokenStorage, IConnectivityService connectivityService, OfflineCache offlineCache)
		{
			_resolver = new DefaultPlatformRequesterHostResolver
			{
				Host = host,
				PackageVersion = beamableVersion
			};
			_beamableVersion = beamableVersion;
			this.accessTokenStorage = accessTokenStorage;
			_connectivityService = connectivityService;
			_offlineCache = offlineCache;
		}

		public PlatformRequester RemoveConnectivityChecks()
		{
			_connectivityService = null;
			return this;
		}

		public IBeamableRequester WithAccessToken(TokenResponse token)
		{
			PlatformRequester CreateInstance()
			{
				if (_provider == null)
				{
					return new PlatformRequester(Host, _beamableVersion, accessTokenStorage, _connectivityService,
												 _offlineCache);
				}
				return new PlatformRequester(_provider);
			}

			var requester = CreateInstance();
			requester.Cid = Cid;
			requester.Pid = Pid;
			requester.Shard = Shard;
			requester.TimeOverride = TimeOverride;
			requester.AuthService = AuthService;
			requester.Token = new AccessToken(accessTokenStorage, Cid, Pid, token.access_token, token.refresh_token,
											  token.expires_in);
			return requester;
		}

		public Promise<T> ManualRequest<T>(Method method, string url, object body = null, Dictionary<string, string> headers = null, string contentType = "application/json", Func<string, T> parser = null)
		{
			byte[] bodyBytes = null;

			if (body != null)
			{
				bodyBytes = body is string json ? Encoding.UTF8.GetBytes(json) : Encoding.UTF8.GetBytes(JsonUtility.ToJson(body));
				contentType = "application/json";
			}

			var result = new Promise<T>();
			var request = BuildWebRequest(method, url, contentType, bodyBytes);

			if (headers != null)
			{
				foreach (var header in headers)
				{
					request.SetRequestHeader(header.Key, header.Value);
				}
			}

			var op = request.SendWebRequest();
			op.completed += _ =>
			{
				try
				{
					var responsePayload = request.downloadHandler.text;
					if (request.responseCode >= 300 || request.IsNetworkError())
					{
						result.CompleteError(new HttpRequesterException(responsePayload));
					}
					else
					{
						T parsedResult = parser == null
							? JsonUtility.FromJson<T>(responsePayload)
							: parser(responsePayload);
						result.CompleteSuccess(parsedResult);
					}
				}
				catch (Exception ex)
				{
					result.CompleteError(ex);
				}
				finally
				{
					request.Dispose();
				}
			};
			return result;
		}

		public string EscapeURL(string url)
		{
			return UnityWebRequest.EscapeURL(url);
		}

		public void DeleteToken(bool eraseFromDisk=true)
		{
			if (eraseFromDisk)
			{
				Token?.Delete();
				Token?.DeleteAsCustomerScoped();
			}

			Token = null;
		}

		public void Dispose()
		{
			_disposed = true;
		}

		public UnityWebRequest BuildWebRequest(string contentType, byte[] body, ISDKRequesterOptionData opts)
		{
			var address = opts.Uri.Contains("://") ? opts.Uri : $"{Host}{opts.Uri}";

			var enableCompression = body?.Length > Gzip.MINIMUM_BYTES_FOR_COMPRESSION;

			// Prepare the request
			var request = new UnityWebRequest(address)
			{
				downloadHandler = new DownloadHandlerBuffer(),
				method = opts.Method.ToString()
			};

			if (enableCompression)
			{
				request.SetRequestCompressionHeader();
			}

			// Set the body
			if (body != null)
			{
				var upload = new UploadHandlerRaw(enableCompression ? Gzip.Compress(body) : body) { contentType = contentType };
				request.uploadHandler = upload;
			}

			request.timeout = _timeoutSeconds;
			return request;
		}

		public UnityWebRequest BuildWebRequest(Method method, string uri, string contentType, byte[] body)
		{
			return BuildWebRequest(contentType, body, new SDKRequesterOptionData
			{
				Method = method,
				Uri = uri
			});
		}

		[Obsolete("Use " + nameof(Request) + " instead")]
		public Promise<T> RequestForm<T>(string uri, WWWForm form, bool includeAuthHeader = true)
		{
			return RequestForm<T>(uri, form, Method.POST, includeAuthHeader);
		}

		[Obsolete("Use " + nameof(Request) + " instead")]
		public Promise<T> RequestForm<T>(string uri, WWWForm form, Method method, bool includeAuthHeader = true)
		{
			var opts = new SDKRequesterOptions<T>
			{
				uri = uri,
				method = method,
				includeAuthHeader = includeAuthHeader
			};
			return MakeRequestWithTokenRefresh("application/x-www-form-urlencoded", form.data,
			   opts);
		}

		public Promise<T> Request<T>(Method method,
									 string uri,
									 object body = null,
									 bool includeAuthHeader = true,
									 Func<string, T> parser = null,
									 bool useCache = false)
		{
			return BeamableRequest(new SDKRequesterOptions<T>
			{
				method = method,
				body = body,
				includeAuthHeader = includeAuthHeader,
				uri = uri,
				parser = parser,
				useCache = useCache,
				useConnectivityPreCheck = true
			});
		}

		public Promise<T> BeamableRequest<T>(SDKRequesterOptions<T> req)
		{
			string contentType = null;
			byte[] bodyBytes = null;

			if (req.body != null)
			{
				bodyBytes = req.body is string json ? Encoding.UTF8.GetBytes(json) : Encoding.UTF8.GetBytes(JsonUtility.ToJson(req.body));
				contentType = "application/json";
			}

			var safetyClone = new SDKRequesterOptions<T>(req); // TODO: since its a struct, do we need to do this? 
			return MakeRequestWithTokenRefresh<T>(contentType, bodyBytes, safetyClone);
		}

		[Obsolete]
		public Promise<T> RequestJson<T>(Method method, string uri, JsonSerializable.ISerializable body,
		   bool includeAuthHeader = true)
		{
			const string contentType = "application/json";
			var jsonFields = JsonSerializable.Serialize(body);

			using (var pooledBuilder = StringBuilderPool.StaticPool.Spawn())
			{
				var json = Serialization.SmallerJSON.Json.Serialize(jsonFields, pooledBuilder.Builder);
				var bodyBytes = Encoding.UTF8.GetBytes(json);
				return MakeRequestWithTokenRefresh<T>(contentType, bodyBytes, new SDKRequesterOptions<T>
				{
					uri = uri,
					method = method,
					includeAuthHeader = includeAuthHeader
				});
			}
		}

		private Promise<T> MakeRequestWithTokenRefresh<T>(
			string contentType,
			byte[] body,
			SDKRequesterOptions<T> opts)
		{
			internetConnectivity = !opts.useConnectivityPreCheck || ((_connectivityService?.HasConnectivity ?? true) && !(_connectivityService?.Disabled ?? false));

			if (internetConnectivity)
			{
				return MakeRequest<T>(contentType, body, opts)
					   .FlatMap(res =>
					   {
						   if (_connectivityService == null) return Promise<T>.Successful(res);
						   return _connectivityService?.SetHasInternet(true)
													  .Recover(_ => PromiseBase.Unit)
													  .Map(_ => res);
					   })
					   .RecoverWith(error =>
					   {
						   var httpNoInternet = error is NoConnectivityException ||
												error is PlatformRequesterException noInternet &&
												noInternet.Status == 0;

						   if (httpNoInternet)
						   {
							   _connectivityService?.ReportInternetLoss();
						   }

						   if (opts.useCache && httpNoInternet && Application.isPlaying &&
							   _offlineCache.UseOfflineCache)
						   {
							   return _offlineCache.Get<T>(opts.uri, Token, opts.includeAuthHeader);
						   }
						   else if (httpNoInternet)
						   {
							   return Promise<T>.Failed(error); 
						   }

						   return HandleError<T>(error, contentType, body, opts);

					   }).Then(_response =>
					   {

						   if (opts.useCache && Token != null && Application.isPlaying && _offlineCache.UseOfflineCache)
						   {
							   _offlineCache.Set<T>(opts.uri, _response, Token, opts.includeAuthHeader);
						   }
					   });
			}
			else if (!internetConnectivity && opts.useCache && Application.isPlaying && _offlineCache.UseOfflineCache)
			{
				return _offlineCache.Get<T>(opts.uri, Token, opts.includeAuthHeader);
			}
			else
			{
				return Promise<T>.Failed(new BeamableConnectionNotEstablishedException(opts));
			}
		}

		protected virtual async Promise<T> HandleError<T>(Exception error,
													string contentType,
													byte[] body,
													SDKRequesterOptions<T> opts)
		{
			if (error is PlatformRequesterException code && (code?.Error?.error == "InvalidTokenError" ||
															 code?.Error?.error == "ExpiredTokenError" || code?.Error.error == "TokenValidationError"))
			{
				try
				{
					var analytics = _provider.GetService<IAnalyticsTracker>();
					var userId = _provider.GetService<IUserContext>().UserId;
					var settings = _provider.GetService<ITokenEventSettings>();
					var oldToken = Token;
					if (settings.EnableTokenAnalytics)
					{
						analytics.TrackEvent(TokenEvent.InvalidAccessToken(playerId: userId,
						                                                   accessToken: oldToken?.Token, 
						                                                   refreshToken: oldToken?.RefreshToken, 
						                                                   error: code?.Error.error), true);
					}
					
					var nextToken = await AuthService.LoginRefreshToken(Token.RefreshToken);
					Token = new AccessToken(accessTokenStorage, Cid, Pid, nextToken.access_token,
											nextToken.refresh_token, nextToken.expires_in);

					if (settings.EnableTokenAnalytics)
					{
						analytics.TrackEvent(
							TokenEvent.GetNewToken(playerId: userId, 
							                       newAccessToken: Token?.Token, 
							                       newRefreshToken: Token?.RefreshToken, 
							                       oldAccessToken: oldToken?.Token,
							                       oldRefreshToken: oldToken?.RefreshToken), true);
						analytics.TrackEvent(
							TokenEvent.ChangingToken(playerId: userId,
							                         newAccessToken: Token?.Token,
							                         newRefreshToken: Token?.RefreshToken,
							                         oldAccessToken: oldToken?.Token,
							                         oldRefreshToken: oldToken?.RefreshToken), true);
					}

					await Token.Save();
				}
				catch (Exception err)
				{
					Debug.LogError($"Failed to refresh account for {Token.RefreshToken} for uri=[{opts.uri}] method=[{opts.method}] includeAuth=[{opts.includeAuthHeader}]");
					Debug.LogException(err);
				}

				return await MakeRequest(contentType, body, opts);
			}

			if (ErrorHandler != null)
			{
				return await ErrorHandler.HandleError(error, contentType, body, opts);
			}
			throw error;
		}

		protected Promise<T> MakeRequest<T>(
			string contentType,
		   byte[] body,
		   SDKRequesterOptions<T> opts)
		{
			return Promise.RetryPromise<T>(() =>
			{
				var result = new Promise<T>();
				var request = PrepareWebRequester(contentType, body, opts);
				var op = request.SendWebRequest();
				op.completed += _ => HandleResponse<T>(result, request, opts);
				return result;
			}, (ex) =>
			{
				if (ex?.Message?.ToLowerInvariant()?.Contains(SSL_ERROR.ToLowerInvariant()) ?? false)
				{
					// this is an SSL error, and from empirical observation, maybe it'll work if we try again...
					PlatformLogger.Log($"<b>[PlatformRequester][{opts.method.ToString()}]</b> {Host}{opts.uri} -- trying again due to ssl error.");
					return true;
				}
				// the error is not a known retry case... 
				return false;
			});

		}
		
		[Conditional("BEAMABLE_ENABLE_VERSION_HEADERS")]
		protected void AddVersionHeaders(Dictionary<string, string> headers)
		{
#if !BEAMABLE_DISABLE_VERSION_HEADERS
			headers[Constants.Requester.HEADER_BEAMABLE_VERSION] = _beamableVersion.ToString();
			headers[Constants.Requester.HEADER_APPLICATION_VERSION] = Application.version;
			headers[Constants.Requester.HEADER_UNITY_VERSION] = Application.unityVersion;
			headers[Constants.Requester.HEADER_ENGINE_TYPE] = $"Unity-{Application.platform}";
#endif
		}

		protected virtual void AddCidPidHeaders(Dictionary<string, string> headers)
		{
			if (!string.IsNullOrEmpty(Cid))
			{
				if (!string.IsNullOrEmpty(Pid))
				{
					headers[Constants.Requester.HEADER_SCOPE] = $"{Cid}.{Pid}";
				}
				else
				{
					headers[Constants.Requester.HEADER_SCOPE] = $"{Cid}";
				}
			}
		}

		protected void AddAuthHeader<T>(Dictionary<string, string> headers, SDKRequesterOptions<T> opts)
		{
			if (opts.includeAuthHeader)
			{
				var authHeader = GenerateAuthorizationHeader();
				if (authHeader != null)
				{
					headers[Constants.Requester.HEADER_AUTH] = authHeader;
				}
			}
		}

		protected virtual void AddShardHeader(Dictionary<string, string> headers)
		{
			if (Shard != null)
			{
				headers[Constants.Requester.HEADER_SHARD] = Shard;
			}
		}

		protected void AddTimeOverrideHeader(Dictionary<string, string> headers)
		{
			if (TimeOverride != null)
			{
				headers[Constants.Requester.HEADER_TIME_OVERRIDE] = TimeOverride;
			}
		}

		protected void AddRequestTimeoutHeader(Dictionary<string, string> headers)
		{
			if (RequestTimeoutMs != null)
			{
				headers[Constants.Requester.HEADER_TIMEOUT] = RequestTimeoutMs;
			}
		}

		protected virtual string GetAcceptHeader() => ACCEPT_HEADER;


		private UnityWebRequest PrepareWebRequester<T>(string contentType, byte[] body, SDKRequesterOptions<T> opts)
		{
			PlatformLogger.Log($"<b>[PlatformRequester][{opts.method.ToString()}]</b> {Host}{opts.uri}");

			// Prepare the request
			UnityWebRequest request = BuildWebRequest(contentType, body, opts);

			var headers = new Dictionary<string, string>();
			headers["Accept"] = GetAcceptHeader();
			
			if (!opts.disableScopeHeaders)
			{
				AddCidPidHeaders(headers);
			}

			_routingKeyResolution?.ApplyRoutingHeaders(headers);
			
			AddVersionHeaders(headers);
			AddAuthHeader(headers, opts);
			AddShardHeader(headers);
			AddTimeOverrideHeader(headers);
			AddRequestTimeoutHeader(headers);

			headers[Constants.Requester.HEADER_ACCEPT_LANGUAGE] = "";

			if (opts.headerInterceptor != null)
			{
				headers = opts.headerInterceptor.Invoke(headers);
			}

			foreach (var kvp in headers)
			{
				request.SetRequestHeader(kvp.Key, kvp.Value);
			}
			
			return request;
		}

		private void HandleResponse<T>(Promise<T> promise, UnityWebRequest request, SDKRequesterOptions<T> opts)
		{
			// swallow any responses if already disposed
			if (_disposed)
			{
				PlatformLogger.Log("<b>[PlatformRequester]</b> Disposed, Ignoring Response");
				return;
			}

			try
			{
				var responsePayload = request.downloadHandler.text;


				if (request.IsNetworkError())
				{
					PlatformLogger.Log($"<b>[PlatformRequester][NetworkError]</b> {typeof(T).Name}");
					promise.CompleteError(new BeamableConnectionFailedException(opts, request));
				}
				else if (request.responseCode >= 300)
				{
					NoteServerTime(request);
					// Handle errors
					PlatformError platformError = null;
					try
					{
						platformError = JsonUtility.FromJson<PlatformError>(responsePayload);
					}
					catch (Exception)
					{
						// Swallow the exception and let the error be null
					}
					promise.CompleteError(new PlatformRequesterException(platformError, request, responsePayload));
				}
				else
				{
					// Parse JSON object and resolve promise
					NoteServerTime(request);

					if (string.IsNullOrWhiteSpace(responsePayload))
					{
						PlatformLogger.Log($"<b>[PlatformRequester][Response]</b> {typeof(T).Name}");
					}
					else
					{
						PlatformLogger.Log($"<b>[PlatformRequester][Response]</b> {typeof(T).Name}: {responsePayload}");
					}

					try
					{
						T result = default;
						if (opts.parser != null)
						{
							result = opts.parser(responsePayload);
						}
						else if (typeof(T) == typeof(Unit) )
						{
							// don't even bother trying to interpret the response payload if our type is _Unit_. 
							//  we don't care about the server response anyway.
							//  btw, if the server returns `""` as a response, then the JsonUtility.FromJson call fails. 
							result = default;
						}
						else
						{
							result = JsonUtility.FromJson<T>(responsePayload);
						}
						promise.CompleteSuccess(result);
					}
					catch (Exception ex)
					{
						promise.CompleteError(ex);
					}
				}
			}
			finally
			{
				request?.Dispose();
			}
		}

		protected virtual void NoteServerTime(UnityWebRequest request)
		{
			var dateHeaderValue = request.GetResponseHeader("Date");
			LatestReceiveTime = DateTimeOffset.UtcNow;
			if (!DateTimeOffset.TryParse(dateHeaderValue, out var parsedDate))
			{
				return;
			}
			
			LatestServerTime = parsedDate;
		}

		protected virtual string GenerateAuthorizationHeader()
		{
			return Token != null ? $"Bearer {Token.Token}" : null;
		}

		public DateTimeOffset LatestServerTime { get; protected set; }
		public DateTimeOffset LatestReceiveTime { get; protected set; }
	}

}
