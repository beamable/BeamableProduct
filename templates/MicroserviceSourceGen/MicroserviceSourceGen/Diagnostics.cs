using Beamable.Common;
using Beamable.Server;
using Microsoft.CodeAnalysis;

namespace Beamable.Microservice.SourceGen;

public static class Diagnostics
{
	public const string Category_Config = "BeamableSourceGenerator_Config";
	public const string Category_Services = "BeamableSourceGenerator_Microservices";
	public const string Category_Federations = "BeamableSourceGenerator_Federations";

	public static class Cfg
	{
		public static readonly DiagnosticDescriptor NoSourceGenConfigFound
			= new("BEAM_CFG_O001",
				$"No {nameof(MicroserviceFederationsConfig)} file found",
				$"No {nameof(MicroserviceFederationsConfig)} file found. Please run `dotnet beam init` again so this will be created for you.",
				Category_Config,
				DiagnosticSeverity.Error,
				true);

		public static readonly DiagnosticDescriptor MultipleSourceGenConfigsFound
			= new("BEAM_CFG_O002",
				$"Multiple {nameof(MicroserviceFederationsConfig)} files found",
				$"Multiple {nameof(MicroserviceFederationsConfig)} files found: {{0}}",
				Category_Config,
				DiagnosticSeverity.Error,
				true);

		public static readonly DiagnosticDescriptor FailedToDeserializeSourceGenConfig
			= new("BEAM_CFG_O003",
				$"{nameof(MicroserviceFederationsConfig)} could not be deserialized",
				$"{nameof(MicroserviceFederationsConfig)} could not be deserialized. Error={{0}}.",
				Category_Config,
				DiagnosticSeverity.Error,
				true);

		public static readonly DiagnosticDescriptor DeserializedSourceGenConfig
			= new("BEAM_CFG_O004",
				$"Loaded {nameof(MicroserviceFederationsConfig)}",
				$"Loaded {nameof(MicroserviceFederationsConfig)}. Text={{1}}.",
				Category_Config,
				DiagnosticSeverity.Hidden,
				true);
	}

	public static class Srv
	{
		public const string NO_MICROSERVICE_DETECTED_DIAGNOSTIC_ID = "BEAM_SRV_O001";
		public const string MULTIPLE_MICROSERVICE_CLASSES_DETECTED_DIAGNOSTIC_ID = "BEAM_SRV_O002";
		public const string NON_PARTIAL_MICROSERVICE_CLASS_DETECTED_DIAGNOSTIC_ID = "BEAM_SRV_O003";
		public const string MISSING_MICROSERVICE_ID_DIAGNOSTIC_ID = "BEAM_SRV_O004";
		public const string INVALID_MICROSERVICE_ID_DIAGNOSTIC_ID = "BEAM_SRV_O005";
		public const string INVALID_ASYNC_VOID_CALLABLE_DIAGNOSTIC_ID = "BEAM_SRV_O006";
		public const string CALLABLE_METHOD_TYPE_INSIDE_MICROSERVICE_SCOPE_ID = "BEAM_SRV_O007";
		public const string CALLABLE_METHOD_TYPE_IS_NESTED_ID = "BEAM_SRV_O008";
		public const string CLASS_BEAM_GENERATE_SCHEMA_ATTRIBUTE_IS_NESTED_ID = "BEAM_SRV_O009";

		public static readonly DiagnosticDescriptor NoMicroserviceClassesDetected
			= new(NO_MICROSERVICE_DETECTED_DIAGNOSTIC_ID,
				$"No {nameof(Server.Microservice)} classes detected",
				$"No {nameof(Server.Microservice)} classes detected. Make sure only a single class implementing {nameof(Server.Microservice)} exists in each service project.",
				Category_Services,
				DiagnosticSeverity.Error,
				true);

		public static readonly DiagnosticDescriptor MultipleMicroserviceClassesDetected
			= new(MULTIPLE_MICROSERVICE_CLASSES_DETECTED_DIAGNOSTIC_ID,
				$"Multiple {nameof(Server.Microservice)} classes detected",
				$"Multiple Microservice classes detected. Make sure only a single class implementing {nameof(Server.Microservice)} exists in each service project. ClassNames={{0}}.",
				Category_Services,
				DiagnosticSeverity.Error,
				helpLinkUri: "https://docs.beamable.com/docs/cli-guide-microservices#multiple-microservice-classes-detected",
				isEnabledByDefault: true);

