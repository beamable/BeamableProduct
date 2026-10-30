using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Content;
using Beamable.Editor.ContentService;
using Beamable.Tests.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Beamable.Editor.Tests
{
	public class LocalContentSessionTests
	{
		private ContentConfiguration _config;
		private readonly List<EditorLocalContentSession> _sessions = new List<EditorLocalContentSession>();

		[SetUp]
		public void SetUp()
		{
			// A transient object only: never read or modify ContentConfiguration.Instance.
			_config = ScriptableObject.CreateInstance<ContentConfiguration>();
			_config.EnableLocalContentInEditor = true;
			_config.EditorManifestID = "local-test";
		}

		[TearDown]
		public void TearDown()
		{
			foreach (var session in _sessions) session.OnDispose();
			_sessions.Clear();
			UnityEngine.Object.DestroyImmediate(_config);
		}

		[Test]
		public void EditorRegistrationReplacesDefaultWithoutInstantiatingUserConfiguration()
		{
			IDependencyBuilder builder = new DependencyBuilder();
			builder.AddSingleton<ILocalContentSource, DisabledLocalContentSource>();
			var before = builder.GetSingletonServices().Single(s => s.Interface == typeof(ILocalContentSource));
			Assert.That(before.Implementation, Is.EqualTo(typeof(DisabledLocalContentSource)));
			var disabled = builder.Build().GetService<ILocalContentSource>();
			Assert.That(disabled, Is.InstanceOf<DisabledLocalContentSource>());
			Assert.That(disabled.HandlesManifest("global"), Is.False);
			EditorLocalContentRegistration.Register(builder);
			var after = builder.GetSingletonServices().Single(s => s.Interface == typeof(ILocalContentSource));
			Assert.That(after, Is.Not.SameAs(before));
			Assert.That(after.Factory, Is.Not.Null);
			// Deliberately do not execute the production factory: it reads the user's singleton configuration.
		}

		[TestCase(false, "local-test")]
		[TestCase(true, "other")]
		public void UnhandledNamespaceFailsBeforeAccessingEditorReadiness(bool enabled, string manifestId)
		{
			_config.EnableLocalContentInEditor = enabled;
			var session = Session();
			Assert.That(session.HandlesManifest(manifestId), Is.False);
			Assert.That(Failure(session.GetManifest(manifestId)), Is.InstanceOf<InvalidOperationException>());
			Assert.That(Failure(session.GetContent("local.item", typeof(ContentObject), manifestId)), Is.InstanceOf<InvalidOperationException>());
		}

		[Test]
		public void SessionCapturesModeAndNamespaceUntilContextIsRecreated()
		{
			var session = Session();
			_config.EnableLocalContentInEditor = false;
			_config.EditorManifestID = "other";
			Assert.That(session.HandlesManifest("local-test"), Is.True);
			Assert.That(session.HandlesManifest("other"), Is.False);
			Assert.That(Session().HandlesManifest("local-test"), Is.False);
		}

		[Test]
		public void DisposeIsIdempotentAndRejectsFurtherRequests()
		{
			var session = Session();
			session.OnDispose();
			Assert.DoesNotThrow(() => session.OnDispose());
			Assert.That(Failure(session.GetManifest("local-test")), Is.InstanceOf<ObjectDisposedException>());
			Assert.That(Failure(session.GetContent("local.item", typeof(ContentObject), "local-test")), Is.InstanceOf<ObjectDisposedException>());
		}

		[Test]
		public void ParameterProviderCanOverrideGlobalLocalModeSetting()
		{
			_config.EnableLocalContentInEditor = false;
			var session = new EditorLocalContentSession(_config, new TestConfigProvider(), true);
			_sessions.Add(session);
			Assert.That(session.HandlesManifest("local-test"), Is.True);
		}

		[UnityTest]
		public IEnumerator DisposeCancelsSimulatedDelay()
		{
			var session = Session();
			var waiting = (Task)typeof(EditorLocalContentSession)
				.GetMethod("WaitForDelay", BindingFlags.Instance | BindingFlags.NonPublic)
				.Invoke(session, new object[] { 60f });
			Assert.That(waiting.IsCompleted, Is.False);
			session.OnDispose();
			yield return WaitForCompletion(waiting);
			Assert.That(waiting.IsCanceled, Is.True);
		}

		[TestCase(0f)]
		[TestCase(-1f)]
		[TestCase(float.NaN)]
		[TestCase(float.PositiveInfinity)]
		public void UnsetOrInvalidDelayDoesNotBlock(float seconds)
		{
			var waiting = (Task)typeof(EditorLocalContentSession)
				.GetMethod("WaitForDelay", BindingFlags.Instance | BindingFlags.NonPublic)
				.Invoke(Session(), new object[] { seconds });
			Assert.That(waiting.IsCompleted, Is.True);
		}

		[UnityTest]
		public IEnumerator DisposeCancelsPendingReadinessWithoutWaitingForEditorInitialization()
		{
			var session = Session();
			var readiness = new Promise();
			var waiting = WaitForReady(session, readiness);
			Assert.That(waiting.IsCompleted, Is.False);
			session.OnDispose();
			yield return WaitForCompletion(waiting);
			Assert.That(waiting.IsCanceled, Is.True);
			Assert.That(readiness.IsCompleted, Is.False, "Disposal must not complete or cancel the shared editor readiness promise.");
			Assert.DoesNotThrow(() => readiness.CompleteSuccess());
		}

		[UnityTest]
		public IEnumerator ReadinessFailurePropagatesItsOriginalError()
		{
			var session = Session();
			var readiness = new Promise();
			var waiting = WaitForReady(session, readiness);
			var error = new InvalidOperationException("configuration failed");
			readiness.CompleteError(error);
			yield return WaitForCompletion(waiting);
			Assert.That(waiting.IsFaulted, Is.True);
			Assert.That(waiting.Exception.InnerException, Is.SameAs(error));
		}

		[Test]
		public void AlreadyReadyConfigurationCompletesWithoutDelay()
		{
			Assert.That(WaitForReady(Session(), Promise.Success).IsCompleted, Is.True);
		}

		private static IEnumerator WaitForCompletion(Task task)
		{
			// EditMode ticks can exhaust a frame budget before a Task continuation is scheduled.
			var timeout = System.Diagnostics.Stopwatch.StartNew();
			while (!task.IsCompleted && timeout.Elapsed.TotalSeconds < 5) yield return null;
		}

		[Test]
		public void CompletedSnapshotWithUnchangedIdentityRemainsUsable()
		{
			var session = SessionWithSource(out _, out var source);
			Assert.DoesNotThrow(() => ValidateSource(session, source, "workspace-before", "customer-before"));
			Assert.That(SessionField("_source").GetValue(session), Is.SameAs(source));
			Assert.That(SourceIsDisposed(source), Is.False);
		}

		[TestCase("runtime customer")]
		[TestCase("runtime realm")]
		[TestCase("workspace directory")]
		[TestCase("editor customer")]
		public void IdentityChangeRejectsCompletedSnapshotAndReleasesItsSource(string changedIdentity)
		{
			var session = SessionWithSource(out var runtime, out var source);
			var directory = "workspace-before";
			var editorCid = "customer-before";
			switch (changedIdentity)
			{
				case "runtime customer": runtime.Cid = "customer-after"; break;
				case "runtime realm": runtime.Pid = "realm-after"; break;
				case "workspace directory": directory = "workspace-after"; break;
				case "editor customer": editorCid = "customer-after"; break;
			}

			var error = Assert.Throws<TargetInvocationException>(() => ValidateSource(session, source, directory, editorCid));
			Assert.That(error.InnerException, Is.InstanceOf<OperationCanceledException>());
			Assert.That(SourceIsDisposed(source), Is.True);
			Assert.That(SessionField("_source").GetValue(session), Is.Null);
		}

		[Test]
		public void OlderCompletionCannotClearOrDisposeReplacementSource()
		{
			var session = SessionWithSource(out _, out var olderSource);
			var replacement = new EditorLocalContentSource(true, "workspace-before", "customer-before", "realm-before", "local-test");
			SessionField("_source").SetValue(session, replacement);
			try
			{
				var error = Assert.Throws<TargetInvocationException>(() =>
					ValidateSource(session, olderSource, "workspace-before", "customer-before"));
				Assert.That(error.InnerException, Is.InstanceOf<OperationCanceledException>());
				Assert.That(SourceIsDisposed(olderSource), Is.True);
				Assert.That(SessionField("_source").GetValue(session), Is.SameAs(replacement));
				Assert.That(SourceIsDisposed(replacement), Is.False);
			}
			finally
			{
				olderSource.Dispose();
			}
		}

		[Test]
		public void CompletionAfterSessionDisposalIsRejected()
		{
			var session = SessionWithSource(out _, out var source);
			session.OnDispose();
			var error = Assert.Throws<TargetInvocationException>(() =>
				ValidateSource(session, source, "workspace-before", "customer-before"));
			Assert.That(error.InnerException, Is.InstanceOf<ObjectDisposedException>());
			Assert.That(SourceIsDisposed(source), Is.True);
			Assert.That(SessionField("_source").GetValue(session), Is.Null);
		}

		private EditorLocalContentSession SessionWithSource(out TestConfigProvider runtime, out EditorLocalContentSource source)
		{
			// Exercise the post-await identity gate without initializing the real editor singleton.
			runtime = new TestConfigProvider { Cid = "customer-before", Pid = "realm-before" };
			var session = new EditorLocalContentSession(_config, runtime);
			_sessions.Add(session);
			source = new EditorLocalContentSource(true, "workspace-before", runtime.Cid, runtime.Pid, "local-test");
			SessionField("_source").SetValue(session, source);
			SessionField("_directory").SetValue(session, "workspace-before");
			SessionField("_cid").SetValue(session, runtime.Cid);
			SessionField("_pid").SetValue(session, runtime.Pid);
			return session;
		}

		private static FieldInfo SessionField(string name) =>
			typeof(EditorLocalContentSession).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);

		private static bool SourceIsDisposed(EditorLocalContentSource source) =>
			(bool)typeof(EditorLocalContentSource).GetField("_disposed", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(source);

		private static void ValidateSource(EditorLocalContentSession session, EditorLocalContentSource source, string directory, string editorCid) =>
			typeof(EditorLocalContentSession).GetMethod("ValidateSource", BindingFlags.Instance | BindingFlags.NonPublic)
				.Invoke(session, new object[] { source, directory, editorCid });

		private EditorLocalContentSession Session()
		{
			var session = new EditorLocalContentSession(_config, new TestConfigProvider());
			_sessions.Add(session);
			return session;
		}

		private static Task WaitForReady(EditorLocalContentSession session, Promise ready) =>
			(Task)typeof(EditorLocalContentSession).GetMethod("WaitForReady", BindingFlags.Instance | BindingFlags.NonPublic)
				.Invoke(session, new object[] { ready });

		private static Exception Failure<T>(Promise<T> promise)
		{
			Exception failure = null;
			promise.Error(error => failure = error);
			Assert.That(failure, Is.Not.Null);
			return failure;
		}
	}
}
