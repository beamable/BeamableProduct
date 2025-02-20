namespace Beamable.Common
{
    public static partial class Constants
    {
        public static partial class Features
        {
            public static partial class Otel
            {

                public const string METER_NAME = "Beamable.Service.Core";
                
                public const string TRACE_WS = "beam.handleMessage";
                public const string TRACE_WS_BEAM = "beam.handleBeamRequest";
                public const string TRACE_WS_CLIENT = "beam.handleClientRequest";
                public const string TRACE_CONSTRUCT_CTX = "beam.parseRequestContext";

            }
        }
    }
}