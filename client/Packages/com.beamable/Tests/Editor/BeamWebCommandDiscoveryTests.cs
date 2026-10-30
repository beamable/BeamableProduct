using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Beamable.Common;
using Beamable.Common.BeamCli;
using Beamable.Editor.BeamCli;
using Beamable.Editor.BeamCli.Commands;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;

namespace Beamable.Editor.Tests
{
    public class BeamWebCommandDiscoveryTests
    {
        private readonly List<BeamWebCommandStopTests.Fixture> _fixtures = new List<BeamWebCommandStopTests.Fixture>();
        private Harness _harness;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _harness?.FinishCommands();
            yield return BeamWebCommandStopTests.CleanupFixtures(_fixtures);
        }

        private static readonly string[] VersionCases =
        {
            "0.0.123|0.0.123.1|Match",
            "0.0.123|0.0.123.25|Match",
            "0.0.123|0.0.123|Match",
            "0.0.123|0.0.1234.1|Mismatch",
            "0.0.123|0.0.123.invalid|Mismatch",
            "0.0.123|0.0.123.1-NIGHTLY|Mismatch",
            "0.0.123.1|0.0.123.1|Match",
            "0.0.123.1|0.0.123.2|Mismatch",
            "0.0.123.1|0.0.123|Mismatch",
            "7.3.0|7.3.0|Match",
            "7.3.0|7.3.1|Mismatch"
        };

