using Beamable.Common.Dependencies;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.TestTools;
using Beamable.Common.Api.Realms;
using Beamable.Api;

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
			Configure(builder);

			Context = BeamEditorContext.Instantiate("test", builder);
			
			/*Context.CurrentCustomer = new CustomerView()
			{
				Cid = "000",
				Alias = "test-alias",
				DisplayName = "test-displayName",
				Projects = new List<RealmView>()
			};*/

			Context.CurrentCustomer = new CustomerView();
			Context.ProductionRealm = new RealmView();
			Context.CurrentRealm = new RealmView();
			Context.Requester.Token = new AccessToken(new AccessTokenStorage(), "cid", "pid", "token", "refreshToken", 420);

			yield return Context.InitializePromise.ToYielder();
		}
	}
}
