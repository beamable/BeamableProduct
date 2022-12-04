#if DB_MICROSERVICE

using System;

using System.Net.WebSockets;

using System.Text;

using System.Threading;

using System.Threading.Tasks;
using Beamable.Common;
using Serilog;
using System.Diagnostics;
using System.Threading.RateLimiting;


namespace Beamable.Server

{

    public class EasyWebSocketProvider : IConnectionProvider

    {

        public IConnection Create(string host, IMicroserviceArgs args)

        {

            var ws = EasyWebSocket.Create(host, args);

            return ws;

        }
    }


    public class EasyWebSocket : IConnection

    {
	    private readonly IMicroserviceArgs _args;

	    private const int ReceiveChunkSize = 1024;

        private const int SendChunkSize = 1024;



        private readonly ClientWebSocket _ws;

        private readonly Uri _uri;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly CancellationToken _cancellationToken;



        private Action<EasyWebSocket> _onConnected;

        private Action<EasyWebSocket, string, long, Stopwatch> _onMessage;

        private Action<EasyWebSocket, bool> _onDisconnected;



        private long messageNumber = 0;


        public WebSocketState State => _ws.State;


        protected EasyWebSocket(string uri, IMicroserviceArgs args)

        {
	        _args = args;

	        _ws = new ClientWebSocket();



            _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
            _uri = new Uri(uri);

            _cancellationToken = _cancellationTokenSource.Token;

        }



        /// <summary>

        /// Creates a new instance.

        /// </summary>

        /// <param name="uri">The URI of the WebSocket server.</param>

        /// <returns></returns>

        public static EasyWebSocket Create(string uri, IMicroserviceArgs args)

        {

            return new EasyWebSocket(uri, args);

        }



        /// <summary>

        /// Connects to the WebSocket server.

        /// </summary>

        /// <returns></returns>

        public IConnection Connect()

        {
            var connectAsync = ConnectAsync();
            connectAsync.Wait(_cancellationToken);
            return this;

        }


        /// <summary>

        /// Set the Action to call when the connection has been established.

        /// </summary>

        /// <param name="onConnect">The Action to call.</param>

        /// <returns></returns>

        public IConnection OnConnect(Action<IConnection> onConnect)

        {

            _onConnected += onConnect;

            return this;

        }



        /// <summary>

        /// Set the Action to call when the connection has been terminated.

        /// </summary>

        /// <param name="onDisconnect">The Action to call</param>

        /// <returns></returns>

        public IConnection OnDisconnect(Action<IConnection, bool> onDisconnect)

        {

            _onDisconnected += onDisconnect;

            return this;

        }



        public IConnection OnMessage(Action<IConnection, string, long> onMessage) =>
	        OnMessage((c, msg, id, _) => onMessage(c, msg, id));
        
        /// <summary>
        /// Set the Action to call when a messages has been received.
        /// </summary>
        /// <param name="onMessage">The Action to call.</param>
        /// <returns></returns>
        public IConnection OnMessage(Action<IConnection, string, long, Stopwatch> onMessage)

        {

            _onMessage = onMessage;

            return this;

        }



        /// <summary>

        /// Send a message to the WebSocket server.

        /// </summary>

        /// <param name="message">The message to send</param>

        public Task SendMessage(string message, Stopwatch sw=null)
        {
            return SendMessageAsync(message, sw);
        }



        /// <summary>

        /// Terminate the socket in a friendly way.

        /// </summary>

        public async Task Close()

        {

            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "shutting down", CancellationToken.None);

        }



        private async Task SendMessageAsync(string message, Stopwatch sw)
        {
            if (_ws.State != WebSocketState.Open)
            {
                throw new Exception($"Connection is not open. state=[{_ws.State}]");
            }


            var messageBuffer = Encoding.UTF8.GetBytes(message);

            var messagesCount = (int)Math.Ceiling((double)messageBuffer.Length / SendChunkSize);



            for (var i = 0; i < messagesCount; i++)

            {

                var offset = (SendChunkSize * i);

                var count = SendChunkSize;

                var lastMessage = ((i + 1) == messagesCount);



                if ((count * (i + 1)) > messageBuffer.Length)

                {

                    count = messageBuffer.Length - offset;

                }



                await _ws.SendAsync(new ArraySegment<byte>(messageBuffer, offset, count), WebSocketMessageType.Text, lastMessage, _cancellationToken);

            }
            
            if (sw != null)
            {
	            sw.Stop();
	            Log.Verbose($"client message time=[{sw.ElapsedMilliseconds}]");
            }

        }



        private async Task ConnectAsync()

        {

            try

            {

                await _ws.ConnectAsync(_uri, _cancellationToken);

                CallOnConnected();

            }

            catch (Exception)

            {

                CallOnDisconnected(false);

            }



            StartListen();

        }



        private async void StartListen()
        {

            var buffer = new byte[ReceiveChunkSize];
            
            var tokenLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
	            QueueLimit = _args.RateLimitWebsocketMaxQueueSize,
	            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
	            TokenLimit = _args.RateLimitWebsocketTokens,
	            TokensPerPeriod = _args.RateLimitWebsocketTokensPerPeriod,
	            AutoReplenishment = true,
	            ReplenishmentPeriod = TimeSpan.FromSeconds(_args.RateLimitWebsocketPeriodSeconds)
            });
            
            try
            {
	            var readSegment = new ArraySegment<byte>(buffer);
	            var stringResult = new StringBuilder();
	            while (_ws.State == WebSocketState.Open)
	            {
		            if (_args.RateLimitWebsocket)
		            {
			            await tokenLimiter.AcquireAsync();
		            }
		            
		            WebSocketReceiveResult result;
		            var sw = new Stopwatch();
		            
		            sw.Start();
		            do
		            {
			            result = await _ws.ReceiveAsync(readSegment, _cancellationToken);
			            if (result.MessageType == WebSocketMessageType.Close)
			            {
				            await  _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty,
						            CancellationToken.None);
				            CallOnDisconnected(true);
			            }
			            else
			            {
				            var str = Encoding.UTF8.GetString(buffer, 0, result.Count);
				            stringResult.Append(str);
			            }
		            } while (!result.EndOfMessage);

		            CallOnMessage(stringResult, sw);
		            stringResult.Clear();
	            }

            }

            catch (Exception)

            {

                CallOnDisconnected(false);

            }

            finally

            {

                _ws.Dispose();

                // attempt to reconnect....

            }

        }
        
        private void CallOnMessage(StringBuilder stringResult, Stopwatch sw)
        {
	        if (_onMessage != null)
            {
	            var next = Interlocked.Increment(ref messageNumber);
                var text = stringResult.ToString();
                RunInTask(() =>
                {
	                _onMessage(this, text, next, sw);
                });
            }
        }



        private void CallOnDisconnected(bool wasClean)

        {

            if (_onDisconnected != null)

                RunInTask(() => _onDisconnected(this, wasClean));

        }



        private void CallOnConnected()

        {

            if (_onConnected != null)

                RunInTask(() => _onConnected(this));

        }



        private static void RunInTask(Action action)

        {
	        Task.Run(action);
	        // Task.Factory.StartNew(action, );

        }



    }

}

#endif

