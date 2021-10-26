using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using Beamable.Common.Api;
using Beamable.Common.Api.Stats;

namespace Beamable.Common.Player
{

   public enum StatAccess
   {
      Public, Private
   }

   public enum StatDomain
   {
      Game, Client
   }


   public static class StatAccessExtensions
   {
      public static string ToNetworkString(this StatAccess access)
      {
         switch (access)
         {
            case StatAccess.Private: return "private";
            case StatAccess.Public: return "public";
         }

         return access.ToString().ToLower();
      }
   }

   public interface IPlayerStats : IObservable<Dictionary<string, string>>
   {
      StatAccess Access { get; }
      StatDomain Domain { get; }
      Promise Set(string key, string value);

      string this[string key] { get; }
   }

   public class PlayerStats : DefaultObservable<Dictionary<string, string>>, IPlayerStats, IActionStackMiddleware
   {
      public StatAccess Access { get; }
      public StatDomain Domain { get; }


      public PlayerStats(StatAccess access, IBeamableRequester requester) : base(requester)
      {
         Access = access;

         _actionStack.RegisterMiddleware(this);
      }

      protected override Promise<Dictionary<string, string>> PerformFetch()
      {
         string queryString = "";
         for (int i = 0; i < gamerTags.Count; i++)
         {
            if (i > 0)
            {
               queryString += ",";
            }
            queryString += $"{prefix}{gamerTags[i]}";
         }

         return Requester.Request<BatchReadStatsResponse>(
            Method.GET,
            $"/basic/stats/client/batch?format=stringlist&objectIds={queryString}",
            useCache: true
         );
      }

      public string this[string key] => Data[key] ?? (Data[key] = null);

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
         public override Promise Execute()
         {

         }
      }

      public ISdkAction OnPush(ISdkAction newAction)
      {
         // there is already an existing
         return newAction;
      }

      public ISdkAction OnPop(ISdkAction poppedAction)
      {
         return poppedAction;
      }
   }



}