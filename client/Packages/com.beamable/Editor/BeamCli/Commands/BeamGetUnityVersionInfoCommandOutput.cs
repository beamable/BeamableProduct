
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamGetUnityVersionInfoCommandOutput
    {
        public string beamableNugetVersion;
        public string sdkVersion;
        public string packageFolder;
    }
}
