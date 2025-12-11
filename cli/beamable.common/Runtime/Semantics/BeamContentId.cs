using System;
using Beamable.Common.BeamCli;
using Beamable.Common.Content;

namespace Beamable.Common.Semantics
{
    [CliContractType, Serializable, BeamSemanticType(BeamSemanticType.ContentId)]
    public struct BeamContentId : IBeamSemanticType<string>
    {
        private string _value;

        public string AsString
        {
            get => _value;
            set => _value = value;
        }

        public BeamContentId(string value)
        {
            _value = value;
        }
        
        public BeamContentId(ContentRef contentRef) : this(contentRef.GetId()) { }
        public BeamContentId(ContentObject contentObject) : this(contentObject.Id) { }
        
        public static implicit operator string(BeamContentId contentId) => contentId.AsString;
        public static implicit operator BeamContentId(ContentRef contentRef) => new BeamContentId(contentRef);
        public static implicit operator BeamContentId(ContentObject contentObject) => new BeamContentId(contentObject);
    }
}
