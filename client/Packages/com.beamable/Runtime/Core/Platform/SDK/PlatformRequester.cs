using Beamable.Api.Caches;
using Beamable.Api.Connectivity;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.Pooling;
using Beamable.Common.Spew;
using Beamable.Serialization;
using Core.Platform.SDK;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Beamable.Api
{

	public interface IPlatformRequester : IBeamableRequester
	{
		AccessToken Token { get; set; }
		string TimeOverride { get; set; }
		string Cid { get; set; }
		string Pid { get; set; }
		string Language { get; set; }
		IAuthApi AuthService { set; }
		void DeleteToken();
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
	public class PlatformRequester : IPlatformRequester, IHttpRequester
	{
		private const string ACCEPT_HEADER = "application/json";
		private AccessTokenStorage _accessTokenStorage;
		private IConnectivityService _connectivityService;
		private bool _disposed;
		private bool internetConnectivity;
		public string Host { get; set; }
		public string Cid { get; set; }
		public string Pid { get; set; }

		public IAccessToken AccessToken => Token;
		public AccessToken Token { get; set; }
		public string Shard { get; set; }
		public string Language { get; set; }
		public string TimeOverride { get; set; }
		public IAuthApi AuthService { private get; set; }
		public string RequestTimeoutMs { get; set; }
		public PlatformRequester(string host, AccessTokenStorage accessTokenStorage, IConnectivityService connectivityService)
		{
			Host = host;
			_accessTokenStorage = accessTokenStorage;
			_connectivityService = connectivityService;
		}

		public IBeamableRequester WithAccessToken(TokenResponse token)
		{
			var requester = new PlatformRequester(Host, _accessTokenStorage, _connectivityService)
			{
				Cid = Cid,
				Pid = Pid,
				Shard = Shard,
				Language = Language,
				TimeOverride = TimeOverride,
				AuthService = AuthService,
				Token = new AccessToken(_accessTokenStorage, Cid, Pid, token.access_token, token.refresh_token,
				  token.expires_in)
			};
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

		public void DeleteToken()
		{
			Token?.Delete();
			Token?.DeleteAsCustomerScoped();
			Token = null;
		}

		public void Dispose()
		{
			_disposed = true;
		}

		public UnityWebRequest BuildWebRequest(Method method, string uri, string contentType, byte[] body)
		{
			var address = uri.Contains("://") ? uri : $"{Host}{uri}";

			var enableCompression = body?.Length > Gzip.MINIMUM_BYTES_FOR_COMPRESSION;

			// Prepare the request
			var request = new UnityWebRequest(address)
			{
				downloadHandler = new DownloadHandlerBuffer(),
				method = method.ToString()
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

			return request;
		}

		public Promise<T> RequestForm<T>(string uri, WWWForm form, bool includeAuthHeader = true)
		{
			return RequestForm<T>(uri, form, Method.POST, includeAuthHeader);
		}

		public Promise<T> RequestForm<T>(string uri, WWWForm form, Method method, bool includeAuthHeader = true)
		{
			return MakeRequestWithTokenRefresh<T>(method, uri, "application/x-www-form-urlencoded", form.data,
			   includeAuthHeader);
		}

		public Promise<T> Request<T>(Method method, string uri, object body = null, bool includeAuthHeader = true, Func<string, T> parser = null, bool useCache = false)
		{
			string contentType = null;
			byte[] bodyBytes = null;

			if (body != null)
			{
				bodyBytes = body is string json ? Encoding.UTF8.GetBytes(json) : Encoding.UTF8.GetBytes(JsonUtility.ToJson(body));
				contentType = "application/json";
			}

			return MakeRequestWithTokenRefresh<T>(method, uri, contentType, bodyBytes, includeAuthHeader, parser, useCache);
		}

		public Promise<T> RequestJson<T>(Method method, string uri, JsonSerializable.ISerializable body,
		   bool includeAuthHeader = true)
		{
			const string contentType = "application/json";
			var jsonFields = JsonSerializable.Serialize(body);

			using (var pooledBuilder = StringBuilderPool.StaticPool.Spawn())
			{
				var json = Serialization.SmallerJSON.Json.Serialize(jsonFields, pooledBuilder.Builder);
				var bodyBytes = Encoding.UTF8.GetBytes(json);
				return MakeRequestWithTokenRefresh<T>(method, uri, contentType, bodyBytes, includeAuthHeader);
			}
		}

		private Promise<T> MakeRequestWithTokenRefresh<T>(
		   Method method,
		   string uri,
		   string contentType,
		   byte[] body,
		   bool includeAuthHeader,
		   Func<string, T> parser = null,
		   bool useCache = false)
		{
			internetConnectivity = _connectivityService?.HasConnectivity ?? true;

			if (internetConnectivity)
			{
				return MakeRequest<T>(method, uri, contentType, body, includeAuthHeader, parser)
				   .RecoverWith(error =>
				   {
					   var httpNoInternet = error is NoConnectivityException ||
									   error is PlatformRequesterException noInternet && noInternet.Status == 0;

					   if (httpNoInternet)
					   {
						   _connectivityService?.ReportInternetLoss();
					   }

					   if (useCache && httpNoInternet && Application.isPlaying)
					   {
						   return OfflineCache.Get<T>(uri, Token, includeAuthHeader);
					   }

					   switch (error)
					   {
						   case Exception _ when httpNoInternet:
							   return Promise<T>.Failed(new NoConnectivityException(uri + " should not be cached and requires internet connectivity. Internet connection lost."));

						   // if we get a 401 InvalidTokenError, let's refresh the token and retry the request.
						   case PlatformRequesterException code when code?.Error?.error == "InvalidTokenError":
							   Debug.LogError("Invalid token, trying again");
							   return AuthService.LoginRefreshToken(Token.RefreshToken)
							 .Map(rsp =>
							 {
								 Token = new AccessToken(_accessTokenStorage, Cid, Pid, rsp.access_token, rsp.refresh_token,
							   rsp.expires_in);
								 Token.Save();
								 return PromiseBase.Unit;
							 })
							 .Error(err =>
							   {
								   Debug.LogError($"Failed to refresh account for {Token.RefreshToken} for uri=[{uri}] method=[{method}] includeAuth=[{includeAuthHeader}]");
								   Debug.LogException(err);
							   })
							 .FlatMap(_ => MakeRequest(method, uri, contentType, body, includeAuthHeader, parser));

					   }

					   return Promise<T>.Failed(error);
					   //The uri + Token.RefreshToken.ToString() wont work properly for anything with a body in the request
				   }).Then(_response =>
				   {
					   if (useCache && Token != null && Application.isPlaying)
					   {
						   OfflineCache.Set<T>(uri, _response, Token, includeAuthHeader);
					   }
				   });
			}
			else if (!internetConnectivity && useCache && Application.isPlaying)
			{
				return OfflineCache.Get<T>(uri, Token, includeAuthHeader);
			}
			else
			{
				return Promise<T>.Failed(new NoConnectivityException(uri + " should not be cached and requires internet connectivity."));
			}
		}

		private Promise<T> MakeRequest<T>(
		   Method method,
		   string uri,
		   string contentType,
		   byte[] body,
		   bool includeAuthHeader,
		   Func<string, T> parser = null)
		{
			var result = new Promise<T>();
			var request = BuildWebRequest(method, uri, contentType, body, includeAuthHeader);
			var op = request.SendWebRequest();
			op.completed += _ => HandleResponse<T>(result, request, parser);
			return result;
		}

		private UnityWebRequest BuildWebRequest(Method method, string uri, string contentType, byte[] body,
		   bool includeAuthHeader)
		{
			PlatformLogger.Log($"PLATFORM REQUEST: {Host}{uri}");

			// Prepare the request
			UnityWebRequest request = BuildWebRequest(method, uri, contentType, body);
			request.SetRequestHeader("Accept", ACCEPT_HEADER);
			if (!string.IsNullOrEmpty(Cid))
			{
				request.SetRequestHeader("X-KS-CLIENTID", Cid);
			}

			if (!string.IsNullOrEmpty(Pid))
			{
				request.SetRequestHeader("X-KS-PROJECTID", Pid);
			}

			if (includeAuthHeader)
			{
				var authHeader = GenerateAuthorizationHeader();
				if (authHeader != null)
				{
					request.SetRequestHeader("Authorization", authHeader);
				}
			}

			if (Shard != null)
			{
				request.SetRequestHeader("X-KS-SHARD", Shard);
			}

			if (TimeOverride != null)
			{
				request.SetRequestHeader("X-KS-TIME", TimeOverride);
			}

			if (Language != null)
			{
				request.SetRequestHeader("Accept-Language", Language);
			}

			if (RequestTimeoutMs != null)
			{
				request.SetRequestHeader("X-KS-TIMEOUT", RequestTimeoutMs);
			}

			return request;
		}

		private void HandleResponse<T>(Promise<T> promise, UnityWebRequest request, Func<string, T> parser = null)
		{
			// swallow any responses if already disposed
			if (_disposed)
			{
				PlatformLogger.Log("PLATFORM REQUESTER: Disposed, Ignoring Response");
				return;
			}

			try
			{
				var responsePayload = request.downloadHandler.text;

				if (request.responseCode >= 300 || request.IsNetworkError())
				{
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
					PlatformLogger.Log($"PLATFORM RESPONSE: {responsePayload}");

					try
					{
						T result = parser == null ? JsonUtility.FromJson<T>(responsePayload) : parser(responsePayload);
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

		private string GenerateAuthorizationHeader()
		{
			return Token != null ? $"Bearer {Token.Token}" : null;
		}
	}

	[Serializable]
	public class PlatformError
	{
		public long status;
		public string service;
		public string error;
		public string message;
	}

	public class PlatformRequesterException : RequesterException
	{
		public PlatformError Error { get; }
		public UnityWebRequest Request { get; }
		public PlatformRequesterException(PlatformError error, UnityWebRequest request, string responsePayload)
		: base("HTTP Error", request.method, request.url, request.responseCode, responsePayload)
		{
			Error = error;
			Request = request;
		}
	}


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
