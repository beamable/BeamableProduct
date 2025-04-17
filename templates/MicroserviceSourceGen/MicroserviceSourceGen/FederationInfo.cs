using Beamable.Server;
using Microsoft.CodeAnalysis;

namespace Beamable.Microservice.SourceGen;

public readonly record struct FederationInfo
{
	public string Id { get; }
	public string ClassName { get; }
	public FederationInstanceConfig Federation { get; }
	public Location Location { get; }

	public FederationInfo(string id, string className, FederationInstanceConfig federation, Location location)
	{
		Id = id;
		ClassName = className;
		Federation = federation;
		Location = location;
	}
}