		public static readonly DiagnosticDescriptor NonPartialMicroserviceClassDetected
			= new(NON_PARTIAL_MICROSERVICE_CLASS_DETECTED_DIAGNOSTIC_ID,
				$"Non-Partial {nameof(Server.Microservice)} classes detected",
				$"Non-Partial Microservice class detected. Make sure your {nameof(Server.Microservice)} class is marked as `partial`.",
				Category_Services,
				DiagnosticSeverity.Error,
				helpLinkUri: "https://docs.beamable.com/docs/cli-guide-microservices#non-partial-microservice-class-detected",
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor MissingMicroserviceId
			= new(MISSING_MICROSERVICE_ID_DIAGNOSTIC_ID,
				$"{nameof(Server.Microservice)} class is missing the microservice id",
				$"{nameof(Server.Microservice)} class is missing the microservice id",
				Category_Services,
				DiagnosticSeverity.Error,
				helpLinkUri: "https://docs.beamable.com/docs/cli-guide-microservices#microservice-class-missing-microservice-id",
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor InvalidMicroserviceId
			= new(INVALID_MICROSERVICE_ID_DIAGNOSTIC_ID,
				$"{nameof(Server.Microservice)} ids can only contain alphanumeric characters",
				$"{nameof(Server.Microservice)} ids can only contain alphanumeric characters",
				Category_Services,
				DiagnosticSeverity.Error,
				true);
		
		public static readonly DiagnosticDescriptor InvalidAsyncVoidCallableMethod
			= new(INVALID_ASYNC_VOID_CALLABLE_DIAGNOSTIC_ID,
				$"{nameof(Server.Microservice)} Callable methods cannot be async voids",
				$"{nameof(Server.Microservice)} Callable methods cannot be async voids. Ex: {{0}}.",
				Category_Services,
				DiagnosticSeverity.Error,
				helpLinkUri: "https://docs.beamable.com/docs/cli-guide-microservices#async-void-callable-methods",
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor CallableTypeInsideMicroserviceScope
			= new(CALLABLE_METHOD_TYPE_INSIDE_MICROSERVICE_SCOPE_ID,
				$"{nameof(Server.Microservice)} Callable methods uses a Type that cannot be inside microservice scope",
				$"{nameof(Server.Microservice)} Callable method {{0}} uses a Type that cannot be inside microservice scope. Type: {{1}}.",
				Category_Services,
				DiagnosticSeverity.Error,
				helpLinkUri: "https://docs.beamable.com/docs/cli-guide-microservices#invalid-type-usage-in-callable-method",
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor CallableMethodTypeIsNested
			= new(CALLABLE_METHOD_TYPE_IS_NESTED_ID,
				$"{nameof(Server.Microservice)} Callable methods uses a Type that is Nested",
				$"{nameof(Server.Microservice)} Callable method {{1}} uses a Type that is Nested, which is not supported by the Source Code Generator. Please move {{0}} to outer scope.",
				Category_Services,
				DiagnosticSeverity.Error,
				helpLinkUri: "https://docs.beamable.com/docs/cli-guide-microservices#callable-method-types-usage-are-nested",
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor ClassBeamGenerateSchemaAttributeIsNested
			= new(CLASS_BEAM_GENERATE_SCHEMA_ATTRIBUTE_IS_NESTED_ID,
				$"Type with [BeamGenerateSchema] attribute cannot be nested type",
				$"Type {{0}} contains [BeamGenerateSchema] attribute and is a Nested type, which is not supported by the Source Code Generator. Please move {{0}} to outer scope.",
				Category_Services,
				DiagnosticSeverity.Error,
				helpLinkUri: "https://docs.beamable.com/docs/cli-guide-microservices#beam-generated-schema-class-is-a-nested-type",
				isEnabledByDefault: true);
	}

	public static class Fed
	{
		public const string DECLARED_FEDERATION_MISSING_FROM_SOURCE_GEN_CONFIG_DIAGNOSTIC_ID = "BEAM_FED_O001";
		public const string CONFIGURED_FEDERATION_MISSING_FROM_CODE_DIAGNOSTIC_ID = "BEAM_FED_O002";
		public const string FEDERATION_CODE_GENERATED_PROPERLY_DIAGNOSTIC_ID = "BEAM_FED_O003";
		public const string DECLARED_FEDERATION_INVALID_FEDERATION_ID_DIAGNOSTIC_ID = "BEAM_FED_O004";
		public const string FEDERATION_ID_MISSING_ATTRIBUTE_DIAGNOSTIC_ID = "BEAM_FED_O005";
		public const string FEDERATION_ID_MUST_BE_DEFAULT_DIAGNOSTIC_ID = "BEAM_FED_O006";
		public const string FEDERATION_CONFIG_FILE_NOT_FOUND_ID = "BEAM_FED_O007";
		public const string ERROR_PARSING_FEDERATION_CONFIG_FILE_ID = "BEAM_FED_O008";
		public const string FEDERATION_ID_INVALID_CONFIG_FILE_ID = "BEAM_FED_O009";

		public const string PROP_FEDERATION_ID = "FederationId";
		public const string PROP_FEDERATION_INTERFACE = "FederationInterface";
		public const string PROP_MICROSERVICE_NAME = "MicroserviceName";
		public const string PROP_FEDERATION_CLASS_NAME = "FederationClassName";

		public static readonly DiagnosticDescriptor DeclaredFederationMissingFromSourceGenConfig
			= new(DECLARED_FEDERATION_MISSING_FROM_SOURCE_GEN_CONFIG_DIAGNOSTIC_ID,
				$"Missing declared Federation in {nameof(MicroserviceFederationsConfig)}",
				$"Missing declared Federation in {nameof(MicroserviceFederationsConfig)}. Microservice={{0}}, Id={{1}}, Interface={{2}}." +
				$" Please add this Id by running `dotnet beam fed add {{0}} {{1}} {{2}}` from your project's root directory. " +
				$"Or remove the {{2}} that references {{1}}  interface from the {{0}} Microservice class.",
				Category_Federations,
				DiagnosticSeverity.Error,
				helpLinkUri: "https://docs.beamable.com/docs/cli-guide-microservice-federation#missing-declared-federation-in-microservicefederationsconfig",
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor ConfiguredFederationMissingFromCode
			= new(CONFIGURED_FEDERATION_MISSING_FROM_CODE_DIAGNOSTIC_ID,
				$"{nameof(MicroserviceFederationsConfig)} contains Federations that do not exist in code",
				$"You have configured federation, but the Microservice does not implement the required interface. Microservice={{0}}, Id={{1}}, Interface={{2}}. " +
				$"Please remove this Id by running `dotnet beam fed remove {{0}} {{1}} {{2}}` from your project's root directory, " +
				$"Or add the {{2}} that references {{1}} interface to the {{0}} Microservice class.",
				Category_Federations,
				DiagnosticSeverity.Error,
				helpLinkUri: "https://docs.beamable.com/docs/cli-guide-microservice-federation#microservicefederationsconfig-contains-federations-that-do-not-exist-in-code",
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor FederationCodeGeneratedProperly
			= new(FEDERATION_CODE_GENERATED_PROPERLY_DIAGNOSTIC_ID,
				$"All federations we found are valid and we code-gen properly",
				$"All federations we found are valid and we code-gen properly",
				Category_Federations,
				DiagnosticSeverity.Hidden,
				true);
		
		public static readonly DiagnosticDescriptor DeclaredFederationInvalidFederationId
			= new(DECLARED_FEDERATION_INVALID_FEDERATION_ID_DIAGNOSTIC_ID,
				$"Invalid Federation Id detected",
				$"The following {nameof(IFederationId)} is invalid. They must:" +
				$" Start with a letter." +
				$" Contain only alphanumeric characters and/or `_`. Microservice={{0}}, Id={{1}}.",
				Category_Federations,
				DiagnosticSeverity.Error,
				helpLinkUri: "https://docs.beamable.com/docs/cli-guide-microservice-federation#invalid-federation-id-detected",
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor FederationIdMissingAttribute
			= new(FEDERATION_ID_MISSING_ATTRIBUTE_DIAGNOSTIC_ID,
				$"IFederationId is missing FederationIdAttribute",
				$"The following {nameof(IFederationId)} must be annotated with a {nameof(FederationIdAttribute)}. They must:" +
				$" Start with a letter." +
				$" Contain only alphanumeric characters and/or `_`.",
				Category_Federations,
				DiagnosticSeverity.Error,
				helpLinkUri: "https://docs.beamable.com/docs/cli-guide-microservice-federation#ifederationid-is-missing-federationidattribute",
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor FederationIdMustBeDefault
			= new(FEDERATION_ID_MUST_BE_DEFAULT_DIAGNOSTIC_ID,
				$"IFederationId must be \"default\"",
				$"The following {nameof(IFederationId)} must be annotated with a {nameof(FederationIdAttribute)} with a value of \"default\", Id={{0}}",
				Category_Federations,
				DiagnosticSeverity.Error,
				helpLinkUri: "https://docs.beamable.com/docs/cli-guide-microservice-federation#ifederationid-must-be-default",
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor FederationIdInvalidConfigFile
			= new(FEDERATION_ID_INVALID_CONFIG_FILE_ID,
				$"Invalid Federation Id detected on Config File",
				$"The following {nameof(IFederationId)} is invalid. They must:" +
				$" Start with a letter." +
				$" Contain only alphanumeric characters and/or `_`. Id={{0}}.",
				Category_Federations,
				DiagnosticSeverity.Error,
				helpLinkUri: "https://docs.beamable.com/docs/cli-guide-microservice-federation#invalid-federation-id-detected-on-config-file",
				isEnabledByDefault: true);
		
	}
}
