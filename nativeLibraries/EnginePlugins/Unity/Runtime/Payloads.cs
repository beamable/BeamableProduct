using System.Collections.Generic;
using Beamable.Serialization;

namespace Beamable.Notifications
{
    /// <summary>
    /// JSON helpers built on Beamable's <see cref="JsonSerializable"/> / SmallerJSON stack.
    /// <see cref="JsonSerializable.FromJson{T}"/> only handles top-level JSON objects, so this adds
    /// a top-level array reader (the native bridge sends arrays for pending notifications + delivery
    /// receipts).
    /// </summary>
    internal static class NotificationJson
    {
        /// <summary>Deserializes a top-level JSON array into a List of ISerializable elements.</summary>
        public static List<T> FromJsonArray<T>(string json) where T : JsonSerializable.ISerializable, new()
        {
            var result = new List<T>();
            if (string.IsNullOrEmpty(json)) return result;
            if (!(SmallerJSON.Json.Deserialize(json) is IList<object> list)) return result;
            foreach (var item in list)
            {
                if (item is IDictionary<string, object> map)
                {
                    var elem = new T();
                    JsonSerializable.Deserialize(elem, map);
                    result.Add(elem);
                }
            }
            return result;
        }
    }

    // DTOs mirroring the Swift core's Codable models and the Kotlin PushModels. Serialized with
    // Beamable's JsonSerializable / SmallerJSON stack (the same JSON used by the rest of the
    // Beamable Unity SDK — no Newtonsoft). Each DTO implements JsonSerializable.ISerializable and
    // defines Serialize(IStreamSerializer s). To get JSON: JsonSerializable.ToJson(dto); to parse:
    // JsonSerializable.FromJson<T>(json).
    //
    // Nulls are omitted on the way out (replicating Newtonsoft NullValueHandling.Ignore) with the
    // conditional-serialize pattern: reference/optional values are only written when non-null. The
    // JsonSaveStream already omits Nullable<T> fields with no value, so primitives behave the same.
    //
    // The JSON field names below MUST match the native bridge verbatim (iOS C ABI + Android
    // UnityNotifications facade depend on them) and the shared §3.3 intent-data contract.

    /// <summary>
    /// Player auth credentials pushed to the native layers so the closed-app campaign funnel can
    /// attribute conversions. Field names are the canonical camelCase wire contract the iOS C ABI
    /// (<c>bmn_configureAuth</c>) and the Android <c>UnityNotifications.configureAuth</c> facade
    /// both expect. <c>AccessTokenExpiresAt</c> is an absolute epoch time in MILLISECONDS.
    /// </summary>
    public class AuthCredentials : JsonSerializable.ISerializable
    {
        public string AccessToken;
        public string RefreshToken;
        public long AccessTokenExpiresAt;
        public string Cid;
        public string Pid;
        public string Host;

        public void Serialize(JsonSerializable.IStreamSerializer s)
        {
            if (s.isSaving)
            {
                if (AccessToken != null) s.Serialize("accessToken", ref AccessToken);
                if (RefreshToken != null) s.Serialize("refreshToken", ref RefreshToken);
                s.Serialize("accessTokenExpiresAt", ref AccessTokenExpiresAt);
                if (Cid != null) s.Serialize("cid", ref Cid);
                if (Pid != null) s.Serialize("pid", ref Pid);
                if (Host != null) s.Serialize("host", ref Host);
            }
            else
            {
                s.Serialize("accessToken", ref AccessToken);
                s.Serialize("refreshToken", ref RefreshToken);
                s.Serialize("accessTokenExpiresAt", ref AccessTokenExpiresAt);
                s.Serialize("cid", ref Cid);
                s.Serialize("pid", ref Pid);
                s.Serialize("host", ref Host);
            }
        }
    }

    public class TriggerSpec : JsonSerializable.ISerializable
    {
        // "immediate" | "timeInterval" | "calendar"
        public string Type = "immediate";
        public double? Seconds;
        public bool? Repeats;
        public int? Year;
        public int? Month;
        public int? Day;
        public int? Hour;
        public int? Minute;
        public int? Second;
        public int? Weekday;

        public static TriggerSpec After(double seconds, bool repeats = false) =>
            new TriggerSpec { Type = "timeInterval", Seconds = seconds, Repeats = repeats };

