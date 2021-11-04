using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Common.Api.Stats
{
   public abstract class AbsStatsApi : IStatsApi
   {
      private readonly UserDataCache<Dictionary<string, string>>.FactoryFunction _cacheFactory;
      public IBeamableRequester Requester { get; }
      public IUserContext UserContext { get; }
      private static long TTL_MS = 15 * 60 * 1000;
      private Dictionary<string, UserDataCache<Dictionary<string, string>>> caches = new Dictionary<string, UserDataCache<Dictionary<string, string>>>();

      public AbsStatsApi(IBeamableRequester requester, IUserContext userContext, UserDataCache<Dictionary<string, string>>.FactoryFunction cacheFactory)
      {
         _cacheFactory = cacheFactory;
         Requester = requester;
         UserContext = userContext;
      }

      public UserDataCache<Dictionary<string, string>> GetCache(string prefix)
      {
         if (!caches.TryGetValue(prefix, out var cache))
         {
            cache = _cacheFactory(
               $"Stats.{prefix}",
               TTL_MS,
               (gamerTags => Resolve(prefix, gamerTags))
            );
            caches.Add(prefix, cache);
         }

         return cache;
      }

      public Promise<EmptyResponse> SetStats(string access, Dictionary<string, string> stats) {
         long gamerTag = UserContext.UserId;
         string prefix = $"client.{access}.player.";
         return Requester.Request<EmptyResponse>(
            Method.POST,
            $"/object/stats/{prefix}{gamerTag}/client/stringlist",
            new StatUpdates(stats)
         ).Then(_ => GetCache(prefix).Remove(gamerTag));
      }

      public Promise<Dictionary<string, string>> GetStats(string domain, string access, string type, long id)
      {
         string prefix = $"{domain}.{access}.{type}.";
         return GetCache(prefix).Get(id);
      }
      public Promise<List<int>> SearchStats(string domain, string access, string type, List<Criteria> criteria)
      {
          void IsValid(out string error)
          {
              error = string.Empty;
              var tmpError = string.Empty;
              
              if (string.IsNullOrWhiteSpace(domain))
              {
                  tmpError += "> domain cannot be an empty string\n";
              }
              if (string.IsNullOrWhiteSpace(access))
              {
                  tmpError += "> access cannot be an empty string\n";
              }
              if (string.IsNullOrWhiteSpace(type))
              {
                  tmpError += "> type cannot be an empty string\n";
              }
              if (criteria == null)
              {
                  tmpError += "> criteria cannot be null\n";
              }
              else if (criteria.Count == 0)
              {
                  tmpError += "> should be at least one criteria\n";
              }

              if (!string.IsNullOrWhiteSpace(tmpError))
              {
                  error += "Error occured in \"SearchStats\". Check for more details:\n\n";
              }
              error += tmpError;

          }
          
          var searchStatsPromise = new Promise<List<int>>();

          IsValid(out var errorMessage);
          if (!string.IsNullOrWhiteSpace(errorMessage))
          {
              Debug.LogError(errorMessage);
              searchStatsPromise.CompleteError(null);
              return searchStatsPromise;
          }

          var payload = new SearchStats(domain, access, type, criteria);
          var webRequest = Requester.Request<List<int>>(
                  Method.POST, 
                  "/basic/stats/search", 
                  JsonUtility.ToJson(payload));
          
          webRequest.Error(error =>
          {
              Debug.LogError(error);
              searchStatsPromise.CompleteError(error);
          }).Then(result =>
          {
              searchStatsPromise.CompleteSuccess(result);
          });
          
          return searchStatsPromise;
      }
      
      protected abstract Promise<Dictionary<long, Dictionary<string, string>>> Resolve(string prefix,
         List<long> gamerTags);
   }

   [Serializable]
   public class SearchStats
   {
       public string domain;
       public string access;
       public string objectType;
       public List<Criteria> criteria;

       public SearchStats(string domain, string access, string objectType, List<Criteria> criteria)
       {
           this.domain = domain;
           this.access = access;
           this.objectType = objectType;
           this.criteria = criteria;
       }
   }
   
   [Serializable]
   public class Criteria
   {
       public string stat;
       public string rel;
       public string value;

       public Criteria(string stat, string rel, string value)
       {
           this.stat = stat;
           this.rel = rel;
           this.value = value;
       }
   }
}