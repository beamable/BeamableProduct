// unset

using Beamable.Common.Api;
using Beamable.Common.Dependencies;
using NUnit.Framework;

namespace Beamable.Tests.Runtime
{
	public class BeamContextTest
	{
		protected MockBeamContext Context;

		[SetUp]
		public void Setup()
		{
			Context = MockBeamContext.Create(
				mutateDependencies:OnRegister,
				onInit:OnInit
				);
		}

		protected virtual void OnInit(MockBeamContext ctx)
		{

		}

		protected virtual void OnRegister(IDependencyBuilder builder)
		{
		}


	}
}