        public void Serialize(JsonSerializable.IStreamSerializer s)
        {
            if (s.isSaving)
            {
                if (Type != null) s.Serialize("type", ref Type);
                if (Seconds.HasValue) s.Serialize("seconds", ref Seconds);
                if (Repeats.HasValue) s.Serialize("repeats", ref Repeats);
                if (Year.HasValue) s.Serialize("year", ref Year);
                if (Month.HasValue) s.Serialize("month", ref Month);
                if (Day.HasValue) s.Serialize("day", ref Day);
                if (Hour.HasValue) s.Serialize("hour", ref Hour);
                if (Minute.HasValue) s.Serialize("minute", ref Minute);
                if (Second.HasValue) s.Serialize("second", ref Second);
                if (Weekday.HasValue) s.Serialize("weekday", ref Weekday);
            }
            else
            {
                s.Serialize("type", ref Type);
                s.Serialize("seconds", ref Seconds);
                s.Serialize("repeats", ref Repeats);
                s.Serialize("year", ref Year);
                s.Serialize("month", ref Month);
                s.Serialize("day", ref Day);
                s.Serialize("hour", ref Hour);
                s.Serialize("minute", ref Minute);
                s.Serialize("second", ref Second);
                s.Serialize("weekday", ref Weekday);
            }
        }
    }

    public class AttachmentSpec : JsonSerializable.ISerializable
    {
        public string Identifier;
        public string Url;
        public string TypeHint;

        public void Serialize(JsonSerializable.IStreamSerializer s)
        {
            if (s.isSaving)
            {
                if (Identifier != null) s.Serialize("identifier", ref Identifier);
                if (Url != null) s.Serialize("url", ref Url);
                if (TypeHint != null) s.Serialize("typeHint", ref TypeHint);
            }
            else
            {
                s.Serialize("identifier", ref Identifier);
                s.Serialize("url", ref Url);
                s.Serialize("typeHint", ref TypeHint);
            }
        }
    }

    public class LocalRequest : JsonSerializable.ISerializable
    {
        public string Id;
        public string Title;
        public string Body;
        public string Subtitle;
        public int? Badge;
        public string Sound;
        public string CategoryId;
        public string ThreadId;
        public string InterruptionLevel;
        public TriggerSpec Trigger;
        public List<AttachmentSpec> Attachments;
        public Dictionary<string, object> UserInfo;
        public string TemplateId;
        public Dictionary<string, string> TemplateValues;

        public void Serialize(JsonSerializable.IStreamSerializer s)
        {
            if (s.isSaving)
            {
                if (Id != null) s.Serialize("id", ref Id);
                if (Title != null) s.Serialize("title", ref Title);
                if (Body != null) s.Serialize("body", ref Body);
                if (Subtitle != null) s.Serialize("subtitle", ref Subtitle);
                if (Badge.HasValue) s.Serialize("badge", ref Badge);
                if (Sound != null) s.Serialize("sound", ref Sound);
                if (CategoryId != null) s.Serialize("categoryId", ref CategoryId);
                if (ThreadId != null) s.Serialize("threadId", ref ThreadId);
                if (InterruptionLevel != null) s.Serialize("interruptionLevel", ref InterruptionLevel);
                if (Trigger != null) s.Serialize("trigger", ref Trigger);
                if (Attachments != null) s.SerializeList("attachments", ref Attachments);
                if (UserInfo != null) SerializeFreeFormDict(s, "userInfo", ref UserInfo);
                if (TemplateId != null) s.Serialize("templateId", ref TemplateId);
                if (TemplateValues != null) s.SerializeDictionary("templateValues", ref TemplateValues);
            }
            else
            {
                s.Serialize("id", ref Id);
                s.Serialize("title", ref Title);
                s.Serialize("body", ref Body);
                s.Serialize("subtitle", ref Subtitle);
                s.Serialize("badge", ref Badge);
                s.Serialize("sound", ref Sound);
                s.Serialize("categoryId", ref CategoryId);
                s.Serialize("threadId", ref ThreadId);
                s.Serialize("interruptionLevel", ref InterruptionLevel);
                s.Serialize("trigger", ref Trigger);
                s.SerializeList("attachments", ref Attachments);
                SerializeFreeFormDict(s, "userInfo", ref UserInfo);
                s.Serialize("templateId", ref TemplateId);
                s.SerializeDictionary("templateValues", ref TemplateValues);
            }
        }

