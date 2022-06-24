using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Beamable.Server.Editor
{
	[Serializable]
	public class MicroserviceDescriptor : IDescriptor
	{
		public const string ASSEMBLY_FOLDER_NAME = "_assemblyReferences";

		[SerializeField]
		private string _name;
		public string Name
		{
			get => _name;
			set => _name = value;
		}
		public string AttributePath { get; set; }
		public Type Type { get; set; }
		public List<ClientCallableDescriptor> Methods { get; set; }
		public List<ViewDescriptor> Views { get; set; } = new List<ViewDescriptor>();

		public string SourcePath => Path.GetDirectoryName(AttributePath);
		public string HidePath => $"./Assets/~/beamservicehide/{Name}";

		public string BuildPath => $"./Temp/beam/{Name}";
		public string ContainerName => $"{Name}_container";
		public string ImageName => Name.ToLower();
		public ServiceType ServiceType => ServiceType.MicroService;
		public bool HasValidationError { get; set; }
		public bool HasValidationWarning { get; set; }

	}

	[Serializable]
	public class ClientCallableDescriptor
	{
		public string Path;
		public HashSet<string> Scopes;
		public ClientCallableParameterDescriptor[] Parameters;
	}

	[Serializable]
	public class ViewDescriptor
	{
		public MicroViewSlot Slot;
		public Type Type;
		public string ViewName;
		public string SourcePath;
		public string AppName;

		public static string GetAppName(string service, string viewName) =>
			// TODO: there are absolutely possibilies for invalid javascript bundle names :/
			$"micro_front_end_{service}_{viewName}".Replace("-", "_").Replace(".", "_");

		public MicroView CreateInstance()
		{
			var instance = (MicroView)Activator.CreateInstance(Type);
			return instance;
		}

		public string BuildEnvName => $"view-{ViewName}-env";

		public string WorkingDir => $"/view-{ViewName}"; // in-container path

		public string SourceDirectory => Path.GetDirectoryName(SourcePath);

	}

	[Serializable]
	public class ClientCallableParameterDescriptor
	{
		public string Name;
		public int Index;
		public Type Type;
	}
}
