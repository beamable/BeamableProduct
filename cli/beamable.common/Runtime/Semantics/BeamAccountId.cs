using Beamable.Common.BeamCli;
using System;

namespace Beamable.Common.Semantics
{
    [CliContractType, Serializable, BeamSemanticType(BeamSemanticType.AccountId)]
    public struct BeamAccountId : IBeamSemanticType<long>
    {
        private long _longValue;
        private string _stringValue;
        
        public long AsLong
        {
            get => _longValue;
            set
            {
                _longValue = value;
                _stringValue = value.ToString();
            }
        }

        public string AsString
        {
            get => _stringValue;
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

        public BeamAccountId(long value)
        {
            _longValue = value;
            _stringValue = value.ToString();
        }

        public BeamAccountId(string value)
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
        
        public static implicit operator string(BeamAccountId id) => id.AsString;
        public static implicit operator long(BeamAccountId id) => id.AsLong;
        
        public static implicit operator BeamAccountId(string value) => new BeamAccountId(value);
        public static implicit operator BeamAccountId(long value) => new BeamAccountId(value);
    }
}