        // Free-form Dictionary<string,object>. The IStreamSerializer dictionary overload takes an
        // IDictionary<string,object> by ref, so we round-trip through a local of that type.
        internal static void SerializeFreeFormDict(JsonSerializable.IStreamSerializer s, string key,
            ref Dictionary<string, object> dict)
        {
            IDictionary<string, object> target = dict;
            s.Serialize(key, ref target);
            if (s.isLoading)
                dict = target as Dictionary<string, object> ?? (target != null ? new Dictionary<string, object>(target) : null);
        }
    }

    public class TemplateSpec : JsonSerializable.ISerializable
    {
        public string Id;
        public string TitleFormat;
        public string BodyFormat;
        public string SubtitleFormat;
        public string Sound;
        public string CategoryId;
        public int? Badge;
        public List<AttachmentSpec> DefaultAttachments;

        public void Serialize(JsonSerializable.IStreamSerializer s)
        {
            if (s.isSaving)
            {
                if (Id != null) s.Serialize("id", ref Id);
                if (TitleFormat != null) s.Serialize("titleFormat", ref TitleFormat);
                if (BodyFormat != null) s.Serialize("bodyFormat", ref BodyFormat);
                if (SubtitleFormat != null) s.Serialize("subtitleFormat", ref SubtitleFormat);
                if (Sound != null) s.Serialize("sound", ref Sound);
                if (CategoryId != null) s.Serialize("categoryId", ref CategoryId);
                if (Badge.HasValue) s.Serialize("badge", ref Badge);
                if (DefaultAttachments != null) s.SerializeList("defaultAttachments", ref DefaultAttachments);
            }
            else
            {
                s.Serialize("id", ref Id);
                s.Serialize("titleFormat", ref TitleFormat);
                s.Serialize("bodyFormat", ref BodyFormat);
                s.Serialize("subtitleFormat", ref SubtitleFormat);
                s.Serialize("sound", ref Sound);
                s.Serialize("categoryId", ref CategoryId);
                s.Serialize("badge", ref Badge);
                s.SerializeList("defaultAttachments", ref DefaultAttachments);
            }
        }
    }

    public class ActionSpec : JsonSerializable.ISerializable
    {
        public string Id;
        public string Title;
        public bool? Foreground;
        public bool? Destructive;
        public bool? AuthenticationRequired;

        public void Serialize(JsonSerializable.IStreamSerializer s)
        {
            if (s.isSaving)
            {
                if (Id != null) s.Serialize("id", ref Id);
                if (Title != null) s.Serialize("title", ref Title);
                if (Foreground.HasValue) s.Serialize("foreground", ref Foreground);
                if (Destructive.HasValue) s.Serialize("destructive", ref Destructive);
                if (AuthenticationRequired.HasValue) s.Serialize("authenticationRequired", ref AuthenticationRequired);
            }
            else
            {
                s.Serialize("id", ref Id);
                s.Serialize("title", ref Title);
                s.Serialize("foreground", ref Foreground);
                s.Serialize("destructive", ref Destructive);
                s.Serialize("authenticationRequired", ref AuthenticationRequired);
            }
        }
    }

    public class CategorySpec : JsonSerializable.ISerializable
    {
        public string Id;
        public List<ActionSpec> Actions = new List<ActionSpec>();
        public string HiddenPreviewsBodyPlaceholder;

        public void Serialize(JsonSerializable.IStreamSerializer s)
        {
            if (s.isSaving)
            {
                if (Id != null) s.Serialize("id", ref Id);
                if (Actions != null) s.SerializeList("actions", ref Actions);
                if (HiddenPreviewsBodyPlaceholder != null)
                    s.Serialize("hiddenPreviewsBodyPlaceholder", ref HiddenPreviewsBodyPlaceholder);
            }
            else
            {
                s.Serialize("id", ref Id);
                s.SerializeList("actions", ref Actions);
                s.Serialize("hiddenPreviewsBodyPlaceholder", ref HiddenPreviewsBodyPlaceholder);
            }
        }
    }

