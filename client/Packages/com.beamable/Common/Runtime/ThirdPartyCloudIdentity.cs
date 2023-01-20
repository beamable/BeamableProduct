using Beamable.Common.Api.Auth;
using Beamable.Common.Dependencies;
using BeamableReflection;

namespace Beamable.Common
{
	#if BEAMABLE_DEVELOPER || DB_MICROSERVICE
	public class ExampleCloudIdentity : IThirdPartyCloudIdentity
	{
		public string UniqueName => "Example";
	}
	#endif
	
	
	[Preserve]
	public interface IThirdPartyCloudIdentity
	{
		string UniqueName { get; }
	}

	public interface IFederatedLogin<in T> where T : IThirdPartyCloudIdentity, new()
	{
		FederatedAuthenticationResponse Authenticate(string token, string challenge, string solution);
	}

	public class FederatedAuthenticationResponse : ExternalAuthenticationResponse
	{
		// exists for typing purposes.
	}

	public interface IHaveServiceName
	{
		string ServiceName { get; }
	}

	public interface ISupportsFederatedLogin<T> : IHaveServiceName where T : IThirdPartyCloudIdentity, new()
	{
		IDependencyProvider Provider { get; }
	}
}
