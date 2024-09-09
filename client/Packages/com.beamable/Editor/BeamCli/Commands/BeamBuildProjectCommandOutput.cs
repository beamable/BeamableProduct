
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public class BeamBuildProjectCommandOutput
    {
        public string service;
        public BeamProjectErrorReport report;
    }
}