    public class PermissionOptions : JsonSerializable.ISerializable
    {
        public bool Alert = true;
        public bool Badge = true;
        public bool Sound = true;
        public bool? Provisional;
        public bool? CriticalAlert;
        public bool? CarPlay;

        public void Serialize(JsonSerializable.IStreamSerializer s)
        {
            s.Serialize("alert", ref Alert);
            s.Serialize("badge", ref Badge);
            s.Serialize("sound", ref Sound);
            if (s.isSaving)
            {
                if (Provisional.HasValue) s.Serialize("provisional", ref Provisional);
                if (CriticalAlert.HasValue) s.Serialize("criticalAlert", ref CriticalAlert);
                if (CarPlay.HasValue) s.Serialize("carPlay", ref CarPlay);
            }
            else
            {
                s.Serialize("provisional", ref Provisional);
                s.Serialize("criticalAlert", ref CriticalAlert);
                s.Serialize("carPlay", ref CarPlay);
            }
        }
    }

    public class PermissionResult : JsonSerializable.ISerializable
    {
        public string Status;
        public bool Granted;
        public bool Alert;
        public bool Badge;
        public bool Sound;

        public void Serialize(JsonSerializable.IStreamSerializer s)
        {
            // Value-type flags are always emitted (meaningful at their default, matching the
            // pre-migration Newtonsoft behavior); only the nullable string is omitted when null.
            if (s.isSaving)
            {
                if (Status != null) s.Serialize("status", ref Status);
                s.Serialize("granted", ref Granted);
                s.Serialize("alert", ref Alert);
                s.Serialize("badge", ref Badge);
                s.Serialize("sound", ref Sound);
            }
            else
            {
                s.Serialize("status", ref Status);
                s.Serialize("granted", ref Granted);
                s.Serialize("alert", ref Alert);
                s.Serialize("badge", ref Badge);
                s.Serialize("sound", ref Sound);
            }
        }
    }

    public class DeliveryReceipt : JsonSerializable.ISerializable
    {
        public string Id;
        public double Timestamp;
        public string Source;
        public Dictionary<string, object> UserInfo;

        public void Serialize(JsonSerializable.IStreamSerializer s)
        {
            // Omit null reference fields on save (Newtonsoft NullValueHandling.Ignore parity);
            // the value-type timestamp is always emitted. Load reads unconditionally.
            if (s.isSaving)
            {
                if (Id != null) s.Serialize("id", ref Id);
                s.Serialize("timestamp", ref Timestamp);
                if (Source != null) s.Serialize("source", ref Source);
                if (UserInfo != null) LocalRequest.SerializeFreeFormDict(s, "userInfo", ref UserInfo);
            }
            else
            {
                s.Serialize("id", ref Id);
                s.Serialize("timestamp", ref Timestamp);
                s.Serialize("source", ref Source);
                LocalRequest.SerializeFreeFormDict(s, "userInfo", ref UserInfo);
            }
        }
    }

    /// <summary>
    /// A single offer carried by a campaign notification (§3.3 <c>offers[]</c>). <c>CustomData</c>
    /// is free-form (the spec's generic <c>T</c>): it travels as an opaque JSON object and is only
    /// typed at the SDK layer. <c>Value</c> is <c>string|number</c> on the wire (matches Android
    /// <c>NotificationOffer.rawValue</c>), so it is held as an <c>object</c> (a boxed string,
    /// <c>long</c>, or <c>double</c>) and serialized through the free-form/"any" path so a numeric
    /// value round-trips as a JSON number instead of being coerced to a quoted string.
    /// </summary>
    public class Offer : JsonSerializable.ISerializable
    {
        public string ItemId;
        public object Value;
        public Dictionary<string, object> CustomData;

        public void Serialize(JsonSerializable.IStreamSerializer s)
        {
            if (s.isSaving)
            {
                if (ItemId != null) s.Serialize("itemId", ref ItemId);
                // Emit `value` with its native JSON type (number stays a number, string stays a
                // string) via the same free-form "any" path that backs Dictionary<string,object>.
                // SetValue routes through SerializeAny on the save streams; omit when null.
                if (Value != null) s.SetValue("value", Value);
                if (CustomData != null) LocalRequest.SerializeFreeFormDict(s, "customData", ref CustomData);
            }
            else
            {
                s.Serialize("itemId", ref ItemId);
                // `value` may be a number or a string on the wire; keep the raw boxed JSON value
                // (long/double/string) so its type is preserved on re-emit.
                Value = s.HasKey("value") ? s.GetValue("value") : null;
                LocalRequest.SerializeFreeFormDict(s, "customData", ref CustomData);
            }
        }
    }

