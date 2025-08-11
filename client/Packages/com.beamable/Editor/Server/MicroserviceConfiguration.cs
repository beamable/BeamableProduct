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
	public class MicroserviceConfiguration : ModuleConfigurationObject
	{
#if UNITY_EDITOR_OSX
		const string DOCKER_LOCATION = "/usr/local/bin/docker";
#else
		const string DOCKER_LOCATION = "docker";
#endif

		public static MicroserviceConfiguration Instance => Get<MicroserviceConfiguration>();
		
		/// <summary>
		/// this exists to migrate data from 1.19.x into the 2.0 era. However, the enabled/disabled status is no longer kept in this field. 
		/// </summary>
		[Obsolete]
		[HideInInspector]
		public List<MicroserviceConfigurationEntry> Microservices;
		
		/// <summary>
		/// this exists to migrate data from 1.19.x into the 2.0 era. However, the enabled/disabled status is no longer kept in this field. 
		/// </summary>
		[Obsolete]
		[HideInInspector]
		public List<StorageConfigurationEntry> StorageObjects;
		
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
		
		[Tooltip("When true, it will use the Old Microservice Generator based on Reflection instead of the OpenApi.")]
		public bool UseOldMicroserviceGenerator = false;

		[Tooltip("When true, the `beam checks scan` command will not be run in the beam services window.")]
		public bool DisableAutoChecks;

		[Tooltip("For each service, a boolean to control if the client code should be generated automatically when the service is built. By default, services will autogenerate, so if the key does not exist, the service will autogenerate a client. ")]
		public SerializableDictionaryStringToBool AutoGenerateClientOnBuilds =
			new SerializableDictionaryStringToBool();

	}
	
	/// <summary>
	/// This type catpures a sub-set of the information it used in 1.19.x.
	/// Check this link for the old schema. 
	/// https://github.com/beamable/BeamableProduct/blob/1.19.23/client/Packages/com.beamable.server/Editor/MicroserviceConfiguration.cs#L395
	/// </summary>
	[Serializable]
	public class MicroserviceConfigurationEntry
	{
		public string ServiceName;
		public bool Enabled;
	}
	
	/// <summary>
	/// This type catpures a sub-set of the information it used in 1.19.x.
	/// Check this link for the old schema. 
	/// https://github.com/beamable/BeamableProduct/blob/1.19.23/client/Packages/com.beamable.server/Editor/MicroserviceConfiguration.cs#L395
	/// </summary>
	[Serializable]
	public class StorageConfigurationEntry
	{
		public string StorageName;
		public bool Enabled;
	}
}
