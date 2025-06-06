// this file was copied from nuget package Beamable.Server.Common@4.3.0-PREVIEW.RC2
// https://www.nuget.org/packages/Beamable.Server.Common/4.3.0-PREVIEW.RC2

using System;

namespace Beamable.Server.Editor
{
	public interface IDescriptor
	{
		string Name { get; }
		string AttributePath { get; }
		string SourcePath { get; }

		Type Type { get; set; }

		string ContainerName { get; }
		string ImageName { get; }
		ServiceType ServiceType { get; }
		bool HasValidationError { get; }
		bool HasValidationWarning { get; }
	}

	[Serializable]
	public enum ServiceType
	{
		MicroService,
		StorageObject
	}
}
