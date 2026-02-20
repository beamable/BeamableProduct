using System;
using System.Collections.Generic;

namespace beamable.server
{
	/// <summary>
	/// Contains runtime metadata information for a Beamable microservice instance.
	/// This class holds configuration and identification data used during microservice execution.
	/// </summary>
	[Serializable]
	public class MicroserviceRuntimeMetadata
	{
		/// <summary>
		/// The name of the microservice.
		/// </summary>
		public string serviceName;

		/// <summary>
		/// The version of the Beamable SDK being used.
		/// </summary>
		public string sdkVersion;

		/// <summary>
		/// The base build version of the Beamable SDK.
		/// </summary>
		public string sdkBaseBuildVersion;

		/// <summary>
		/// The execution version of the Beamable SDK.
		/// </summary>
		public string sdkExecutionVersion;

		/// <summary>
		/// Indicates whether the microservice should use legacy serialization methods.
		/// </summary>
		public bool useLegacySerialization;

		/// <summary>
		/// When true, disables all Beamable events for this microservice instance.
		/// </summary>
		public bool disableAllBeamableEvents;

		/// <summary>
		/// When true, enables eager loading of content at startup rather than lazy loading.
		/// </summary>
		public bool enableEagerContentLoading;

		/// <summary>
		/// Unique identifier for this specific microservice instance.
		/// </summary>
		public string instanceId;

		/// <summary>
		/// The routing key used for message routing to this microservice instance.
		/// </summary>
		public string routingKey;

		/// <summary>
		/// Collection of federated components associated with this microservice.
		/// </summary>
		public List<FederationComponentMetadata> federatedComponents = new List<FederationComponentMetadata>();
	}

	/// <summary>
	/// Contains metadata information for a federated component within a microservice.
	/// Federated components allow microservices to participate in distributed system architectures.
	/// </summary>
	[Serializable]
	public class FederationComponentMetadata
	{
		/// <summary>
		/// The namespace identifier for the federation component.
		/// </summary>
		public string federationNamespace;

		/// <summary>
		/// The type identifier for the federation component.
		/// </summary>
		public string federationType;
	}
}

