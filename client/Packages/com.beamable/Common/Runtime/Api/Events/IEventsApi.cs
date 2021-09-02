using System;
using System.Collections.Generic;

namespace Beamable.Common.Api.Events
{
   public interface IEventsApi : ISupportsGet<EventsGetResponse>
   {
      Promise<EventClaimResponse> Claim(string eventId);

      /// <summary>
      /// Submit a score for the current player. Note that this is only allowed
      /// if the event has the write_self permission.
      /// </summary>
      /// <param name="eventId">Full ID of the event, including timestamp suffix.</param>
      /// <param name="score">The score to submit (or score delta if incremental).</param>
      /// <param name="incremental">If incremental is true, add to the existing score, otherwise set it absolutely.</param>
      /// <param name="stats">Optional key-value mapping of stats to apply to the score.</param>
      /// <returns>Promise indicating success or failure.</returns>
      Promise<Unit> SetScore(string eventId, double score, bool incremental = false, IDictionary<string, object> stats = null);
   }

   [Serializable]
   public class EventsGetResponse
   {
      public List<EventView> running;
      public List<EventView> done;

      public void Init()
      {
         foreach (var view in running)
         {
            view.Init();
         }
         foreach (var view in done)
         {
            view.Init();
         }
      }
   }

   [Serializable]
   public class EventClaimResponse
   {
      public EventView view;
      public string gameRspJson;
   }

   [Serializable]
   public class EventView
   {
      public string id;
      public string name;
      public string leaderboardId;
      public double score;
      public long rank;
      public long secondsRemaining;
      public DateTime endTime;
      public List<EventReward> scoreRewards;
      public List<EventReward> rankRewards;
      public EventPlayerGroupState groupRewards;

      public EventPhase currentPhase;
      public List<EventPhase> allPhases;

      public void Init()
      {
         endTime = DateTime.UtcNow.AddSeconds(secondsRemaining);
      }
   }

   [Serializable]
   public class EventPlayerGroupState
   {
      public double groupScore;
      public long groupRank;
      public List<EventReward> scoreRewards;
      public List<EventReward> rankRewards;
      public string groupId;
   }

   [Serializable]
   public class EventReward
   {
      public List<EventCurrency> currencies;
      public List<EventItem> items;
      public double min;
      public double max;
      public bool earned;
      public bool claimed;
   }

   [Serializable]
   public class EventCurrency
   {
      public string id;
      public long amount;
   }

   [Serializable]
   public class EventItem
   {
      public string id;
      public Dictionary<string, string> properties;
   }

   [Serializable]
   public class EventPhase
   {
      public string name;
      public long durationSeconds;
      public List<EventRule> rules;
   }

   [Serializable]
   public class EventRule
   {
      public string rule;
      public string value;
   }
}
