using Beamable.Common;
using Beamable.Platform.Tests;
using Beamable.Tests.Runtime.Api;

namespace Beamable.Server.Tests.Runtime
{
	public class TestClient : MicroserviceClient
	{
		private readonly string _serviceName;

		public TestClient(string serviceName)
		{
			_serviceName = serviceName;
			MicroserviceClientHelper.SetPrefix("test");

		}

		public Promise<T> Request<T>(string endpoint, string[] serializedFields)
		{
			return base.Request<T>(_serviceName, endpoint, serializedFields);
		}

		public string GetMockPath(string cid, string pid, string endpoint)
		{
			return CreateUrl(cid, pid, _serviceName, endpoint);
		}
	}
}
