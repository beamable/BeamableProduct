using Beamable.Common.BeamCli.Contracts;
using Beamable.Server.Editor;
using System;

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
		/// This is what we need for deployment.
		/// </summary>
		public string ImageId { get; set; }

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
	}
}
