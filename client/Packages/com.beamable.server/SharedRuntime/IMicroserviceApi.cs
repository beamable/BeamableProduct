using System;
using System.Collections.Generic;

namespace Beamable.Server
{
	public interface IMicroserviceApi
	{
		string Name { get; }
		Type Type { get; }
		List<MicroserviceEndPointInfo> EndPoints { get; }
	}
	
	public class MicroserviceArgument
	{
		public string name;
		public Type type;

		public override string ToString() => $"{type.FullName} {name}";
	}

	public class MicroserviceEndPointInfo
	{
		public CallableAttribute callableAttribute;
		public string methodName;
		public Type returnType;
		public IList<MicroserviceArgument> parameters;
		public string description;
	}
}
