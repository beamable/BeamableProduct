
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamBundleInfoCommandOutput
    {
        public BeamBundleSummaryInfo bundle;
        public string description;
        public string docsUrl;
        public BeamBundleReleaseInfo latestRelease;
        public BeamBundleContentInfo latestContent;
        public System.Collections.Generic.Dictionary<string, string> componentGroupDescriptions;
    }
}
