using Beamable.Common.Dependencies;
using Beamable.Serialization.SmallerJSON;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Beamable.Common.Api.Stats
{
	public abstract class AbsStatsApi : IStatsApi
	{
		private readonly UserDataCache<Dictionary<string, string>>.FactoryFunction _cacheFactory;
		public IBeamableRequester Requester { get; }
		public IUserContext UserContext { get; }
		protected IDependencyProvider Provider { get; }
		private static long TTL_MS = 15 * 60 * 1000;
		private Dictionary<string, UserDataCache<Dictionary<string, string>>> caches = new Dictionary<string, UserDataCache<Dictionary<string, string>>>();

		public AbsStatsApi(IBeamableRequester requester, IUserContext userContext, IDependencyProvider provider, UserDataCache<Dictionary<string, string>>.FactoryFunction cacheFactory)
		{
			_cacheFactory = cacheFactory;
			Requester = requester;
			UserContext = userContext;
			Provider = provider;
		}

		public void ClearCaches()
		{
			foreach (var kvp in caches)
			{
				kvp.Value.Clear();
			}
			caches.Clear();
		}

		public UserDataCache<Dictionary<string, string>> GetCache(string domain, string access, string type)
		{
			string prefix = $"{domain}.{access}.{type}.";
			return GetCache(prefix);
		}

		public UserDataCache<Dictionary<string, string>> GetCache(string prefix)
		{
			if (!caches.TryGetValue(prefix, out var cache))
			{
				cache = _cacheFactory(
				   $"Stats.{prefix}",
				   TTL_MS,
				   (gamerTags => Resolve(prefix, gamerTags)), Provider
				);
				caches.Add(prefix, cache);
			}

			return cache;
		}

		public Promise<EmptyResponse> SetStats(string access, Dictionary<string, string> stats)
		{
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



		/// <summary>
		/// <para>Supports searching for DBIDs by stat query. This method is useful e.g for friend search</para>
		/// <para>IMPORTANT: This method only works for admin role</para>
		/// </summary>
		public Promise<StatsSearchResponse> SearchStats(string domain, string access, string type, List<Criteria> criteriaList)
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

				if (criteriaList == null)
				{
					tmpError += "> criteria cannot be null\n";
				}
				else if (criteriaList.Count == 0)
				{
					tmpError += "> should be at least one criteria\n";
				}

				if (!string.IsNullOrWhiteSpace(tmpError))
				{
					error += "Error occured in \"SearchStats\". Check for more details:\n\n";
				}

				error += tmpError;
			}

			ArrayDict ConvertCriteriaToArrayDict(Criteria criteria)
			{
				return new ArrayDict
			  {
				  {"stat", criteria.Stat},
				  {"rel", criteria.Rel},
				  {"value", criteria.Value}
			  };
			}

			IsValid(out var errorMessage);
			if (!string.IsNullOrWhiteSpace(errorMessage))
			{
				Debug.LogError(errorMessage);
				return new Promise<StatsSearchResponse>();
			}

			var convertedCriteriaList = new List<ArrayDict>(criteriaList.Count);
			foreach (var criteria in criteriaList)
				convertedCriteriaList.Add(ConvertCriteriaToArrayDict(criteria));

			var payload = new ArrayDict
		  {
			  { "domain", domain },
			  { "access", access },
			  { "objectType", type },
			  { "criteria", convertedCriteriaList }
		  };

			return Requester.Request<StatsSearchResponse>(
				Method.POST,
				"/basic/stats/search",
				Json.Serialize(payload, new StringBuilder()));
		}

		protected abstract Promise<Dictionary<long, Dictionary<string, string>>> Resolve(string prefix,
		   List<long> gamerTags);
	}

	[Serializable]
	public class StatsSearchResponse
	{
		public long[] ids;
	}

	/// <summary>
	/// A definition of a comparison (<see cref="Rel"/>) to be run against the specified <see cref="Stat"/>.
	/// </summary>
	public class Criteria
	{
		/// <summary>
		/// The stat to compare against (LHS of the comparison).
		/// </summary>
		public string Stat { get; }

		/// <summary>
		/// A string representing the comparision to be executed.
		/// <list type="bullet">
		/// <item>Equality: "equal" OR "eq".</item>
		/// <item>Non-Equality: "notequal" OR "neq".</item>
		/// <item>Less Than: "lessthan" OR "lt".</item>
		/// <item>Less Than or Equal: "lessthanequal" OR "lte".</item>
		/// <item>Greater Than: "greaterthan" OR "gt".</item>
		/// <item>Greater Than or Equal: "greaterthanequal" OR "gte".</item>
		/// <item>In: "in".</item>
		/// <item>Not In: "notin" OR "nin".</item>
		/// </list>
		/// </summary>
		public string Rel { get; }

		/// <summary>
		/// The RHS of the comparison.
		/// </summary>
		public string Value { get; }


		/// <param name="stat"><see cref="Stat"/></param>
		/// <param name="rel"><see cref="Rel"/></param>
		/// <param name="value"><see cref="Value"/></param>
		public Criteria(string stat, string rel, string value)
		{
			Stat = stat;
			Rel = rel;
			Value = value;
		}
	}
}
