using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using Beamable.Common;
using Beamable.Content;
using Beamable.Common.Api.Realms;
using Beamable.Common.BeamCli;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
using Beamable.Common.Content.Serialization;
using Beamable.Common.Content.Validation;
using Beamable.Common.Dependencies;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.ContentService;
using NUnit.Framework;
using UnityEngine;

namespace Beamable.Editor.Tests
{
    // Characterization tests: assertions describe current behavior, not desired Local Mode behavior.
    // Uses the production serializer and editor loader, capturing the outgoing CLI command.
    // No CLI process, credentials, realm request, or user content directory is accessed.
    public class Issue4866UnityReproTests
    {
        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        const string Id = "issue4866Old.test";
        CliContentService _service;
        CapturingCommands _commands;
        ContentConfiguration _config;
        Issue4866OldContent _old;
        Issue4866NewContent _new;
        string _path;

        [SetUp]
        public void Setup()
        {
            _commands = new CapturingCommands();
            var cli = new BeamCli.BeamCli(null)
            {
                latestConfig = new BeamConfigCommandResult { cid = "test", pid = "realm" },
                pidToRealm = new Dictionary<string, RealmView> { ["realm"] = new RealmView { Cid = "test", Pid = "realm" } }
            };
            var provider = new DependencyBuilder().AddSingleton(cli).AddSingleton(new ValidationContext()).Build();
            var context = new BeamEditorContext();
            Set(context, "<ServiceScope>k__BackingField", provider);
            // Bypass real watcher/auth initialization, as ContentSaveRecoveryTests does.
            _service = (CliContentService)FormatterServices.GetUninitializedObject(typeof(CliContentService));
            foreach (var field in typeof(CliContentService).GetFields(Fields))
                if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                    field.SetValue(_service, Activator.CreateInstance(field.FieldType));
            Set(_service, "_cli", _commands);
            Set(_service, "_beamContext", context);
            Set(_service, "_provider", provider);
            Set(_service, "_contentWrites", new ContentWriteTracker());
            Set(_service, "_contentTypeReflectionCache", BeamEditor.GetReflectionSystem<ContentTypeReflectionCache>());
            _config = ScriptableObject.CreateInstance<ContentConfiguration>();
            _config.validateSchemaDifference = true;
            Set(_service, "_contentConfiguration", _config);
            _service.manifestIdOverride = "global";
            _old = ScriptableObject.CreateInstance<Issue4866OldContent>();
            _new = ScriptableObject.CreateInstance<Issue4866NewContent>();
            _old.SetIdAndVersion(Id, "");
            _new.SetIdAndVersion(Id, "");
            _new.damage = 20;
            _new.newModelOnly = 37;
            _path = Path.Combine(Path.GetTempPath(), "beam-4866-" + Guid.NewGuid().ToString("N") + ".json");
        }

        [TearDown]
        public void Teardown()
        {
            _old.OnEditorChanged = null;
            _old.CancelPendingEditorChangeNotification();
            _new.CancelPendingEditorChangeNotification();
            UnityEngine.Object.DestroyImmediate(_old);
            UnityEngine.Object.DestroyImmediate(_new);
            UnityEngine.Object.DestroyImmediate(_config);
            if (File.Exists(_path)) File.Delete(_path);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void NewModelJsonLoadedByOldModel_OnlyValidationEnabledIssuesLossySave(bool validationEnabled)
        {
            _config.validateSchemaDifference = validationEnabled;
            var producerJson = ClientContentSerializer.SerializeContent(_new);
            Assert.That(producerJson, Does.Contain("newModelOnly"));
            File.WriteAllText(_path, producerJson);
            Invoke("LoadContentObject", Entry(), _old);
            Assert.That(_old.damage, Is.EqualTo(20));
            Assert.That(_commands.Saves.Count, Is.EqualTo(validationEnabled ? 1 : 0));
            if (validationEnabled)
            {
                Assert.That(_commands.Saves[0].contentIds, Is.EqualTo(new[] { Id }));
                Assert.That(_commands.Saves[0].contentProperties[0], Does.Contain("damage"));
                Assert.That(_commands.Saves[0].contentProperties[0], Does.Not.Contain("newModelOnly"));
                TestContext.WriteLine("CONFIRMED command-boundary loss: producer newModelOnly=37 is absent from automatic ContentSave payload.");
            }
            // Capture-only commands deliberately do not write: this assertion bounds the evidence.
            Assert.That(File.ReadAllText(_path), Is.EqualTo(producerJson));
        }

        [Test]
        public void SameModelJsonDoesNotScheduleAutomaticSave()
        {
            _old.damage = 20;
            File.WriteAllText(_path, ClientContentSerializer.SerializeContent(_old));
            Invoke("LoadContentObject", Entry(), _old);
            Assert.That(_commands.Saves, Is.Empty);
        }

        [Test]
        public void UnknownContentTypeIsNotAddedToInvalidContentCache()
        {
            var entry = Entry();
            entry.TypeName = "issue4866Unregistered";
            entry.FullId = entry.TypeName + ".test";
            Invoke("CacheScriptableContent", entry);
            Assert.That(_service.HasInvalidContent, Is.False);
            Assert.That(((Dictionary<string, ContentObject>)Get(_service, "_contentScriptableCache")).ContainsKey(entry.FullId), Is.False);
            Assert.That(_commands.Saves, Is.Empty);
            TestContext.WriteLine("CONFIRMED unknown type is skipped by object cache without an invalid-content marker.");
        }

        LocalContentManifestEntry Entry() => new LocalContentManifestEntry
        {
            FullId = Id, Name = "test", TypeName = "issue4866Old", JsonFilePath = _path,
            CurrentStatus = (int)ContentStatus.UpToDate, Tags = Array.Empty<string>()
        };
        object Invoke(string method, params object[] args) => typeof(CliContentService).GetMethod(method, Fields).Invoke(_service, args);
        static object Get(object instance, string name) => instance.GetType().GetField(name, Fields).GetValue(instance);
        static void Set(object instance, string name, object value) => instance.GetType().GetField(name, Fields).SetValue(instance, value);

        class CapturingCommands : BeamCommands
        {
            public readonly List<ContentSaveArgs> Saves = new List<ContentSaveArgs>();
            public CapturingCommands() : base(null, null) { }
            public override ContentSaveWrapper ContentSave(ContentSaveArgs args)
            {
                Saves.Add(args);
                return new ContentSaveWrapper { Command = new CompletedCommand() };
            }
            public override ContentTagSetWrapper ContentTagSet(ContentTagSetArgs args) =>
                new ContentTagSetWrapper { Command = new CompletedCommand() };
        }
        class CompletedCommand : IBeamCommand
        {
            public void SetCommand(string command) { }
            public Promise Run() => Promise.Success;
            public void Cancel() { }
            public IBeamCommand On<T>(string type, Action<ReportDataPoint<T>> callback) => this;
            public IBeamCommand On(Action<ReportDataPointDescription> callback) => this;
            public IBeamCommand OnError(Action<ReportDataPoint<ErrorOutput>> callback) => this;
            public IBeamCommand OnTerminate(Action<ReportDataPoint<EofOutput>> callback) => this;
        }
    }

    [ContentType("issue4866Old")]
    public class Issue4866OldContent : ContentObject { public int damage; }
    [ContentType("issue4866New")]
    public class Issue4866NewContent : ContentObject { public int damage; public int newModelOnly; }
}
