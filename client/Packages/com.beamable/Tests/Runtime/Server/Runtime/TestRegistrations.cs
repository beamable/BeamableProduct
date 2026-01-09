using Beamable.Api;
using Beamable.Common.Api;
using Beamable.Common.Dependencies;
using System;
using UnityEngine;

namespace Beamable.Server.Tests.Runtime
{
	[BeamContextSystem]
	public class TestRegistrations
	{
		// [RegisterBeamableDependencies(-900, RegistrationOrigin.RUNTIME)]
		// public static void RegisterRuntime(IDependencyBuilder builder)
		// {
		// 	var isTest = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BEAM_UNITY_TEST_CI"));
		// 	if (!isTest) return;
		// 	Debug.Log("---- BEAM UNITY TEST CI ENABLED -----");
		// 	builder.RemoveIfExists<IServiceRoutingResolution>();
		// 	builder.RemoveIfExists<IServiceRoutingStrategy>();
		// }
	}
}
