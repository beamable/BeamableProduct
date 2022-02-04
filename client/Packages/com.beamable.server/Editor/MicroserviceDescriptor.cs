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

		public string SourcePath => Path.GetDirectoryName(AttributePath);
		public string HidePath => $"./Assets/~/beamservicehide/{Name}";

		public string BuildPath => $"./Assets/../Temp/beamservicebuild/{Name}";
		public string ContainerName => $"{Name}_container";
		public string ImageName => Name.ToLower();
		public ServiceType ServiceType => ServiceType.MicroService;
		public bool HasValidationError { get; set; }
		public bool HasValidationWarning { get; set; }

		public bool IsPublishFeatureDisabled()
		{
			return this.GetStorageReferences()?.Count() > 0 || this.HasMongoLibraries();
		}
	}

	[Serializable]
	public class ClientCallableDescriptor
	{
		public string Path;
		public HashSet<string> Scopes;
		public ClientCallableParameterDescriptor[] Parameters;
	}

	[Serializable]
	public class ClientCallableParameterDescriptor
	{
		public string Name;
		public int Index;
		public Type Type;
	}
}
