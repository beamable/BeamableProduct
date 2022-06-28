using Beamable.Common;
using System;
using System.Net.WebSockets;
using System.Threading.Tasks;


namespace Beamable.Server
{
	public interface IConnectionProvider
	{
		IConnection Create(string host);
	}

	public interface IConnection

	{

		IConnection Connect();

		Task Close();

		Promise SendMessage(string message);

		IConnection OnConnect(Action<IConnection> onConnect);

		IConnection OnDisconnect(Action<IConnection, bool> onDisconnect);

		IConnection OnMessage(Action<IConnection, string, long> onMessage);

		WebSocketState State { get; }
	}

}
