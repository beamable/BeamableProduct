
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamSiteResultInfo
    {
        public string site;
        public string state;
        public string error;
        public string manifestId;
    }
}
