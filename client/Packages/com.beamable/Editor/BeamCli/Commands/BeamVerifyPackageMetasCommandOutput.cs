
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamVerifyPackageMetasCommandOutput
    {
        public System.Collections.Generic.List<string> directoriesMissingMetaFiles;
        public int directoriesChecked;
    }
}
