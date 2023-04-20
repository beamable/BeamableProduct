using Beamable.Server;

namespace Beamable.standalone_microservice
{
	[Microservice("standalone_microservice")]
	public class StandaloneMicroservice : Microservice
	{
		[Callable]
		public int Add(int a, int b)
		{
			return a + b;
		}

		[ClientCallable]
		public string JoinNumbersAsString(int a, int b)
		{
			return $"{a}{b}";
		}
		
		[AdminOnlyCallable]
		public bool AlwaysTrue(string text)
		{
			return true;
		}
	}
}
