using Beamable.Api;
using Beamable.Api.Payments;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Content;
using Beamable.Common.Dependencies;
using Beamable.Platform.Tests;
using Beamable.Platform.Tests.Content;
using NUnit.Framework;
using System;
using System.IO;
using Tests.Runtime.Beamable;
using UnityEditor;

namespace Beamable.Tests.Runtime
{
	public class BeamContextTest
	{
		protected MockBeamContext Context;
		protected MockContentService MockContent;
		protected MockPlatformAPI Requester;

		[SetUp]
		public void Setup()
		{
			MockContent = new MockContentService();
			ContentApi.Instance = Promise<IContentApi>.Successful(MockContent);

		}

		protected void TriggerContextInit(Action<IDependencyBuilder> buildDelegate = null, Action<MockBeamContext> initDelegate = null)
		{
			Context = MockBeamContext.Create(
				mutateDependencies: buildDelegate ?? OnRegister,
				onInit: initDelegate ?? OnInit
			);

			Requester = Context.Requester;
		}


		[TearDown]
		public void Cleanup()
		{
			_ = Context.ClearPlayerAndStop();
		}

		protected virtual void OnInit(MockBeamContext ctx)
		{
			ctx.AddStandardGuestLoginRequests()
			   .AddPubnubRequests()
			   .AddSessionRequests();
		}

		protected virtual void OnRegister(IDependencyBuilder builder)
		{
			builder.RemoveIfExists<IServiceRoutingResolution>();
			builder.RemoveIfExists<IBeamablePurchaser>();
			builder.RemoveIfExists<IContentApi>();
			builder.AddSingleton<IContentApi>(MockContent);
			builder.RemoveIfExists<IRuntimeConfigProvider>();
			builder.AddSingleton<IRuntimeConfigProvider>(new TestConfigProvider());
			builder.RemoveIfExists<IBeamableFilesystemAccessor>();
			builder.AddSingleton<IBeamableFilesystemAccessor>(() =>
			{
				var dir = FileUtil.GetUniqueTempPathInProject();
				Directory.CreateDirectory(dir);
				return new MockFileAccessor(dir);
			});
		}
	}
}
