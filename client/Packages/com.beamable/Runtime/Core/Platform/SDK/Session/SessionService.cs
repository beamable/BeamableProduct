using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Serialization.SmallerJSON;
using Beamable.Service;

namespace Beamable.Api.Sessions
{
	public interface ISessionService
	{
		Promise<EmptyResponse> StartSession(User user, string advertisingId, string locale);
		Promise<Session> GetHeartbeat(long gamerTag);
		Promise<EmptyResponse> SendHeartbeat();
		float SessionStartedAt { get; }
		float TimeSinceLastSessionStart { get; }

	}

   /// <summary>
   /// This type defines the %Client main entry point for the %Session feature.
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
   public class SessionService : ISessionService
   {
      private static long TTL_MS = 60 * 1000;

      private UnityUserDataCache<Session> cache;
      private IBeamableRequester _requester;

      private readonly SessionParameterProvider _parameterProvider;
      private readonly SessionDeviceOptions _deviceOptions;

      public float SessionStartedAt { get; private set; }
      public float TimeSinceLastSessionStart => Time.realtimeSinceStartup - SessionStartedAt;

      public SessionService (IBeamableRequester requester, SessionParameterProvider parameterProvider, SessionDeviceOptions deviceOptions)
      {
         _requester = requester;
         // _parameterProvider = ServiceManager.ResolveIfAvailable<SessionParameterProvider>();
         // _deviceOptions = ServiceManager.ResolveIfAvailable<SessionDeviceOptions>();
         _parameterProvider = parameterProvider;
         _deviceOptions = deviceOptions;
         cache = new UnityUserDataCache<Session>("Session", TTL_MS, resolve);
      }

      private Promise<Dictionary<long, Session>> resolve(List<long> gamerTags)
      {
         string queryString = "";
         for (int i = 0; i < gamerTags.Count; i++)
         {
            if (i > 0)
            {
               queryString += "&";
            }
            queryString += String.Format("gts={0}", gamerTags[i]);
         }

         return _requester.Request<MultiOnlineStatusesResponse>(
            Method.GET,
            String.Format("/presence/bulk?{0}", queryString)
         ).Map(rsp =>
         {
            Dictionary<long, Session> result = new Dictionary<long, Session>();
            var dict = rsp.ToDictionary();
            for (int i = 0; i < gamerTags.Count; i++)
            {
               if (!dict.ContainsKey(gamerTags[i]))
               {
                  dict[gamerTags[i]] = 0;
               }
               result.Add(gamerTags[i], new Session(dict[gamerTags[i]]));
            }
            return result;
         });
      }

      private ArrayDict GenerateDeviceParams(SessionStartRequestArgs args)
      {
         ArrayDict deviceParams = new ArrayDict(); // by default, don't send anything...
         if (_deviceOptions != null)
         {
            foreach (var option in _deviceOptions.All)
            {
               if (option == null || !option.IsEnabled) continue;
               deviceParams.Add(option.Key, option.Get(args));
            }
         }

         return deviceParams;
      }

      private Promise<ArrayDict> GenerateCustomParams(ArrayDict deviceParams, User user)
      {
         return (_parameterProvider != null) ? _parameterProvider.GetCustomParameters(deviceParams, user) : Promise<ArrayDict>.Successful(null);
      }

      /// <summary>
      /// Starts a new Beamable user session. A session will record user analytics and track the user's play times.
      /// This method is automatically called by the Beamable SDK anytime the user changes and when Beamable SDK is initialized.
      /// </summary>
      /// <param name="advertisingId"></param>
      /// <param name="locale"></param>
      /// <returns></returns>
      public Promise<EmptyResponse> StartSession (User user, string advertisingId, string locale)
      {
	      SessionStartedAt = Time.realtimeSinceStartup;
         var args = new SessionStartRequestArgs
         {
            advertisingId = advertisingId,
            locale = locale
         };
         var deviceParams = GenerateDeviceParams(args);

         var promise = GenerateCustomParams(deviceParams, user);

         return promise.FlatMap(customParams =>
         {
            var req = new ArrayDict
            {
               {"platform", Application.platform.ToString()},
               {"device", SystemInfo.deviceModel.ToString()},
               {"locale", locale}
            };
            if (customParams != null && customParams.Count > 0)
            {
               req["customParams"] = customParams;
            }
            var json = Json.Serialize(req, new StringBuilder());
            return _requester.Request<EmptyResponse>(
               Method.POST,
               "/basic/session",
               json
            );
         });
      }

      /// <summary>
      /// Notifies the Beamable platform that the session is still active.
      /// This method is automatically called at a standard interval by the Beamable SDK itself.
      /// </summary>
      /// <returns></returns>
      public Promise<EmptyResponse> SendHeartbeat() {
         return _requester.Request<EmptyResponse>(
            Method.POST,
            "/basic/session/heartbeat"
         );
      }

      public Promise<Session> GetHeartbeat (long gamerTag)
      {
         return cache.Get(gamerTag);
      }
   }

   public class SessionStartRequestArgs
   {
      public string advertisingId;
      public string locale;
   }

   [Serializable]
   public class MultiOnlineStatusesResponse
   {
      public List<SessionHeartbeat> statuses;

      public Dictionary<long, long> ToDictionary () {
         Dictionary<long, long> result = new Dictionary<long, long>();
         for (int i=0; i<statuses.Count; i++) {
            var next = statuses[i];
            result[next.gt] = next.heartbeat;
         }
         return result;
      }
   }

   [Serializable]
   public class SessionHeartbeat
   {
      public long gt;
      public long heartbeat;
   }

   public class Session
   {
      private static readonly DateTime Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
      private static long CurrentTimeSeconds() {
         return (long) (DateTime.UtcNow - Jan1st1970).TotalSeconds;
      }

      public long Heartbeat;
      public long LastSeenMinutes;

      public Session(long heartbeat)
      {
         Heartbeat = heartbeat;
         LastSeenMinutes = (CurrentTimeSeconds() - heartbeat) / 60;
      }
   }
}
