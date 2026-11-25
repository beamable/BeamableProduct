using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Beamable.Api;
using Beamable.Common.BeamCli;
using Beamable.Common.Dependencies;
using Beamable.Editor.BeamCli.Commands;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Beamable.Editor.Tests
{
    public partial class ContentWatcherSwitchingTests
    {
        // The CLI wrappers and BeamEditorContext.SwitchRealm run normally. Supply realm configuration in memory.
        private sealed class RealmConfigCommands : IBeamCommandFactory
        {
            public HeldCommand FirstCommand;
            public string Pid = "target";

            public IBeamCommand Create()
            {
                if (FirstCommand == null)
                    return new HeldCommand(false, new BeamConfigCommandResult { cid = "1", pid = Pid }, true);
                var first = FirstCommand;
                FirstCommand = null;
                return first;
            }

            public void ClearAll() { }
        }

        private BeamEditorContext CreateRealmSwitchContext(RealmConfigCommands factory = null)
        {
            factory ??= new RealmConfigCommands();
            var requester = (PlatformRequester)FormatterServices.GetUninitializedObject(typeof(PlatformRequester));
            requester.Pid = "previous";
            var scope = new DependencyBuilder().AddSingleton<IBeamCommandFactory>(factory)
                .AddSingleton(requester).AddSingleton(service)
                .AddSingleton<BeamCli.BeamCli>(provider => new BeamCli.BeamCli(provider)).Build();
            var cli = scope.GetService<BeamCli.BeamCli>();
            cli.latestConfig = new BeamConfigCommandResult { cid = "1", pid = "previous" };
            cli.latestRealmInfo = new BeamRealmsListCommandOutput
            {
                VisibleRealms = new[]
                {
                    new BeamOrgRealmData { Cid = "1", Pid = "target", RealmName = "Target" },
                    new BeamOrgRealmData { Cid = "1", Pid = "next", RealmName = "Next" }
                }
            };
            var context = new BeamEditorContext();
            Set(context, "<ServiceScope>k__BackingField", scope);
            Set(service, "_beamContext", context);
            return context;
        }

        [UnityTest]
        public IEnumerator RealmChangeIsRaisedOnceAfterTheFirstContentSnapshot()
        {
            var context = CreateRealmSwitchContext();
            var notified = new List<string>();
            context.OnRealmChange += realm => notified.Add(realm.Pid);
            commands.DelayInitialContent = true;
            var switching = context.SwitchRealm("target");
            Assert.That(context.Requester.Pid, Is.EqualTo("target"));
            Assert.That(notified, Is.Empty);
            commands.Content.Single().Emit();
            yield return Wait(switching);
            switching.GetResult();
            Assert.That(notified, Is.EqualTo(new[] { "target" }));
            Assert.That(context.IsSwitchingRealms, Is.False);
        }

        [UnityTest]
        public IEnumerator RealmChangeIsRaisedOnceOnTimeoutAndCallerReceivesContentFailure()
        {
            var context = CreateRealmSwitchContext();
            var notified = new List<string>();
            context.OnRealmChange += realm =>
            {
                notified.Add(realm.Pid);
                throw new InvalidOperationException("Controlled realm listener failure");
            };
            double now = 0;
            service.ContentWatcherClock = () => now;
            commands.DelayInitialContent = true;
            Exception error = null;
            var switching = context.SwitchRealm("target").Error(e => error = e);
            Assert.That(context.Requester.Pid, Is.EqualTo("target"));
            Assert.That(notified, Is.Empty);
            LogAssert.Expect(LogType.Exception, new Regex("Controlled realm listener failure"));
            now = 31;
            yield return Wait(switching);
            Assert.That(error, Is.TypeOf<TimeoutException>());
            Assert.That(notified, Is.EqualTo(new[] { "target" }));
            Assert.That(context.IsSwitchingRealms, Is.False);
        }

        [UnityTest]
        public IEnumerator OverlappingRealmSwitchesNotifyInOrderWhileEachRealmIsCurrent()
        {
            var factory = new RealmConfigCommands();
            var context = CreateRealmSwitchContext(factory);
            var notifications = new List<(string payload, string cli, string requester)>();
            context.OnRealmChange += realm => notifications.Add((realm.Pid, context.BeamCli.CurrentRealm.Pid, context.Requester.Pid));
            commands.DelayInitialContent = true;
            var first = context.SwitchRealm("target");
            var watcher = commands.Content.Single();
            factory.Pid = "next";
            var second = context.SwitchRealm("next");
            Assert.That(context.Requester.Pid, Is.EqualTo("target"));
            Assert.That(watcher.Cancelled, Is.False);
            watcher.OnCancel = () => watcher.Completion.CompleteSuccess();
            watcher.Emit();
            yield return Wait(first);
            first.GetResult();
            Assert.That(notifications, Is.EqualTo(new[] { ("target", "target", "target") }));
            Assert.That(context.IsSwitchingRealms, Is.True);
            commands.Content.Last().Emit();
            yield return Wait(second);
            second.GetResult();
            Assert.That(notifications, Is.EqualTo(new[] { ("target", "target", "target"), ("next", "next", "next") }));
            Assert.That(context.IsSwitchingRealms, Is.False);
        }

        [UnityTest]
        public IEnumerator FailedCliRealmSwitchLeavesRequesterAndSubscribersOnPreviousRealm()
        {
            var configCommand = new HeldCommand(false, null) { DelayInitial = true };
            var context = CreateRealmSwitchContext(new RealmConfigCommands { FirstCommand = configCommand });
            int notifications = 0;
            context.OnRealmChange += _ => notifications++;
            Exception error = null;
            var switching = context.SwitchRealm("target").Error(e => error = e);
            var failure = new IOException("Controlled CLI config failure");
            configCommand.Completion.CompleteError(failure);
            yield return Wait(switching);
            Assert.That(error, Is.SameAs(failure));
            Assert.That(context.Requester.Pid, Is.EqualTo("previous"));
            Assert.That(commands.Content, Is.Empty);
            Assert.That(notifications, Is.Zero);
            Assert.That(context.IsSwitchingRealms, Is.False);
        }

        [UnityTest]
        public IEnumerator QueuedRealmSwitchRunsAfterPreviousCliFailure()
        {
            var configCommand = new HeldCommand(false, null) { DelayInitial = true };
            var context = CreateRealmSwitchContext(new RealmConfigCommands { FirstCommand = configCommand, Pid = "next" });
            var notified = new List<string>();
            context.OnRealmChange += realm => notified.Add(realm.Pid);
            var first = context.SwitchRealm("target");
            Exception firstError = null;
            first.Error(error => firstError = error);
            var second = context.SwitchRealm("next");
            Assert.That(context.Requester.Pid, Is.EqualTo("previous"));
            var failure = new IOException("Controlled config failure");
            configCommand.Completion.CompleteError(failure);
            yield return Wait(second);
            second.GetResult();
            Assert.That(firstError, Is.SameAs(failure));
            Assert.That(notified, Is.EqualTo(new[] { "next" }));
            Assert.That(context.IsSwitchingRealms, Is.False);
        }

        [UnityTest]
        public IEnumerator LogoutCancelsQueuedRealmSwitchBeforeItApplies()
        {
            var factory = new RealmConfigCommands();
            var context = CreateRealmSwitchContext(factory);
            commands.DelayInitialContent = true;
            var first = context.SwitchRealm("target");
            Exception queuedError = null;
            var second = context.SwitchRealm("next").Error(error => queuedError = error);
            // Hold the logout command so the fixture never reloads the editor's global environment.
            factory.FirstCommand = new HeldCommand(false, null) { DelayInitial = true };
            context.Logout(true);
            Assert.That(context.IsSwitchingRealms, Is.False);
            commands.Content.Single().Emit();
            yield return Wait(first);
            yield return Wait(second);
            Assert.That(queuedError, Is.TypeOf<OperationCanceledException>());
            Assert.That(commands.Content.Count, Is.EqualTo(1));
        }
    }
}
