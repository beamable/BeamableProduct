using BeamableReflection;

namespace Beamable.Common.Api.Auth
{
	[Preserve]
	public interface IThirdPartyCloudIdentity
	{
		string UniqueName { get; }
	}

	public interface IFederatedLogin<in T> where T : IThirdPartyCloudIdentity
	{
		ExternalAuthenticationResponse Authenticate(string token, string challenge, string solution);
	}

	public interface IHaveServiceName
	{
		string ServiceName { get; }
	}

	public interface ISupportsFederatedLogin<T> : IHaveServiceName where T : IThirdPartyCloudIdentity { }
}
