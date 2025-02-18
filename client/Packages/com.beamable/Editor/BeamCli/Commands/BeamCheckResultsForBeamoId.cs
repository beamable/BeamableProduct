
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamCheckResultsForBeamoId
    {
        public string beamoId;
        public System.Collections.Generic.List<BeamRequiredFileEdit> fileEdits;
    }
}
