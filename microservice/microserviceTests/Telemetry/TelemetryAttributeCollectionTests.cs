using Beamable.Server;
using NUnit.Framework;

namespace microserviceTests.Telemetry;

public class TelemetryAttributeCollectionTests
{
    [Test]
    public void CannotHaveDuplicates()
    {
        var collection = new TelemetryAttributeCollection();
        collection.Add(TelemetryAttributes.Cid("123"));

        Assert.Throws<DuplicateTelemetryAttributeException>(() =>
        {
            collection.Add(TelemetryAttributes.Cid("123"));
        });

    }
}