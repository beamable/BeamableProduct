using System.Collections.Generic;
using Newtonsoft.Json;

namespace Beamable.Notifications
{
    // DTOs mirroring the Swift core's Codable models. Serialized with Newtonsoft so
    // arbitrary `userInfo` dictionaries marshal cleanly. Nulls are omitted on the way
    // out so the Swift side sees only the fields you set (all are optional there).

    internal static class Json
    {
        internal static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        internal static string Serialize(object value) => JsonConvert.SerializeObject(value, Settings);
        internal static T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json);
    }

    public class TriggerSpec
    {
        // "immediate" | "timeInterval" | "calendar"
        [JsonProperty("type")] public string Type = "immediate";
        [JsonProperty("seconds", NullValueHandling = NullValueHandling.Ignore)] public double? Seconds;
        [JsonProperty("repeats", NullValueHandling = NullValueHandling.Ignore)] public bool? Repeats;
        [JsonProperty("year", NullValueHandling = NullValueHandling.Ignore)] public int? Year;
        [JsonProperty("month", NullValueHandling = NullValueHandling.Ignore)] public int? Month;
        [JsonProperty("day", NullValueHandling = NullValueHandling.Ignore)] public int? Day;
        [JsonProperty("hour", NullValueHandling = NullValueHandling.Ignore)] public int? Hour;
        [JsonProperty("minute", NullValueHandling = NullValueHandling.Ignore)] public int? Minute;
        [JsonProperty("second", NullValueHandling = NullValueHandling.Ignore)] public int? Second;
        [JsonProperty("weekday", NullValueHandling = NullValueHandling.Ignore)] public int? Weekday;

        public static TriggerSpec After(double seconds, bool repeats = false) =>
            new TriggerSpec { Type = "timeInterval", Seconds = seconds, Repeats = repeats };
    }

    public class AttachmentSpec
    {
        [JsonProperty("identifier", NullValueHandling = NullValueHandling.Ignore)] public string Identifier;
        [JsonProperty("url")] public string Url;
        [JsonProperty("typeHint", NullValueHandling = NullValueHandling.Ignore)] public string TypeHint;
    }

    public class LocalRequest
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)] public string Title;
        [JsonProperty("body", NullValueHandling = NullValueHandling.Ignore)] public string Body;
        [JsonProperty("subtitle", NullValueHandling = NullValueHandling.Ignore)] public string Subtitle;
        [JsonProperty("badge", NullValueHandling = NullValueHandling.Ignore)] public int? Badge;
        [JsonProperty("sound", NullValueHandling = NullValueHandling.Ignore)] public string Sound;
        [JsonProperty("categoryId", NullValueHandling = NullValueHandling.Ignore)] public string CategoryId;
        [JsonProperty("threadId", NullValueHandling = NullValueHandling.Ignore)] public string ThreadId;
        [JsonProperty("interruptionLevel", NullValueHandling = NullValueHandling.Ignore)] public string InterruptionLevel;
        [JsonProperty("trigger", NullValueHandling = NullValueHandling.Ignore)] public TriggerSpec Trigger;
        [JsonProperty("attachments", NullValueHandling = NullValueHandling.Ignore)] public List<AttachmentSpec> Attachments;
        [JsonProperty("userInfo", NullValueHandling = NullValueHandling.Ignore)] public Dictionary<string, object> UserInfo;
        [JsonProperty("templateId", NullValueHandling = NullValueHandling.Ignore)] public string TemplateId;
        [JsonProperty("templateValues", NullValueHandling = NullValueHandling.Ignore)] public Dictionary<string, string> TemplateValues;
    }

    public class TemplateSpec
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("titleFormat", NullValueHandling = NullValueHandling.Ignore)] public string TitleFormat;
        [JsonProperty("bodyFormat", NullValueHandling = NullValueHandling.Ignore)] public string BodyFormat;
        [JsonProperty("subtitleFormat", NullValueHandling = NullValueHandling.Ignore)] public string SubtitleFormat;
        [JsonProperty("sound", NullValueHandling = NullValueHandling.Ignore)] public string Sound;
        [JsonProperty("categoryId", NullValueHandling = NullValueHandling.Ignore)] public string CategoryId;
        [JsonProperty("badge", NullValueHandling = NullValueHandling.Ignore)] public int? Badge;
        [JsonProperty("defaultAttachments", NullValueHandling = NullValueHandling.Ignore)] public List<AttachmentSpec> DefaultAttachments;
    }

    public class ActionSpec
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("title")] public string Title;
        [JsonProperty("foreground", NullValueHandling = NullValueHandling.Ignore)] public bool? Foreground;
        [JsonProperty("destructive", NullValueHandling = NullValueHandling.Ignore)] public bool? Destructive;
        [JsonProperty("authenticationRequired", NullValueHandling = NullValueHandling.Ignore)] public bool? AuthenticationRequired;
    }

    public class CategorySpec
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("actions")] public List<ActionSpec> Actions = new List<ActionSpec>();
        [JsonProperty("hiddenPreviewsBodyPlaceholder", NullValueHandling = NullValueHandling.Ignore)] public string HiddenPreviewsBodyPlaceholder;
    }

    public class PermissionOptions
    {
        [JsonProperty("alert")] public bool Alert = true;
        [JsonProperty("badge")] public bool Badge = true;
        [JsonProperty("sound")] public bool Sound = true;
        [JsonProperty("provisional", NullValueHandling = NullValueHandling.Ignore)] public bool? Provisional;
        [JsonProperty("criticalAlert", NullValueHandling = NullValueHandling.Ignore)] public bool? CriticalAlert;
        [JsonProperty("carPlay", NullValueHandling = NullValueHandling.Ignore)] public bool? CarPlay;
    }

    public class AnalyticsConfig
    {
        [JsonProperty("enabled")] public bool Enabled = true;
        [JsonProperty("endpoint")] public string Endpoint;
        [JsonProperty("headers", NullValueHandling = NullValueHandling.Ignore)] public Dictionary<string, string> Headers;
        [JsonProperty("commonParams", NullValueHandling = NullValueHandling.Ignore)] public Dictionary<string, object> CommonParams;
    }

    public class PermissionResult
    {
        [JsonProperty("status")] public string Status;
        [JsonProperty("granted")] public bool Granted;
        [JsonProperty("alert")] public bool Alert;
        [JsonProperty("badge")] public bool Badge;
        [JsonProperty("sound")] public bool Sound;
    }

    public class DeliveryReceipt
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("timestamp")] public double Timestamp;
        [JsonProperty("source")] public string Source;
        [JsonProperty("userInfo")] public Dictionary<string, object> UserInfo;
    }

    public class NotificationData
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("title")] public string Title;
        [JsonProperty("body")] public string Body;
        [JsonProperty("subtitle")] public string Subtitle;
        [JsonProperty("deepLink")] public string DeepLink;
        [JsonProperty("actionId")] public string ActionId;
        [JsonProperty("wasLaunch")] public bool WasLaunch;
        [JsonProperty("userInfo")] public Dictionary<string, object> UserInfo;
    }
}
