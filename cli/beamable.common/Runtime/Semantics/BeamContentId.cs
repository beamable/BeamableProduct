using System;
using Beamable.Common.BeamCli;
using Beamable.Common.Content;

namespace Beamable.Common.Semantics
{
    [CliContractType, Serializable]
    public struct BeamContentId : IBeamSemanticType<string>, IEquatable<string>, IEquatable<BeamContentId>
    {
        private string _value;

        public string SemanticName => "ContentId";
        
        public string AsString
        {
	        get => _value ?? string.Empty;
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
        public static implicit operator BeamContentId(string contentId) => new BeamContentId(contentId);
        public string ToJson()
        {
	        return $"\"{AsString}\"";
        }

        public bool Equals(string other)
        {
	        return other == AsString;
        }

        public bool Equals(BeamContentId other)
        {
	        return other.AsString == AsString;
        }

        public override string ToString()
        {
	        return AsString;
        }
    }
}
