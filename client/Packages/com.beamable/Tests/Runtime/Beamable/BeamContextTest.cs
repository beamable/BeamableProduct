
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


		[TearDown]
		public void Cleanup()
		{
			Context.ClearPlayerAndStop();
		}

		protected virtual void OnInit(MockBeamContext ctx)
		{

		}

		protected virtual void OnRegister(IDependencyBuilder builder)
		{
		}


	}
}
