using Beamable.Server;

namespace Beamable.Server.NewMicroServiceddaasdad
{
   [Microservice("NewMicroServiceddaasdad")]
   public class NewMicroServiceddaasdad : Microservice
   {
      [ClientCallable]
      public void ServerCall()
      {
         // This code executes on the server.
      }
   }
}