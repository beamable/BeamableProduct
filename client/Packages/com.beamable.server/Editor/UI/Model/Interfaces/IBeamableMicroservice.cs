using System.Threading.Tasks;
using Beamable.Server.Editor;
using Beamable.Server.Editor.ManagerClient;

namespace Beamable.Editor.UI.Model
{
	public interface IBeamableMicroservice : IBeamableService
	{
		MicroserviceDescriptor ServiceDescriptor
		{
			get;
		}

		MicroserviceBuilder ServiceBuilder
		{
			get;
		}

		ServiceStatus RemoteStatus
		{
			get;
		}

		MicroserviceConfigurationEntry Config
		{
			get;
		}

		Task Build();
		Task BuildAndStart();
		Task BuildAndRestart();
		void OpenLocalDocs();
		void EnrichWithRemoteReference(ServiceReference remoteReference);
		void EnrichWithStatus(ServiceStatus status);
	}
}
