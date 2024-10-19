using Beamable.Common.BeamCli.Contracts;
using Beamable.Server.Editor;
using System;
using System.Collections.Generic;

namespace Beamable.Editor.UI.Model
{
	public enum BeamoServiceStatus
	{
		Unknown,
		Running,
		NotRunning
	}
	public interface IBeamoServiceDefinition
	{
		/// <summary>
		/// Is this a local service or an only remote service.
		/// </summary>
		public bool HasLocalSource { get; set; }

		IBeamableBuilder Builder { get; set; }

		/// <summary>
		/// The id that this service will be know, both locally and remotely.
		/// </summary>
		public string BeamoId { get; }

		/// <summary>
		/// The type this service represents.
		/// </summary>
		ServiceType ServiceType { get; set; }
		
		/// <summary>
		/// Whether or not this service should be enabled when we deploy remotely.
		/// </summary>
		public bool ShouldBeEnabledOnRemote { get; set; }

		/// <summary>
		/// Current service status on local computer.
		/// </summary>
		public bool IsRunningLocally { get; }

		/// <summary>
		/// Current service status on server.
		/// </summary>
		public BeamoServiceStatus IsRunningOnRemote { get; set; }

		ServiceInfo ServiceInfo { get; set; }
		bool ExistLocally { get; }
		public List<string> Dependencies { get; set; }
		public List<string> AssemblyDefinitionsNames { get; set; }
	}
}
