using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Server;
using microserviceTests.microservice.Util;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace microserviceTests.microservice.dbmicroservice;

[TestFixture]
public class ProcessHealthHttpTests : CommonTest
{
    [TestCase(true, true, "Healthy", false)]
    [TestCase(true, false, "Degraded", false)]
    [TestCase(false, true, "Healthy", false)]
    [TestCase(true, true, "Unhealthy", true)]
    public async Task RealHttp_PreservesLegacyContract_AndReportsAllInstanceObservation(
        bool legacyInitialized, bool secondConnected, string proposed, bool missHeartbeat)
    {
        using var socket = new RequesterHarness(_ => { });
        var service = new BeamableMicroService { InstanceArgs = new TestArgs() };
        typeof(BeamableMicroService).GetProperty(nameof(BeamableMicroService.HasInitialized))!
            .SetValue(service, legacyInitialized);
        SetField(service, "_webSocketPromise", Promise<IConnection>.Successful(socket.Socket));
        SetField(service, "_socketRequesterContext", socket.Context);
        var instances = new[]
        {
            new InstanceProcessHealth(0, true, true, true, 1, 0, 0, null, null, false),
            new InstanceProcessHealth(1, true, secondConnected, true, 1, 0, 0, null, null, false)
        };
        using var monitor = new MonitorOwner(new ProcessHealthMonitor(Array.Empty<IBeamableService>(), 2,
            new ProcessHealthOptions(), action => { if (!missHeartbeat) action(); }, _ => instances));
        var now = Stopwatch.GetTimestamp();
        monitor.Value.Check(now);
        if (missHeartbeat) monitor.Value.Check(now + 31 * Stopwatch.Frequency);
        var portReservation = new TcpListener(IPAddress.Loopback, 0);
        portReservation.Start();
        var port = ((IPEndPoint)portReservation.LocalEndpoint).Port;
        portReservation.Stop();
        using var server = new ContainerDiagnosticService(port, service, null, "health-test", monitor.Value);
        var running = server.Run();
        using var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{port}"), Timeout = TimeSpan.FromSeconds(3) };
        try
        {
            using var response = await GetWhenListening(client, "/health");
            Assert.That(response.StatusCode, Is.EqualTo(legacyInitialized ? HttpStatusCode.OK : HttpStatusCode.Conflict));
            Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo(legacyInitialized ? "\"Healthy: true\"" : "\"Healthy: false\""));
            Assert.That(response.Headers.GetValues("X-Beam-Process-Health"), Does.Contain(proposed));
            using var detail = await client.GetAsync("/health/process");
            Assert.That(detail.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var json = JObject.Parse(await detail.Content.ReadAsStringAsync());
            Assert.That((string)json["State"], Is.EqualTo(proposed));
            Assert.That((bool)json["ObservationOnly"], Is.True);
            Assert.That(((JArray)json["Instances"]).Count, Is.EqualTo(2));
            Assert.That((bool)json["Instances"]![1]!["Connected"], Is.EqualTo(secondConnected));
            using var incident = await client.GetAsync("/health/process/incident");
            var retained = JToken.Parse(await incident.Content.ReadAsStringAsync());
            Assert.That(retained.Type, Is.EqualTo(missHeartbeat ? JTokenType.Object : JTokenType.Null));
            if (missHeartbeat) Assert.That((string)retained["Reason"], Is.EqualTo("thread_pool_heartbeat_missed"));
        }
        finally
        {
            server.Dispose();
            await running.WaitAsync(TimeSpan.FromSeconds(2));
        }
    }

    private static void SetField(object service, string name, object value) =>
        typeof(BeamableMicroService).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(service, value);

    private static async Task<HttpResponseMessage> GetWhenListening(HttpClient client, string path)
    {
        var deadline = Stopwatch.StartNew();
        while (true)
        {
            try { return await client.GetAsync(path); }
            catch (HttpRequestException) when (deadline.Elapsed < TimeSpan.FromSeconds(2)) { await Task.Delay(20); }
        }
    }

    private sealed class MonitorOwner : IDisposable
    {
        internal readonly ProcessHealthMonitor Value;
        internal MonitorOwner(ProcessHealthMonitor value) => Value = value;
        public void Dispose() => Value.Stop();
    }
}
