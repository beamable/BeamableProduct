using System;
using System.Globalization;
using Beamable.Common;
using Beamable.Common.Api;

namespace Beamable.Api
{
   /// <summary>
   /// This type defines the %Client main entry point for the %AccessToken feature.
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
   public class AccessToken : IAccessToken
   {
      private AccessTokenStorage _storage;
      public string Token { get; private set; }
      public string RefreshToken { get; }
      public DateTime ExpiresAt { get; set; }
      public string Cid { get; }
      public string Pid { get; }

      //Consider the token expired if we're within 1 Day of true expiration
      //This is to avoid the token expiring during a play session
      public bool IsExpired => DateTime.UtcNow.AddDays(1) > ExpiresAt;

      public AccessToken(AccessTokenStorage storage, string cid, string pid, string token, string refreshToken, long expiresAt)
      {
         _storage = storage;
         Cid = cid;
         Pid = pid;
         Token = token;
         RefreshToken = refreshToken;
         ExpiresAt = DateTime.UtcNow.AddMilliseconds(expiresAt);
      }

      public AccessToken(AccessTokenStorage storage, string cid, string pid, string token, string refreshToken, string expiresAtISO)
      {
         _storage = storage;
         Cid = cid;
         Pid = pid;
         Token = token;
         RefreshToken = refreshToken;
         ExpiresAt = DateTime.Parse(expiresAtISO, CultureInfo.InvariantCulture);
      }

      // Saves to disk
      public Promise<Unit> Save()
      {

         return _storage.SaveTokenForRealm(
            Cid,
            Pid,
            this
         );
      }
      public Promise<Unit> SaveAsCustomerScoped()
      {
         return _storage.SaveTokenForCustomer(Cid, this);
      }

      // Deletes from disk
      public Promise<Unit> Delete()
      {
         return _storage.DeleteTokenForRealm(Cid, Pid);
      }

      public Promise<Unit> DeleteAsCustomerScoped()
      {
         return _storage.DeleteTokenForCustomer(Cid);
      }

      internal void CorruptAccessToken()
      {
         // Set as a garbage (but plausible) token
         Token = "ffffffff-ffff-ffff-ffff-ffffffffffff";
         Save();
      }

      internal void ExpireAccessToken()
      {
         ExpiresAt = DateTime.UtcNow.AddDays(-2);
         Save();
      }
   }
}