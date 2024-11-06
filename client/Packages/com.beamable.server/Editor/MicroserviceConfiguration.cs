using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using static Beamable.Common.Constants;

namespace Beamable.Server.Editor
{

	public class MicroserviceConfigConstants : IConfigurationConstants
	{
		public string GetSourcePath(Type type)
		{
			//
			// TODO: make this work for multiple config types
			//       but for now, there is just the one...

			return $"{Directories.BEAMABLE_SERVER_PACKAGE_EDITOR}/microserviceConfiguration.asset";

		}
	}

	public class MicroserviceConfiguration : AbsModuleConfigurationObject<MicroserviceConfigConstants>
	{
#if UNITY_EDITOR_OSX
		const string DOCKER_LOCATION = "/usr/local/bin/docker";
#else
		const string DOCKER_LOCATION = "docker";
#endif

		public static MicroserviceConfiguration Instance => Get<MicroserviceConfiguration>();

		private const string AutoBuildCommonAssemblyTooltip =
			"When true, Beamable automatically generates a common assembly called Unity.Beamable.Customer.Common that is auto-referenced by Unity code, and automatically imported by Microservice assembly definitions. ";
		[Tooltip(AutoBuildCommonAssemblyTooltip)]
		public bool AutoBuildCommonAssembly = true;
		
		[Tooltip("It will enable microservice health check at the begining of publish process.")]
		public bool EnablePrePublishHealthCheck = false;

		[Tooltip("Deploy microservices without removing existing services.")]
		public bool EnableMergeDeploy;

		[Tooltip("When a log would be printed to the Beam Services tab, if it is an error, should the log also be printed to the Unity console?")]
		public bool LogErrorsToUnityConsole = true;

	}
}
