using System;
using Beamable.Common.BeamCli;

namespace Beamable.Common.Semantics
{
    [CliContractType, Serializable]
    public struct BeamGamerTag : IBeamSemanticType<long>
    {
        private long _longValue;
        private string _stringValue;
        
        public string SemanticName => "GamerTag";
        
        public long AsLong
        {
            get => _longValue;
            set {
                _longValue = value;
                _stringValue = value.ToString();
            }
        }

        public string AsString
        {
	        get => string.IsNullOrEmpty(_stringValue) ? _longValue.ToString() : _stringValue;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException($"Parameter {nameof(value)} cannot be null or empty.");
                }
                _stringValue = value;
                _longValue = long.TryParse(value, out var longValue)
                    ? longValue
                    : throw new ArgumentException($"Parameter {nameof(value)} is invalid. Must be a numeric value.");
            }
        }

        public BeamGamerTag(long value)
        {
            _longValue = value;
            _stringValue = value.ToString();
        }

        public BeamGamerTag(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException($"Parameter {nameof(value)} cannot be null or empty.");
            }
            _stringValue = value;
            _longValue = long.TryParse(value, out var longValue)
                ? longValue
                : throw new ArgumentException($"Parameter {nameof(value)} is invalid. Must be a numeric value.");
        }
        
        public static implicit operator string(BeamGamerTag tag) => tag.AsString;
        public static implicit operator long(BeamGamerTag tag) => tag.AsLong;
        
        public static implicit operator BeamGamerTag(string value) => new BeamGamerTag(value);
        public static implicit operator BeamGamerTag(long value) => new BeamGamerTag(value);
        public string ToJson()
        {
	        return AsString;
        }
    }
}
