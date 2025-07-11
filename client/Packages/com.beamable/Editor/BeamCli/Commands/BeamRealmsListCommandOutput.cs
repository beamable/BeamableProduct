
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    [System.SerializableAttribute()]
    public partial class BeamRealmsListCommandOutput
    {
        public string CustomerAlias;
        public string Cid;
        public BeamOrgRealmData[] VisibleRealms;
    }
}
