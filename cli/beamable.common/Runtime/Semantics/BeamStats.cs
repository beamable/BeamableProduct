using System;
using Beamable.Common.Api.Stats;
using Beamable.Common.BeamCli;

namespace Beamable.Common.Semantics
{
    [CliContractType, Serializable, BeamSemanticType(BeamSemanticType.StatsType)]
    public struct BeamStats : IBeamSemanticType<string>
    {
        private string _value;

        public string AsString
        {
            get => _value;
            set => _value = value;
        }

        public BeamStats(StatsDomainType domainType, StatsAccessType accessType, long userId)
        {
            _value = StatsApiHelper.GeneratePrefix(domainType, accessType, userId);
        }

        public BeamStats(string value)
        {
            _value = value;
        }
        
        public static implicit operator string(BeamStats stats) => stats.AsString;

        public static implicit operator BeamStats((StatsDomainType, StatsAccessType, long) tuple)
        {
            return new BeamStats(StatsApiHelper.GeneratePrefix(tuple.Item1, tuple.Item2, tuple.Item3));
        }
    }
}