        [UnityTest]
        public IEnumerator DiscoveryRespectsInstalledVersionConstraint([ValueSource(nameof(VersionCases))] string scenario)
        {
            // The installer explicitly expands only 0.0.123 into 0.0.123.*.
            // These responses are independent of Factory.Version, unlike the ordinary fixture default.
            var values = scenario.Split('|');
            var server = new BeamWebCommandStopTests.Fixture(_fixtures)
            {
                InfoPids = new[] { System.Diagnostics.Process.GetCurrentProcess().Id },
                InfoVersion = values[1]
            };
            var options = (BeamWebCommandFactoryOptions)typeof(BeamWebCommandFactory)
                .GetField("_options", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(server.Factory);
            options.versionOverride.Set(values[0]);
            var ping = server.Factory.PingServer();
            yield return WaitFor(() => ping.IsCompleted);
            Assert.That(ping.GetResult().ToString(), Is.EqualTo(values[2]));
        }

        [UnityTest]
        public IEnumerator RejectedOwnedLaunchFailsAllCallersAndDoesNotAccumulateOnRetry()
        {
            var h = _harness = new Harness(_fixtures);
            var first = h.Request();
            var second = h.Request();
            yield return WaitFor(() => first.IsCompleted || h.Commands.Launched.Count > 1);
            Assert.That(first.IsFailed && second.IsFailed, Is.True, "Reject the discovery batch instead of launching another child.");
            Assert.That(h.Commands.Launched.Count, Is.EqualTo(1));
            var retry = h.Request();
            yield return WaitFor(() => retry.IsCompleted || h.Commands.Launched.Count > 1);
            Assert.That(retry.IsFailed, Is.True);
            Assert.That(h.Commands.Launched.Count, Is.EqualTo(1));
            Assert.That(h.Factory.port, Is.EqualTo(h.ServerPort), "Retry must revisit the owned endpoint.");
        }

        [UnityTest]
        public IEnumerator CorrectedIdentityAllowsReentrantRetryOfTheExistingLaunch()
        {
            var h = _harness = new Harness(_fixtures);
            Promise retry = null;
            h.Request().Error(_ =>
            {
                h.Server.InfoVersion = null;
                retry = h.Request();
            });
            yield return WaitFor(() => retry?.IsCompleted == true || h.Commands.Launched.Count > 1);
            Assert.That(retry, Is.Not.Null, "A rejected discovery must notify its caller.");
            Assert.That(retry.IsFailed, Is.False);
            Assert.That(h.Commands.Launched.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator UnresponsiveOwnedLaunchIsRetainedUntilItsCommandCompletes()
        {
            var h = _harness = new Harness(_fixtures);
            var first = h.Request();
            yield return WaitFor(() => first.IsCompleted || h.Commands.Launched.Count > 1);
            Assert.That(first.IsFailed, Is.True);
            h.Server.InfoStatus = 503;
            var unavailable = h.Request();
            yield return WaitFor(() => unavailable.IsCompleted || h.Commands.Launched.Count > 1);
            Assert.That(unavailable.IsFailed, Is.True);
            Assert.That(h.Commands.Launched.Count, Is.EqualTo(1));

            h.Commands.Launched[0].Completion.CompleteSuccess();
            h.Commands.BeforeLaunch = () => { h.Server.InfoStatus = 200; h.Server.InfoVersion = null; };
            var replacement = h.Request();
            yield return WaitFor(() => replacement.IsCompleted);
            Assert.That(replacement.IsFailed, Is.False);
            Assert.That(h.Commands.Launched.Count, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator ForeignMismatchStillAdvancesDiscoveryWithoutClaimingThatServer()
        {
            var h = _harness = new Harness(_fixtures, startAtServer: true);
            h.Request();
            yield return WaitFor(() => h.Factory.port != h.ServerPort);
            Assert.That(h.Factory.port, Is.EqualTo(h.ServerPort + 1));
            Assert.That(h.Commands.Launched, Is.Empty);
        }

        private static IEnumerator WaitFor(Func<bool> done)
        {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (!done() && DateTime.UtcNow < deadline)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
            Assert.That(done(), Is.True, "Discovery did not reach the expected boundary.");
        }

        private sealed class Harness
        {
            public readonly BeamWebCommandStopTests.Fixture Server;
            public BeamWebCommandFactory Factory => Server.Factory;
            public readonly HeldServerCommands Commands;
            public readonly int ServerPort;

            public Harness(List<BeamWebCommandStopTests.Fixture> fixtures, bool startAtServer = false)
            {
                Server = new BeamWebCommandStopTests.Fixture(fixtures)
                {
                    InfoPids = Enumerable.Repeat(System.Diagnostics.Process.GetCurrentProcess().Id, 64).ToArray(),
                    InfoVersion = "incompatible-version"
                };
                ServerPort = Factory.port;
                Commands = new HeldServerCommands(ServerPort);
                Factory.processCommands = Commands;
                if (!startAtServer)
                {
                    var reservation = new TcpListener(IPAddress.Loopback, 0);
                    reservation.Start();
                    Factory.port = ((IPEndPoint)reservation.LocalEndpoint).Port;
                    reservation.Stop();
                }
                var loop = (IEnumerator)typeof(BeamWebCommandFactory)
                    .GetMethod("ServerDiscoveryLoop", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Factory, null);
                Factory.dispatcher.Run("discovery-regression", loop);
            }

            public Promise Request()
            {
                var request = Factory.EnsureServerIsRunning();
                request.Error(_ => { });
                return request;
            }

            public void FinishCommands()
            {
                // The fake launch models the lifetime of the real command, without starting processes.
                foreach (var command in Commands.Launched) command.Completion.CompleteSuccess();
            }
        }

        private sealed class HeldServerCommands : BeamCommands
        {
            public readonly List<HeldServerCommand> Launched = new List<HeldServerCommand>();
            public Action BeforeLaunch;
            private readonly int _port;
            public HeldServerCommands(int port) : base(null, null) { _port = port; }
            public override ServerServeWrapper ServerServe(ServerServeArgs args)
            {
                BeforeLaunch?.Invoke();
                var command = new HeldServerCommand(_port);
                Launched.Add(command);
                return new ServerServeWrapper { Command = command };
            }
        }

        private sealed class HeldServerCommand : IBeamCommand
        {
            public readonly Promise Completion = new Promise();
            private readonly BeamServeCliCommandOutput _ready;
            private Action _emit = () => { };
            public HeldServerCommand(int port) { _ready = new BeamServeCliCommandOutput { port = port, uri = "http://127.0.0.1:" + port }; }
            public void SetCommand(string command) { }
            public Promise Run() { _emit(); return Completion; }
            public void Cancel() => Completion.CompleteSuccess();
            public IBeamCommand On<T>(string type, Action<ReportDataPoint<T>> callback)
            {
                if (type == "stream" && _ready is T value) _emit += () => callback(new ReportDataPoint<T> { data = value });
                return this;
            }
            public IBeamCommand On(Action<ReportDataPointDescription> callback) => this;
            public IBeamCommand OnError(Action<ReportDataPoint<ErrorOutput>> callback) => this;
            public IBeamCommand OnTerminate(Action<ReportDataPoint<EofOutput>> callback) => this;
        }
    }
}
