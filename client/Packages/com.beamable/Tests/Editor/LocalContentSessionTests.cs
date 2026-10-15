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
			for (var frame = 0; frame < 30 && !waiting.IsCompleted; frame++) yield return null;
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
			for (var frame = 0; frame < 30 && !waiting.IsCompleted; frame++) yield return null;
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
			for (var frame = 0; frame < 30 && !waiting.IsCompleted; frame++) yield return null;
			Assert.That(waiting.IsFaulted, Is.True);
			Assert.That(waiting.Exception.InnerException, Is.SameAs(error));
		}

		[Test]
		public void AlreadyReadyConfigurationCompletesWithoutDelay()
		{
			Assert.That(WaitForReady(Session(), Promise.Success).IsCompleted, Is.True);
		}

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
