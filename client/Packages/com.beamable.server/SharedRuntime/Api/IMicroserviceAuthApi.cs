using Beamable.Common;
using Beamable.Common.Api.Auth;

namespace Beamable.Server.Api
{
   /// <summary>
   /// This type defines the %Microservice main entry point for the %Auth feature.
   /// 
   /// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
   /// 
   /// #### Related Links
   /// - See the <a target="_blank" href="https://docs.beamable.com/docs/accounts-feature">Accounts</a> feature documentation
   /// - See Beamable.Server.IBeamableServices script reference
   /// 
   /// ![img beamable-logo]
   /// 
   /// </summary>
   public interface IMicroserviceAuthApi : IAuthApi
   {
      Promise<User> GetUser(long userId);
   }
}