    /// <summary>
    /// The canonical notification intent-data schema (§3.3), as parsed out of a notification's
    /// <c>userInfo</c>. Scalar fields are plain strings; <c>Offers</c> and <c>CampaignData</c> arrive
    /// stringified on the wire (Decision Q3) and are parsed into typed objects here — this is the
    /// "typed at the engine/SDK layer" generic boundary. Field names match the shared iOS/Android
    /// contract verbatim. A notification is only part of a tracked campaign when both
    /// <c>CampaignId</c> and <c>NodeId</c> are present (§4.2).
    /// </summary>
    public class NotificationIntentData : JsonSerializable.ISerializable
    {
        public string CampaignId;
        public string NodeId;
        public string GamerTag;
        public string AccountId;
        /// <summary>"&lt;cid&gt;.&lt;pid&gt;" realm scope.</summary>
        public string CidPid;
        public string Deeplink;
        public List<Offer> Offers;
        public Dictionary<string, object> CampaignData;

        /// <summary>True when this notification belongs to a tracked campaign (§4.2).</summary>
        public bool IsTrackedCampaign =>
            !string.IsNullOrEmpty(CampaignId) && !string.IsNullOrEmpty(NodeId);

        public void Serialize(JsonSerializable.IStreamSerializer s)
        {
            if (s.isSaving)
            {
                if (CampaignId != null) s.Serialize("campaignId", ref CampaignId);
                if (NodeId != null) s.Serialize("nodeId", ref NodeId);
                if (GamerTag != null) s.Serialize("gamerTag", ref GamerTag);
                if (AccountId != null) s.Serialize("accountId", ref AccountId);
                if (CidPid != null) s.Serialize("cidPid", ref CidPid);
                if (Deeplink != null) s.Serialize("deeplink", ref Deeplink);
                if (Offers != null) s.SerializeList("offers", ref Offers);
                if (CampaignData != null) LocalRequest.SerializeFreeFormDict(s, "campaignData", ref CampaignData);
            }
            else
            {
                s.Serialize("campaignId", ref CampaignId);
                s.Serialize("nodeId", ref NodeId);
                s.Serialize("gamerTag", ref GamerTag);
                s.Serialize("accountId", ref AccountId);
                s.Serialize("cidPid", ref CidPid);
                s.Serialize("deeplink", ref Deeplink);
                s.SerializeList("offers", ref Offers);
                LocalRequest.SerializeFreeFormDict(s, "campaignData", ref CampaignData);
            }
        }

        /// <summary>
        /// Parse the §3.3 intent-data schema out of a notification's free-form <c>userInfo</c>.
        /// Scalars are read directly; <c>offers</c>/<c>campaignData</c> are accepted both as
        /// JSON-encoded strings (the canonical wire form per Decision Q3) and — defensively — as
        /// already-decoded objects, since locally-scheduled notifications may carry real nested values.
        /// </summary>
        public static NotificationIntentData FromUserInfo(IDictionary<string, object> userInfo)
        {
            var intent = new NotificationIntentData();
            if (userInfo == null) return intent;

            intent.CampaignId = AsString(userInfo, "campaignId");
            intent.NodeId = AsString(userInfo, "nodeId");
            intent.GamerTag = AsString(userInfo, "gamerTag");
            intent.AccountId = AsString(userInfo, "accountId");
            intent.CidPid = AsString(userInfo, "cidPid");
            intent.Deeplink = ReadDeeplink(userInfo);

            if (userInfo.TryGetValue("offers", out var offersRaw) && offersRaw != null)
                intent.Offers = ParseOffers(offersRaw);

            if (userInfo.TryGetValue("campaignData", out var dataRaw) && dataRaw != null)
                intent.CampaignData = ParseObject(dataRaw);

            return intent;
        }

