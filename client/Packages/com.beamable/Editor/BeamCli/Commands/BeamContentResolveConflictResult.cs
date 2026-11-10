
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamContentResolveConflictResult
    {
        public string[] ResolvedContentIds;
        public Beamable.Common.BeamCli.Contracts.ContentReconciliationPreview Preview;
    }
}
