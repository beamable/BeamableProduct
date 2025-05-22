using OpenTelemetry.Trace;

namespace Beamable.Server;

public class TraceSampler : Sampler
{
    // TODO: we need to control the tracing level based on realm config. 
    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        if (samplingParameters.Tags == null) return new SamplingResult(true);
        
        var levelTag = samplingParameters.Tags.FirstOrDefault((kvp) => kvp.Key == "LEVEL");

        if (string.IsNullOrEmpty(levelTag.Key)) return new SamplingResult(true);

        var value = levelTag.Value as string;
        if (value == "IGNORE")
        {
            return new SamplingResult(false);
        }

        return new SamplingResult(true);
    }
}