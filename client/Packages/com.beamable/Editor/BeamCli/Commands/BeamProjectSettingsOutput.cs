
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamProjectSettingsOutput
    {
        public string serviceName;
        public System.Collections.Generic.List<BeamSettingOutput> settings;
    }
}
