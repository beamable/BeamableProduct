using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Spew;
using Beamable.Coroutines;
using Beamable.Endel.NativeWebSocket;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Beamable.Api.Connectivity;
using Beamable.Api.Sessions;
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
		private readonly IBeamableApiRequester _apiRequester;
		private readonly CoroutineService _coroutineService;
		private IEnumerator _dispatchMessagesRoutine;
		private Promise _onConnectPromise;
		private string _connectAddress;

		public WebSocketConnection(IBeamableApiRequester apiRequester, CoroutineService coroutineService)
		{
			_apiRequester = apiRequester;
			_coroutineService = coroutineService;
		}

		public Promise Connect(string address)
		{
			_connectAddress = $"{address}/connect";
			if (_webSocket is not null)
			{
				Reconnect();
			}
			else
			{
				DoConnect();
			}

			return _onConnectPromise;
		}

		public async Promise Disconnect()
		{
			_disconnecting = true;

			await _webSocket.Close();
			_webSocket = null;
#if !UNITY_WEBGL || UNITY_EDITOR
			if (_dispatchMessagesRoutine != null)
			{
				_coroutineService.StopCoroutine(_dispatchMessagesRoutine);
				_dispatchMessagesRoutine = null;
			}
#endif
		}

		private void DoConnect()
		{
			CreateWebSocket();
			_onConnectPromise = new Promise();
			Task _ = _webSocket.Connect();

#if !UNITY_WEBGL || UNITY_EDITOR
			_dispatchMessagesRoutine = DispatchMessages();
			_coroutineService.StartCoroutine(_dispatchMessagesRoutine);
#endif
		}

		private void Reconnect()
		{
			Disconnect().Then(_ => DoConnect());
		}

		private void CreateWebSocket()
		{
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
			 _webSocket.OnError += async error =>
          {
             PlatformLogger.Log($"<b>[WebSocketConnection]</b> OnError received: {error}");
             try
             {
                // No connectivity kills the onConnectPromise, requiring that we call Reconnect
                // if we are "retrying" to connect.  If there's no connectivity, OnOpen never fires
                if (_onConnectPromise is not null && !_onConnectPromise.IsCompleted)
                {
                   _onConnectPromise.CompleteError(new WebSocketConnectionException(error));
                   Error?.Invoke(error);

				   await TryRefreshToken();
				   
                   if (_retrying)
                   {
                      PlatformLogger.Log($"<b>[WebSocketConnection]</b> Retrying connection in {_delay / 1000} seconds");
                       await Task.Delay(_delay);
                      _delay = (_retrying && _delay < MAX_DELAY) ? _delay * 2 : MIN_DELAY;
                      _delay = Mathf.Clamp(_delay, MIN_DELAY, MAX_DELAY);
                      Reconnect();
                   }
                }
               
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
                // When Reconnect is invoked, _disconnecting == true until the socket successfully opens
                // This prevents the retry routine from happening here, because it exits early.
                if (_disconnecting)
                {
                   Close?.Invoke();
                }
                else
                {
                   PlatformLogger.Log($"<b>[WebSocketConnection]</b> Ungraceful close of websocket. Reconnecting!");
                   // Handle exceptions when making RefreshToken request, to ensure that we attempt to Reconnect() after
                   await TryRefreshToken();

                   if (_retrying)
                   {
                      PlatformLogger.Log($"<b>[WebSocketConnection]</b> Retrying connection in {_delay / 1000} seconds");
                      await Task.Delay(_delay);
                   }
                   _retrying = true;
                   // Kick off initial reconnect due to a valid socket being closed
                   Reconnect();
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

		private async Promise TryRefreshToken()
		{
		  try
		  {
			 await _apiRequester.RefreshToken();
		  }
		  catch (NoConnectivityException ex)
		  {
			 PlatformLogger.Log(
				$"<b>[WebSocketConnection]</b> RefreshToken failed due to NoConnectivity. " +
				$"Will still retry websocket reconnect. {ex.Message}");
		  }
		  catch (Exception ex)
		  {
			 PlatformLogger.Log(
				$"<b>[WebSocketConnection]</b> RefreshToken failed due to an exception. " +
				$"Will still retry websocket reconnect. {ex.Message}");
		  }
		}
		public async Promise OnDispose()
		{
			await Disconnect();
		}

		bool IHeartbeatService.IsRunning
		{
			get
			{
				// the websocket is always "running", in that it is always
				//  either connected, or trying to connect. 
				return true;
			}
		}

		void IHeartbeatService.Start()
		{
			// no-op.
			//  the websocket doesn't start/stop. 
			//  it is in a constant state of connection or reconnection. 
		}
		void IHeartbeatService.ResetLegacyInterval()
		{
			// no-op
			//  the websocket does not need to change its interval,
			//  because its connection is defined by the connectivity of the
			//  socket itself.
		}
		void IHeartbeatService.UpdateLegacyInterval(int seconds)
		{
			// no-op
			//  save as ResetLegacyInterval.
		}
	}
}


