using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Stats;
using Beamable.Common.Player;
using Beamable.Serialization.SmallerJSON;

namespace Beamable.Player
{

   public class PlayerStats : UserObservable<Dictionary<string, object>>, IPlayerStats
   {
      private readonly IStatsApi _api;
      public StatAccess Access { get; }

      protected string ObjectId => $"client.{Access.ToNetworkString()}.player.{UserId}";

      protected string ObjectRoute => $"/object/stats/{ObjectId}";

      public PlayerStats(StatAccess access, long userId, IStatsApi api, IBeamableRequester requester) : base(userId, requester)
      {
         _api = api;
         Access = access;
      }

      protected override async Promise<Dictionary<string, object>> PerformFetch()
      {
         var statKeys = string.Join(",", Data.Keys);
         var result = await Requester.Request<GetStatsResponse>(
            Method.GET,
            $"{ObjectRoute}/client?stats={statKeys}",
            useCache: true,
            parser: (json) =>
            {
               var statsDict = new Dictionary<string, object>();

               if (Json.Deserialize(json) is ArrayDict dict)
               {
                  var stats = dict[nameof(GetStatsResponse.stats)] as ArrayDict;
                  foreach (var kvp in stats)
                  {
                     statsDict[kvp.Key] = kvp.Value;
                  }
               }

               return new GetStatsResponse
               {
                  stats = statsDict
               };
            }
         );

         return result.stats;
      }

      [Serializable]
      private class GetStatsResponse
      {
         public Dictionary<string, object> stats;
      }

      public object this[string key]
      {
         get
         {
            if (Data.TryGetValue(key, out var existing))
            {
               return existing;
            }

            Data[key] = null;
            PushRefresh();
            return null;
         }
      }

      public Promise Set(string key, string value)
      {
         // optimistically set the stat value.
         Data[key] = value;

         // then enqueue the action
         return PushAction(new SetStatAction(key, value));
      }

      public class SetStatAction : UserSdkAction
      {
         public string Key { get; }
         public string Value { get; }

         public SetStatAction(string key, string value)
         {
            Key = key;
            Value = value;
         }

         public override Promise Execute()
         {
            throw new NotImplementedException(
               "This shouldn't be possible, because it should always get picked up by middleware.");
         }
      }

      public class BatchSetStatAction : UserSdkAction
      {
         private readonly IStatsApi _api;
         public IEnumerable<SetStatAction> SetActions { get; }

         public BatchSetStatAction(IStatsApi api, IEnumerable<SetStatAction> setActions)
         {
            _api = api;
            SetActions = setActions;
         }
         public override Promise Execute()
         {
            var stats = SetActions.ToDictionary(s => s.Key, s => s.Value);
            return _api
               .SetStats("public", stats)
               .ToPromise();
         }
      }

      public override ISdkAction GetNextAction(IEnumerable<ISdkAction> set)
      {
         var list = set.ToList();

         var setActions = list
            .Where(action => action is SetStatAction)
            .Cast<SetStatAction>()
            .ToList();
         foreach (var toRemove in setActions)
         {
            list.Remove(toRemove);
         }

         if (setActions.Count > 0)
         {
            var batchAction = new BatchSetStatAction(_api, setActions);
            ConfigureAction(batchAction);
            list.Add(batchAction);
         }

         return list.Last();
      }
   }


}