using Beamable.Common;
using Beamable.Microservices.View;
using Beamable.Server;
using System.IO;
using UnityEngine;

namespace Beamable.Microservices
{
	[Microservice("AuthTesting")]
	public class AuthTesting : Microservice
	{
		[ClientCallable]
		public int Add(int a, int b)
		{
			return a + b;
		}
	}
}
