using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Api.Realms;
using Beamable.Common.Dependencies;
using Beamable.Editor.Toolbox.Models;
using Beamable.Tests.Runtime;
using System.Collections;
using UnityEngine.TestTools;

namespace Beamable.Editor.Tests
{
	/// <summary>
	/// A base class for Beamable editor tests. It will automatically create a
	/// <see cref="BeamEditorContext"/> with the player code of "test".
	/// </summary>
	public class EditorTest
	{
		protected BeamEditorContext Context;

		protected IDependencyProvider Provider => Context.ServiceScope;

		/// <summary>
		/// Configure the services that will be used to create the <see cref="Context"/>
		/// </summary>
		/// <param name="builder">A <see cref="IDependencyBuilder"/> that has everything
		/// in the default Beamable editor scope. Remove or replace.</param>
		protected virtual void Configure(IDependencyBuilder builder)
		{
			// use this method to override whatever services you need to.
		}

		[UnitySetUp]
		public IEnumerator Setup()
		{
			var builder = BeamEditorDependencies.DependencyBuilder.Clone();
			builder.ReplaceSingleton<IWebsiteHook, MockWebsiteHook>();
			builder.ReplaceSingleton<IRuntimeConfigProvider>(new TestConfigProvider
			{
				Cid = "000",
				Pid = "111"
			});
			builder.ReplaceSingleton<IAccountService, MockAccountService>(provider =>
			{
				var service = new MockAccountService();

				return service;
			});
			Configure(builder);

			Context = BeamEditorContext.Instantiate("test", builder);

			Context.Requester.Token = new AccessToken(new AccessTokenStorage(), "000", "111", "token", "refreshToken", 420);

			yield return Context.InitializePromise.ToYielder();
		}
	}
}
