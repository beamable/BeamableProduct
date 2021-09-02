using Beamable.Common;
using Beamable.Common.Api.Commerce;
using System.Collections.Generic;

namespace Beamable.Server.Api.Commerce
{
   public interface IMicroserviceCommerceApi : ICommerceApi
   {
      Promise<Unit> AccelerateListingCooldown(long gamerTag, List<CooldownReductionRequest> cooldownReductions);
   }

   [System.Serializable]
   public class UpdateListingCooldownRequest
   {
      public long gamerTag;
      public List<CooldownReductionRequest> updateListingCooldownRequests;

      public UpdateListingCooldownRequest(long gamerTag, List<CooldownReductionRequest> updateListingCooldownRequests)
      {
         this.gamerTag = gamerTag;
         this.updateListingCooldownRequests = updateListingCooldownRequests;
      }
   }

   [System.Serializable]

   public class CooldownReductionRequest
   {
      public string symbol;
      public int cooldownReduction;

      public CooldownReductionRequest(string symbol, int cooldownReduction)
      {
         this.symbol = symbol;
         this.cooldownReduction = cooldownReduction;
      }
   }

}
