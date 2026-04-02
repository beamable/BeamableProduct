using Beamable.Common;
using Beamable.Server.Api.RealmConfig;
using Beamable.Server.Content;

namespace Beamable.Server
{


    /// <summary>
    /// An event description for Beamable's internal server communication system. 
    /// </summary>
    /// <typeparam name="TPayload"></typeparam>
    public class CustomEvent<TPayload>
    {
        /// <summary>
        /// A unique name for the event. 
        /// </summary>
        public string EventName;

        /// <summary>
        /// When true, the event is sent to all services listening for ANY events.
        /// When false, the event is only sent to services that are listening for the given event name
        /// </summary>
        public bool ToAll;

        public CustomEvent(string eventName, bool toAll)
        {
            EventName = eventName;
            ToAll = toAll;
        }
    }

    public static class StandardBeamableEvents
    {
        public static readonly ContentRefreshEvent ContentRefreshEvent = ContentRefreshEvent.Instance;
        public static readonly RealmConfigUpdateEvent RealmConfigUpdateEvent = RealmConfigUpdateEvent.Instance;
    }

    public class ContentRefreshEvent : CustomEvent<ContentManifestEvent>
    {
        public static ContentRefreshEvent Instance { get; } = new ContentRefreshEvent();

        private ContentRefreshEvent() : base(Constants.Features.Services.CONTENT_UPDATE_EVENT, true)
        {
        }
    }


    public class RealmConfigUpdateEvent : CustomEvent<GetRealmConfigResponse>
    {
        public static RealmConfigUpdateEvent Instance { get; } = new RealmConfigUpdateEvent();

        private RealmConfigUpdateEvent() : base(Constants.Features.Services.REALM_CONFIG_UPDATE_EVENT, true)
        {
        }
    }
}

namespace Beamable.Server.Api.RealmConfig
{
    [System.Serializable]
    public class GetRealmConfigResponse
    {
        // ReSharper disable once InconsistentNaming
        public Dictionary<string, string> config;
    }
}

namespace Beamable.Server.Content
{
    public class ContentManifestEvent
    {
        public string[] categories;
    }
}
