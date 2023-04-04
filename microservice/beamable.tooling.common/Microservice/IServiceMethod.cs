// using System.Reflection;
//
// namespace Beamable.Server.Common.Microservice;
//
// public interface IServiceMethod
// {
// 	List<string> ParameterNames { get; }
// 	MethodInfo Method { get; }
// 	List<ParameterInfo> ParameterInfos { get; }
// 	HashSet<string> RequiredScopes { get; }
// 	string Tag { get; }
// 	string Path { get; }
// 	bool RequireAuthenticatedUser { get; }
// }
//
// public class ReadonlyServiceMethod : IServiceMethod
// {
// 	public List<string> ParameterNames { get; init;  }
// 	public MethodInfo Method { get;init;  }
// 	public List<ParameterInfo> ParameterInfos { get; init; }
// 	public HashSet<string> RequiredScopes { get; init; }
// 	public string Tag { get; init; }
// 	public string Path { get; init; }
// 	public bool RequireAuthenticatedUser { get; init; }
// }
//
