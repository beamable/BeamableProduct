using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Spew;
using Beamable.Coroutines;
using NativeWebSocket;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Connection
{
	public class WebSocketConnection : IBeamableConnection
	{
		public event Action Open;
		public event Action<string> Message;
		public event Action<string> Error;
		public event Action Close;
		
		
		private WebSocket _webSocket;
		private readonly CoroutineService _coroutineService;
		private IEnumerator _dispatchMessagesRoutine;

		public WebSocketConnection(CoroutineService coroutineService)
		{
			_coroutineService = coroutineService;
		}

		public Promise<Unit> Connect(string address, AccessToken token)
		{
			_webSocket = CreateWebSocket(address, token);
			SetUpEventCallbacks();
			// This is a bit gross but the underlying library doesn't complete the connect task until the connection
			// is closed.
			var p = new Promise<Unit>();
			p.CompleteSuccess(PromiseBase.Unit);
			_webSocket
				.Connect()
				.ContinueWith(_ => p.CompleteSuccess(PromiseBase.Unit));
#if !UNITY_WEBGL || UNITY_EDITOR
			_dispatchMessagesRoutine = DispatchMessages();
			_coroutineService.StartCoroutine(_dispatchMessagesRoutine);
#endif
			return p;
		}

		public Promise<Unit> Disconnect()
		{
#if !UNITY_WEBGL || UNITY_EDITOR
			if (_dispatchMessagesRoutine != null)
			{
				_coroutineService.StopCoroutine(_dispatchMessagesRoutine);
			}
#endif
			var p = new Promise<Unit>();
			_webSocket
				.Close()
				.ContinueWith(_ => p.CompleteSuccess(PromiseBase.Unit));
			return p;
		}

		private static WebSocket CreateWebSocket(string address, IAccessToken token)
		{
			var subprotocols = new List<string>();
			var headers = new Dictionary<string, string>
			{
				{"Authorization", $"Bearer {token.Token}"}
			};
			return new WebSocket(address, subprotocols, headers);
		}

		private void SetUpEventCallbacks()
		{
			_webSocket.OnOpen += () =>
			{
				PlatformLogger.Log($"<b>[WebSocketConnection]</b> OnOpen received");
				Open?.Invoke();
			};
			_webSocket.OnMessage += message =>
			{
				string messageString = Encoding.UTF8.GetString(message);
				PlatformLogger.Log($"<b>[WebSocketConnection]</b> OnMessage received: {messageString}");
				Message?.Invoke(messageString);
			};
			_webSocket.OnError += e =>
			{
				PlatformLogger.Log($"<b>[WebSocketConnection]</b> OnError received: {e}");
				Error?.Invoke(e);
			};
			_webSocket.OnClose += code =>
			{
				PlatformLogger.Log($"<b>[WebSocketConnection]</b> OnClose received: {code}");
				Close?.Invoke();
			};
		}

		private IEnumerator DispatchMessages()
		{
			const float dispatchIntervalMs = 50.0f;
			var wait = new WaitForSeconds(dispatchIntervalMs / 1000.0f);
			while (true)
			{
				_webSocket.DispatchMessageQueue();
				yield return wait;
			}
		}
	}
}
