using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Beamable.Common.Content.Validation;
using Beamable.Common.Dependencies;
using Beamable.Content;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.ContentService;
using NUnit.Framework;
using UnityEngine;

namespace Beamable.Editor.Tests
{
	public class ContentWatcherLocalModeTests
	{
		[TestCase(false, false)]
		[TestCase(true, false)]
		[TestCase(false, true)]
		[TestCase(true, true)]
		public void WatcherStartupSerializesLocalModePolicy(bool localMode, bool history)
		{
			var config = ScriptableObject.CreateInstance<ContentConfiguration>();
			try
			{
				config.EnableLocalContentInEditor = localMode;
				var commands = new CaptureCommands();
				// Avoid constructor auth/network initialization. Exercise the real watcher startup
				// and generated serialization, stopping at the command transport boundary.
				var service = (CliContentService)FormatterServices.GetUninitializedObject(typeof(CliContentService));
				foreach (var field in typeof(CliContentService).GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
					if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
						field.SetValue(service, Activator.CreateInstance(field.FieldType));
				Set(service, "_cli", commands);
				Set(service, "_contentConfiguration", config);
				Set(service, "_provider", new DependencyBuilder().AddSingleton(new ValidationContext()).Build());
				var cache = typeof(CliContentService).GetField("_contentHistoryEntryCache", BindingFlags.Instance | BindingFlags.NonPublic);
				cache.SetValue(service, Activator.CreateInstance(cache.FieldType));
				service.manifestIdOverride = "test-namespace";
				if (history)
					{
					Exception observed = null;
					service.StartContentHistory().Error(error => observed = error);
					Assert.That(observed, Is.TypeOf<TransportBoundaryException>());
				}
				else
				{
					Exception observed = null;
					service.Reload().Error(error => observed = error);
					Assert.That(observed, Is.TypeOf<TransportBoundaryException>());
				}
				Assert.That(commands.Arguments, Does.Contain("--watch=True"));
				Assert.That(commands.Arguments, Does.Contain("--manifest-ids=test-namespace"));
				Assert.That(commands.Arguments.Contains("--no-auto-sync=True"), Is.EqualTo(localMode));
			}
			finally { UnityEngine.Object.DestroyImmediate(config); }
		}

		private static void Set(object target, string name, object value) =>
			target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

		private sealed class TransportBoundaryException : Exception { }
		private sealed class CaptureCommands : BeamCommands
		{
			public string Arguments;
			public CaptureCommands() : base(null, null) { }
			public override ContentPsWrapper ContentPs(ContentPsArgs args)
			{
				Arguments = args.Serialize();
				throw new TransportBoundaryException();
			}
			public override ContentHistoryWrapper ContentHistory(ContentHistoryArgs args)
			{
				Arguments = args.Serialize();
				throw new TransportBoundaryException();
			}
		}
	}
}
