
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamSitePlanInfo
    {
        public string site;
        public bool isZone;
        public string[] changes;
    }
}
