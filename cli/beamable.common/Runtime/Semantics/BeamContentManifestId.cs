using System;
using Beamable.Common.BeamCli;

namespace Beamable.Common.Semantics
{
    [CliContractType, Serializable, BeamSemanticType(BeamSemanticType.ContentManifestId)]
    public struct BeamContentManifestId : IBeamSemanticType<string>
    {
        private string _value;

        public string AsString
        {
            get => _value;
            set => _value = value;
        }

        public BeamContentManifestId(string value)
        {
            _value = value;
        }
        
        public static implicit operator string(BeamContentManifestId id) => id.AsString;
        public static implicit operator BeamContentManifestId(string value) => new BeamContentManifestId(value);
    }
}
