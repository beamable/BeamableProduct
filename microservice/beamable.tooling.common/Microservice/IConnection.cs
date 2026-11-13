using System.Net.WebSockets;
using System.Diagnostics;
using System.Text.Json;


namespace Beamable.Server
{
	public interface IConnectionProvider
	{
		IConnection Create(string host, IMicroserviceArgs args);
	}

	public interface IConnection

	{

		IConnection Connect();

		Task Close();

		Task SendMessage(string message, Stopwatch sw = null);

		Task SendBinaryMessage(byte[] data, Stopwatch sw = null);

		/// <summary>
		/// Like <see cref="SendMessage(string, Stopwatch)"/>, but the message is skipped, and the task cancelled, when
		/// <paramref name="skipIfCancelled"/> is cancelled before the message starts to be written. Once its first frame
		/// is written, the message is sent in full.
		/// </summary>
		Task SendMessage(string message, Stopwatch sw, CancellationToken skipIfCancelled) =>
			skipIfCancelled.IsCancellationRequested ? Task.FromCanceled(skipIfCancelled) : SendMessage(message, sw);

		/// <summary>
		/// Like <see cref="SendBinaryMessage(byte[], Stopwatch)"/>, but skipped when <paramref name="skipIfCancelled"/> is
		/// cancelled before the message starts to be written.
		/// </summary>
		Task SendBinaryMessage(byte[] data, Stopwatch sw, CancellationToken skipIfCancelled) =>
			skipIfCancelled.IsCancellationRequested ? Task.FromCanceled(skipIfCancelled) : SendBinaryMessage(data, sw);

		IConnection OnConnect(Action<IConnection> onConnect);

		IConnection OnDisconnect(Action<IConnection, bool> onDisconnect);

		IConnection OnMessage(Action<IConnection, JsonDocument, long> onMessage);
		IConnection OnMessage(Action<IConnection, JsonDocument, long, Stopwatch> onMessage);

		WebSocketState State { get; }

		/// <summary>
		/// Tears the connection down at once, without a close handshake, because it is presumed broken. The disconnect
		/// callbacks run, so the owner can reconnect.
		/// </summary>
		void Abort(string reason) { }
	}

}
