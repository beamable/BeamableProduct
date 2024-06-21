
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public class BeamGetUnityVersionInfoCommandOutput
    {
        public string beamableNugetVersion;
        public string sdkVersion;
        public string packageFolder;
    }
}
