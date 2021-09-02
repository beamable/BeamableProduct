using System;
using System.Threading;
using Beamable.Server;

namespace mockservice
{
   public class Program
   {
      public static void Main(string[] args)
      {
         Console.WriteLine("Hello");

         var ws = EasyWebSocket.Create("ws://api.beamable.com/socket");
         ws.OnConnect(_ => Console.WriteLine("Connected!"));
         ws.Connect();

         var cancelSource = new CancellationTokenSource();
         cancelSource.Token.WaitHandle.WaitOne();
      }
   }
}