using System;
using Beamable.Common.BeamCli;

namespace Beamable.Common.Semantics
{
    [CliContractType, Serializable]
    public struct BeamCid : IBeamSemanticType<long>, IEquatable<string>, IEquatable<long>, IEquatable<BeamCid>
    {
	    private long _longValue;
        private string _stringValue;
        
        public string SemanticName => "Cid";
        
        public long AsLong {
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

        public BeamCid(long value)
        {
            _longValue = value;
            _stringValue = value.ToString();
        }

        public BeamCid(string value)
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

        public static implicit operator string(BeamCid cid) => cid.AsString;
        public static implicit operator long(BeamCid cid) => cid.AsLong;

        public static implicit operator BeamCid(string value) => new BeamCid(value);
        public static implicit operator BeamCid(long value) => new BeamCid(value);
        public string ToJson()
        {
	        return AsString;
        }

        public bool Equals(string other)
        {
	        return other == AsString;
        }

        public bool Equals(long other)
        {
	        return other == AsLong;
        }

        public bool Equals(BeamCid other)
        {
	        return other.AsLong == AsLong;
        }

        public override string ToString()
        {
	        return AsString;
        }
    }
}
