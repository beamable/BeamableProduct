using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using Beamable.Common.Api;
using Beamable.Common.Api.Stats;
using Beamable.Serialization.SmallerJSON;

namespace Beamable.Common.Player
{

   public enum StatAccess
   {
      Public, Private
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

   public interface IPlayerStats : IObservable<Dictionary<string, object>>
   {
      StatAccess Access { get; }
      Promise Set(string key, string value);

      object this[string key] { get; }
   }



}