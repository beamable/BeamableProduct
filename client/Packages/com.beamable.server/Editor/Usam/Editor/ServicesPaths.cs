

using Beamable.Editor.UI.Model;
using System;
using System.Collections.Generic;

namespace Beamable.Server.Editor.Usam
{
	[Serializable]
	public class ServicesPaths
	{
		public List<ServiceDefinitionObject> definitions = new List<ServiceDefinitionObject>();
	}

	[Serializable]
	public class ServiceDefinitionObject
	{
		public string name;
		public string path;
	}
}


