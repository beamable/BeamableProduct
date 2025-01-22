
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamManifestServiceEntry
    {
        public string beamoId;
        public bool shouldBeEnabledOnRemote;
        public string csprojPath;
        public string buildDllPath;
        public System.Collections.Generic.List<string> storageDependencies;
        public System.Collections.Generic.List<BeamUnityAssemblyReferenceData> unityReferences;
        public System.Collections.Generic.List<BeamFederationEntry> federations;
    }
}