        // Tolerant deeplink read, matching the native key spellings (deepLink/deeplink/deep_link).
        private static string ReadDeeplink(IDictionary<string, object> userInfo)
        {
            foreach (var key in new[] { "deepLink", "deeplink", "deep_link" })
            {
                var v = AsString(userInfo, key);
                if (!string.IsNullOrEmpty(v)) return v;
            }
            return null;
        }

        private static string AsString(IDictionary<string, object> dict, string key) =>
            dict.TryGetValue(key, out var v) && v != null ? v.ToString() : null;

        private static List<Offer> ParseOffers(object raw)
        {
            // Stringified JSON array (canonical wire form).
            if (raw is string str)
            {
                if (string.IsNullOrEmpty(str)) return null;
                var decoded = SmallerJSON.Json.Deserialize(str);
                return OffersFromList(decoded as IList<object>);
            }
            // Defensive: an already-decoded list of dicts.
            return OffersFromList(raw as IList<object>);
        }

        private static List<Offer> OffersFromList(IList<object> list)
        {
            if (list == null) return null;
            var offers = new List<Offer>(list.Count);
            foreach (var item in list)
            {
                if (item is IDictionary<string, object> map)
                {
                    var offer = new Offer();
                    JsonSerializable.Deserialize(offer, map);
                    offers.Add(offer);
                }
            }
            return offers;
        }

        private static Dictionary<string, object> ParseObject(object raw)
        {
            // SmallerJSON parses objects into ArrayDict (an IDictionary<string,object>, not a
            // Dictionary<string,object>), so normalize through the IDictionary path.
            if (raw is string str)
            {
                if (string.IsNullOrEmpty(str)) return null;
                raw = SmallerJSON.Json.Deserialize(str);
            }
            if (raw is Dictionary<string, object> dict) return dict;
            if (raw is IDictionary<string, object> idict) return new Dictionary<string, object>(idict);
            return null;
        }
    }

    public class NotificationData : JsonSerializable.ISerializable
    {
        public string Id;
        public string Title;
        public string Body;
        public string Subtitle;
        public string DeepLink;
        public string ActionId;
        public bool WasLaunch;
        public Dictionary<string, object> UserInfo;

        public void Serialize(JsonSerializable.IStreamSerializer s)
        {
            // Omit null reference fields on save (Newtonsoft NullValueHandling.Ignore parity);
            // the value-type wasLaunch flag is always emitted. Load reads unconditionally.
            if (s.isSaving)
            {
                if (Id != null) s.Serialize("id", ref Id);
                if (Title != null) s.Serialize("title", ref Title);
                if (Body != null) s.Serialize("body", ref Body);
                if (Subtitle != null) s.Serialize("subtitle", ref Subtitle);
                if (DeepLink != null) s.Serialize("deepLink", ref DeepLink);
                if (ActionId != null) s.Serialize("actionId", ref ActionId);
                s.Serialize("wasLaunch", ref WasLaunch);
                if (UserInfo != null) LocalRequest.SerializeFreeFormDict(s, "userInfo", ref UserInfo);
            }
            else
            {
                s.Serialize("id", ref Id);
                s.Serialize("title", ref Title);
                s.Serialize("body", ref Body);
                s.Serialize("subtitle", ref Subtitle);
                s.Serialize("deepLink", ref DeepLink);
                s.Serialize("actionId", ref ActionId);
                s.Serialize("wasLaunch", ref WasLaunch);
                LocalRequest.SerializeFreeFormDict(s, "userInfo", ref UserInfo);
            }
        }

        /// <summary>
        /// The §3.3 campaign intent-data view of this notification, parsed from <see cref="UserInfo"/>
        /// (additive — does not alter the existing wire shape). Use this to attribute in-app
        /// offer clicks/conversions back to the originating campaign (§4.7). Falls back to
        /// <see cref="DeepLink"/> when <c>userInfo</c> carries no deeplink key.
        /// </summary>
        public NotificationIntentData CampaignIntent
        {
            get
            {
                var intent = NotificationIntentData.FromUserInfo(UserInfo);
                if (string.IsNullOrEmpty(intent.Deeplink) && !string.IsNullOrEmpty(DeepLink))
                    intent.Deeplink = DeepLink;
                return intent;
            }
        }
    }
}
