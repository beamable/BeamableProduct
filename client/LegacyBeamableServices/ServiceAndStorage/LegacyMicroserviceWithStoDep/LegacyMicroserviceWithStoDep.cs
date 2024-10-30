using Beamable.Common;
using Beamable.Server;

namespace Beamable.Microservices
{
	[Microservice("LegacyMicroserviceWithStoDep")]
	public class LegacyMicroserviceWithStoDep : Microservice
	{
		[ClientCallable]
		public async Promise ServerCall(int x)
		{
			var doc = new LegacyDoc
			{
				x = x
			};
			var coll = await Storage.LegacyStorageForDepCollection<LegacyDoc>();
			await coll.InsertOneAsync(doc);
		}
	}
}
