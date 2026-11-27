
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamBundleNoticeInfo
    {
        public string id;
        public string kind;
        public string source;
        public long minVersion;
        public long maxVersion;
        public System.Nullable<System.DateTime> endOfSupport;
        public System.Nullable<System.DateTime> endOfLife;
        public string severity;
        public string advisoryId;
        public System.Nullable<System.DateTime> updateBy;
        public System.Nullable<System.DateTime> acquisitionsEnd;
        public System.Nullable<System.DateTime> resolvedAt;
        public string message;
        public string link;
        public string replacement;
        public string status;
        public bool warns;
        public string description;
    }
}
