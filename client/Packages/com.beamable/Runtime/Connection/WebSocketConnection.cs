using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Spew;
using Beamable.Coroutines;
using Beamable.Endel.NativeWebSocket;
using Core.Platform.SDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Beamable.Connection
{
	public class WebSocketConnection : IBeamableConnection
	{
		public event Action Open;
		public event Action<string> Message;
		public event Action<string> Error;
		public event Action Close;

		private const int MIN_DELAY = 1000;
		private const int MAX_DELAY = 60000;

		private bool _disconnecting;
		private bool _retrying;
		private int _delay;
		private WebSocket _webSocket;
		private readonly CoroutineService _coroutineService;
		private IEnumerator _dispatchMessagesRoutine;
		private Promise _onConnectPromise;
		private IBeamableApiRequester _apiRequester;
		private string _connectAddress;

		public WebSocketConnection(CoroutineService coroutineService)
		{
			_coroutineService = coroutineService;
		}

		public Promise Connect(string address, IBeamableApiRequester requester)
		{
			_connectAddress = $"{address}/connect";
			_apiRequester = requester;
			// This is a bit gross but the underlying library doesn't complete the connect task until the connection
			// is closed.
			DoConnect();

#if !UNITY_WEBGL || UNITY_EDITOR
			_dispatchMessagesRoutine = DispatchMessages();
			_coroutineService.StartCoroutine(_dispatchMessagesRoutine);
#endif
			return _onConnectPromise;
		}

		public async Promise Disconnect()
		{
			_disconnecting = true;

			await _webSocket.Close();
#if !UNITY_WEBGL || UNITY_EDITOR
			if (_dispatchMessagesRoutine != null)
			{
				_coroutineService.StopCoroutine(_dispatchMessagesRoutine);
			}
#endif
		}

		private void DoConnect()
		{
			CreateWebSocket();
			_onConnectPromise = new Promise();
			Task _ = _webSocket.Connect();
		}

		private void CreateWebSocket()
		{
			_ = _webSocket?.Close();

			string token = _apiRequester.Token.Token;
			string address = _connectAddress;
			var subprotocols = new List<string>();
#if !UNITY_WEBGL
			var headers = new Dictionary<string, string>
			{
				{"Authorization", $"Bearer {token}"}
			};
			_webSocket = new WebSocket(address, subprotocols, headers, _coroutineService);
#else
			if(!string.IsNullOrWhiteSpace(token))
			{
				address += "?access_token=" + token;
			}
			_webSocket = new WebSocket(address, subprotocols, null, _coroutineService);
#endif
			SetUpEventCallbacks();
		}

		private void SetUpEventCallbacks()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.playModeStateChanged += state =>
			{
				if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
				{
					_ = Disconnect();
				}
			};
#endif
			_webSocket.OnOpen += () =>
			{
				_disconnecting = false;
				PlatformLogger.Log($"<b>[WebSocketConnection]</b> OnOpen received");
				try
				{
					_onConnectPromise.CompleteSuccess();
					_retrying = false;
					Open?.Invoke();
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			};
			_webSocket.OnMessage += message =>
			{
				string messageString = Encoding.UTF8.GetString(message);
				PlatformLogger.Log($"<b>[WebSocketConnection]</b> OnMessage received: {messageString}");
				try
				{
					Message?.Invoke(messageString);
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			};
			_webSocket.OnError += error =>
			{
				PlatformLogger.Log($"<b>[WebSocketConnection]</b> OnError received: {error}");
				try
				{
					_onConnectPromise.CompleteError(new WebSocketConnectionException(error));
					Error?.Invoke(error);
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			};
			_webSocket.OnClose += async code =>
			{
				PlatformLogger.Log($"<b>[WebSocketConnection]</b> OnClose received: {code}");
				try
				{
					if (_disconnecting)
					{
						Close?.Invoke();
					}
					else
					{
						PlatformLogger.Log($"<b>[WebSocketConnection]</b> Ungraceful close of websocket. Reconnecting!");

						// Let's make sure that we get a fresh new JWT before attempting to reconnect.
						await _apiRequester.RefreshToken();

						if (_retrying)
						{
							PlatformLogger.Log($"<b>[WebSocketConnection]</b> Retrying connection in {_delay / 1000} seconds");
							await Task.Delay(_delay);
						}
						_delay = (_retrying && _delay < MAX_DELAY) ? _delay * 2 : MIN_DELAY; // always doubling the delay in case of an error
						_delay = Mathf.Clamp(_delay, MIN_DELAY, MAX_DELAY);
						_retrying = true;

						DoConnect();
					}

				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			};
		}

#if !UNITY_WEBGL || UNITY_EDITOR
		private IEnumerator DispatchMessages()
		{
			const float dispatchIntervalMs = 50.0f;
			var wait = new WaitForSeconds(dispatchIntervalMs / 1000.0f);
			while (true)
			{
				_webSocket.DispatchMessageQueue();
				yield return wait;
			}
			// ReSharper disable once IteratorNeverReturns
		}
#endif

		public async Promise OnDispose()
		{
			await Disconnect();
		}
	}
}
