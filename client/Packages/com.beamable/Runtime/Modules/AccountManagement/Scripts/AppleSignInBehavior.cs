using Beamable.Api;
using Beamable.Common.Api.Auth;
using Beamable.Platform.SDK;
using Beamable.Platform.SDK.Auth;
using Beamable.Spew;
using UnityEngine;

namespace Beamable.AccountManagement
{
   public class AppleSignInBehavior : MonoBehaviour
   {
      #if UNITY_IOS

      private SignInWithApple signInWithApple;

      public void Start()
      {
         signInWithApple = new SignInWithApple();
      }

      public void StartAppleLogin(ThirdPartyLoginPromise promise)
      {
         if (promise.ThirdParty != AuthThirdParty.Apple)
         {
            return;
         }

         // TODO: Need to do something graceful in the event that the device in question doesn't have iOS13.
         // Also need to ensure that the button doesn't show up if the user isn't on iOS13+

         signInWithApple.Login(callbackArgs =>
         {
            if (!string.IsNullOrEmpty(callbackArgs.error))
            {
               promise.CompleteError(new ErrorCode(1, error: "UnableToSignInToApple", message: callbackArgs.error));
            }
            else
            {
               promise.CompleteSuccess(new ThirdPartyLoginResponse
               {
                  AuthToken = callbackArgs.userInfo.idToken
               });
            }
         });
      }
      #else
      public void StartAppleLogin(ThirdPartyLoginPromise promise)
      {
         // we aren't on apple, so don't do _anything_
      }
      #endif
   }
}
