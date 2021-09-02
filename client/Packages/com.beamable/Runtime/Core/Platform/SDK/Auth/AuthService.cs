using Beamable.Common.Api;
using Beamable.Common.Api.Auth;

namespace Beamable.Api.Auth
{
   /// <summary>
   /// This type defines the %Client main entry point for the %Auth feature.
   ///
   /// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
   ///
   /// #### Related Links
   /// - See the <a target="_blank" href="https://docs.beamable.com/docs/accounts-feature">Accounts</a> feature documentation
   /// - See Beamable.API script reference
   ///
   /// ![img beamable-logo]
   ///
   /// </summary>
   public interface IAuthService : IAuthApi
   {
   }

   /// <summary>
   /// This type defines the %Client main entry point for the %Auth feature.
   ///
   /// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
   ///
   /// #### Related Links
   /// - See the <a target="_blank" href="https://docs.beamable.com/docs/accounts-feature">Accounts</a> feature documentation
   /// - See Beamable.API script reference
   ///
   /// ![img beamable-logo]
   ///
   /// </summary>
   public class AuthService : AuthApi, IAuthService
   {
      public AuthService(IBeamableRequester requester, IAuthSettings settings=null) : base(requester, settings)
      {
      }

   }
}
