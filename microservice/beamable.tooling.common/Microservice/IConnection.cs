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

		IConnection OnConnect(Action<IConnection> onConnect);

		IConnection OnDisconnect(Action<IConnection, bool> onDisconnect);

		IConnection OnMessage(Action<IConnection, JsonDocument, long> onMessage);
		IConnection OnMessage(Action<IConnection, JsonDocument, long, Stopwatch> onMessage);

		WebSocketState State { get; }
	}

}
