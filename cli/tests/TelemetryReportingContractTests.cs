using System.Collections.Generic;
using System.Text;
using Beamable.Common.BeamCli;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Serialization.SmallerJSON;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace tests;

public class TelemetryReportingContractTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("value")]
    public void UnityTelemetryPayloadReadsThroughCliNewtonsoft(string value)
    {
        var attributes = value == null ? null : new Dictionary<string, string>();
        if (value != null) attributes["key"] = value;
        var message = new CliOtelMessage { allLogs = new List<CliOtelLogRecord> { new CliOtelLogRecord { Body = "body", LogLevel = "Information", Attributes = attributes } } };
        var json = Json.Serialize(message, new StringBuilder());
        var result = JsonConvert.DeserializeObject<CliOtelMessage>(json);
        Assert.That(result.allLogs[0].Body, Is.EqualTo("body"));
        if (value == null) Assert.That(result.allLogs[0].Attributes, Is.Null);
        else Assert.That(result.allLogs[0].Attributes["key"], Is.EqualTo(value));
    }

    [Test]
    public void ReportExamplePreservesObjectPayloadAndEnvelope()
    {
        var point = new ReportDataPoint { ts = 123, type = "stream", data = new { name = "example", nested = new[] { 1, 2 } } };
        var payload = JObject.Parse(JsonConvert.SerializeObject(point));
        Assert.That((long)payload["ts"], Is.EqualTo(123));
        Assert.That((string)payload["type"], Is.EqualTo("stream"));
        Assert.That((string)payload["data"]["name"], Is.EqualTo("example"));
        Assert.That(payload["data"]["nested"].ToObject<int[]>(), Is.EqualTo(new[] { 1, 2 }));
        var encoded = Reporting.EncodeMessage(payload.ToString(Newtonsoft.Json.Formatting.None));
        Assert.That(encoded, Does.StartWith(Reporting.PATTERN_START).And.EndWith(Reporting.PATTERN_END));
    }
}
