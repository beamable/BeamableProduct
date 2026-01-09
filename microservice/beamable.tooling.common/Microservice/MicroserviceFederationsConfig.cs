using Beamable.Common.BeamCli;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Beamable.Server;

[Serializable, CliContractType]
public class MicroserviceFederationsConfig
{
	[JsonPropertyName("federations")] public FederationsConfig Federations { get; set; } = new();
}

[Serializable, CliContractType]
public class FederationsConfig : Dictionary<string, FederationInstanceConfig[]>
{
	public FederationsConfig()
	{
	}

	public FederationsConfig(Dictionary<string, FederationInstanceConfig[]> toDictionary) : base(toDictionary)
	{
	}
}

[Serializable, CliContractType]
public class FederationInstanceConfig : IEquatable<FederationInstanceConfig>
{
	[JsonProperty("interface"), System.Text.Json.Serialization.JsonPropertyName("interface"), System.Text.Json.Serialization.JsonRequired]
	public string Interface;
	[JsonProperty("className")] 
	public string ClassName;

	public bool Equals(FederationInstanceConfig other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		return Interface == other.Interface;
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != this.GetType()) return false;
		return Equals((FederationInstanceConfig)obj);
	}

	public override int GetHashCode() => Interface.GetHashCode();

	public static bool operator ==(FederationInstanceConfig left, FederationInstanceConfig right) => Equals(left, right);

	public static bool operator !=(FederationInstanceConfig left, FederationInstanceConfig right) => !Equals(left, right);
}
