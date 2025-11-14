
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamDeploymentDiffSummary
    {
        public System.Collections.Generic.List<BeamDeploymentManifestJsonDiff> jsonChanges;
        public System.Collections.Generic.List<string> addedServices;
        public System.Collections.Generic.List<string> removedServices;
        public System.Collections.Generic.List<string> disabledServices;
        public System.Collections.Generic.List<string> enabledServices;
        public System.Collections.Generic.List<string> addedStorage;
        public System.Collections.Generic.List<string> removedStorage;
        public System.Collections.Generic.List<string> disabledStorages;
        public System.Collections.Generic.List<string> enabledStorages;
        public System.Collections.Generic.List<BeamServiceLogProviderChange> servicesSwitchingLogProvider;
        public System.Collections.Generic.List<BeamServiceFederationChange> addedFederations;
        public System.Collections.Generic.List<BeamServiceFederationChange> removedFederations;
        public System.Collections.Generic.List<BeamServiceImageIdChange> serviceImageIdChanges;
    }
}
