using System;
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
      void SendMessage(string message);
      IConnection OnConnect(Action<IConnection> onConnect);
      IConnection OnDisconnect(Action<IConnection, bool> onDisconnect);
      IConnection OnMessage(Action<IConnection, string, long> onMessage);
   }
}