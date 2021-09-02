using System;

namespace Beamable.Common.Api
{
   public interface IAccessToken
   {
      string Token { get; }
      string RefreshToken { get; }
      DateTime ExpiresAt { get; }
      string Cid { get; }
      string Pid { get; }
   }
}