using System;
using Beamable.Common.BeamCli;

namespace Beamable.Common.Semantics
{
    [CliContractType, Serializable, BeamSemanticType(BeamSemanticType.Pid)]
    public struct BeamPid : IBeamSemanticType<string>
    {
        private string _stringValue;

        public string AsString
        {
            get => _stringValue;
            set => _stringValue = value;
        }
        
        public BeamPid(string value)
        {
            _stringValue = value;
        }
        
        public static implicit operator string(BeamPid id) => id.AsString;
        public static implicit operator BeamPid(string value) => new BeamPid(value);
    }
}
