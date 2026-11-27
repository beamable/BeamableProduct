
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamAcquireBundleCommandOutput
    {
        public string name;
        public bool acquired;
        public string[] refusals;
        public string[] missingHardDependencies;
        public string[] suggestedDependencies;
        public string[] warnings;
    }
}
