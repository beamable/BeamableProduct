using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Common.Util;
using Beamable.Microservice.SourceGen.Analyzers;
using Beamable.Server;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace Beamable.Microservice.SourceGen;

public static class Diagnostics
{
	public const string Category_Debug = "BeamableSourceGenerator_Debug";
	public const string Category_Config = "BeamableSourceGenerator_Config";
	public const string Category_Services = "BeamableSourceGenerator_Microservices";
	public const string Category_Federations = "BeamableSourceGenerator_Federations";

	// To enable Debug go to Roslyn Settings and Set the BeamableSourceGenerator_Debug to any other Severity.
	public static readonly DiagnosticDescriptor BeamVerboseDescriptor = new("BEAM_DBG_0001", "Beamable Verbose Debug", "{0}", Category_Debug, DiagnosticSeverity.Hidden,
		true);
	
	public static readonly DiagnosticDescriptor BeamExceptionDescriptor = new("BEAM_EXC_0001", "Beamable Exception Debug", "{0}", Category_Debug, DiagnosticSeverity.Error,
		true);
	
	
	public static Diagnostic GetVerbose(string title, string message, Location location, Compilation compilation, Location fallbackLocation = null)
	{
		return Diagnostic.Create(BeamVerboseDescriptor, GetValidLocation(location,compilation, fallbackLocation), $"{title} | {message}");
	}

	public static Diagnostic GetException(Exception exception, Location location, Compilation compilation, Location fallbackLocation = null)
	{
		return Diagnostic.Create(BeamExceptionDescriptor, GetValidLocation(location,compilation, fallbackLocation),$"Exception | {exception.Message} | Stacktrace: {exception.StackTrace}");
	}
	
	// Microsoft does this to check location to see if it is valid, we are checking it beforehand so we don't send invalid locations to the Diagnostic.
	public static Location GetValidLocation(Location location, Compilation compilation, Location fallback = null)
	{
		bool isLocationNull = location == null;
		bool isLocationNotInSource = isLocationNull || !location.IsInSource;
		bool isLocationNotInSyntaxTree = isLocationNull || location.SourceTree == null || !compilation.ContainsSyntaxTree(location.SourceTree);
		bool isLocationOutOfSourceTree = isLocationNull || location.SourceTree == null || location.SourceSpan.End > location.SourceTree.Length;
		if(isLocationNull || isLocationNotInSource || isLocationNotInSyntaxTree || isLocationOutOfSourceTree)
		{
			return fallback == null ? Location.None : GetValidLocation(fallback, compilation);
		}

		return location;
	}

	public static class Srv
	{
		// public const string NO_MICROSERVICE_DETECTED_DIAGNOSTIC_ID = "BEAM_SRV_0001"; // deprecated in CLI 6. Leave this here so ordering stays consistent. 
		// public const string MULTIPLE_MICROSERVICE_CLASSES_DETECTED_DIAGNOSTIC_ID = "BEAM_SRV_0002";  // deprecated in CLI 6. Leave this here so ordering stays consistent. 
		// public const string NON_PARTIAL_MICROSERVICE_CLASS_DETECTED_DIAGNOSTIC_ID = "BEAM_SRV_0003";  // deprecated in CLI 6. Leave this here so ordering stays consistent. 
		// public const string MISSING_MICROSERVICE_ID_DIAGNOSTIC_ID = "BEAM_SRV_0004";  // deprecated in CLI 6. Leave this here so ordering stays consistent. 
		// public const string INVALID_MICROSERVICE_ID_DIAGNOSTIC_ID = "BEAM_SRV_0005"; This isn't being used anymore, but we kept it here to keep the Diagnostics IDs order.
		public const string INVALID_ASYNC_VOID_CALLABLE_DIAGNOSTIC_ID = "BEAM_SRV_0006";
		public const string CALLABLE_METHOD_TYPE_INSIDE_MICROSERVICE_SCOPE_ID = "BEAM_SRV_0007";
		public const string CALLABLE_METHOD_TYPE_IS_NESTED_ID = "BEAM_SRV_0008";
		public const string CLASS_BEAM_GENERATE_SCHEMA_ATTRIBUTE_IS_NESTED_ID = "BEAM_SRV_0009";
		public const string MICROSERVICE_ID_INVALID_FROM_CS_PROJ_ID = "BEAM_SRV_0010";
		public const string STATIC_FIELD_FOUND_IN_MICROSERVICE_ID = "BEAM_SRV_0011";
		public const string MISSING_SERIALIZABLE_ATTRIBUTE_ON_TYPE_ID = "BEAM_SRV_0012";
		public const string PROPERTIES_FOUND_IN_SERIALIZABLE_TYPES_ID = "BEAM_SRV_0013";
		public const string NULLABLE_TYPE_FOUND_ID = "BEAM_SRV_0014";
		public const string INVALID_CONTENT_OBJECT_ID = "BEAM_SRV_0015";
		public const string TYPE_IN_BEAM_GENERATED_IS_MISSING_BEAM_GENERATED_ATTRIBUTE_ID = "BEAM_SRV_0016";
		public const string FIELD_DICTIONARY_IS_INVALID = "BEAM_SRV_0017";
		public const string FIELD_IS_SUBTYPE_FROM_DICTIONARY_ID = "BEAM_SRV_0018";
		public const string FIELD_IS_SUBTYPE_FROM_LIST_ID = "BEAM_SRV_0019";
		public const string CALLABLE_METHOD_DECLARATION_IS_INVALID_DICTIONARY = "BEAM_SRV_0020";
		public const string CALLABLE_METHOD_DECLARATION_TYPE_IS_SUBTYPE_FROM_DICTIONARY_ID = "BEAM_SRV_0021";
		public const string CALLABLE_METHOD_DECLARATION_TYPE_IS_SUBTYPE_FROM_LIST_ID = "BEAM_SRV_0022";
		public const string INVALID_GENERIC_TYPE_ID = "BEAM_SRV_0023";

		public const string PROP_BEAM_ID = "BeamId";
		public const string PROP_FIELD_NAME = "FieldName";
		
		public const string TROUBLESHOOTING_GUIDE_BASE_URL = "cli/guides/ms-troubleshooting/";
		
		public static readonly DiagnosticDescriptor InvalidAsyncVoidCallableMethod
			= new(INVALID_ASYNC_VOID_CALLABLE_DIAGNOSTIC_ID,
				$"{nameof(Server.Microservice)} Callable methods cannot be async voids",
				$"{nameof(Server.Microservice)} Callable methods cannot be async voids. Ex: {{0}}.",
				Category_Services,
				DiagnosticSeverity.Error,
				helpLinkUri: DocsPageHelper.GetCliDocsPageUrl($"{TROUBLESHOOTING_GUIDE_BASE_URL}#async-void-callable-methods", Constants.CLI_CURRENT_DOCS_VERSION),
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor CallableTypeInsideMicroserviceScope
			= new(CALLABLE_METHOD_TYPE_INSIDE_MICROSERVICE_SCOPE_ID,
				$"{nameof(Server.Microservice)} Callable methods uses a Type that cannot be inside microservice scope",
				$"{nameof(Server.Microservice)} callable method '{{0}}' uses type '{{1}}' which is not allowed in microservice scope. Ensure all types used in microservice callable methods are defined in a shared project that both client and microservice can reference. Move the class to Assets/Beamable/Common or any other shared lib.",
				Category_Services,
				DiagnosticSeverity.Error,
				helpLinkUri: DocsPageHelper.GetCliDocsPageUrl($"{TROUBLESHOOTING_GUIDE_BASE_URL}#invalid-type-usage-in-callable-method", Constants.CLI_CURRENT_DOCS_VERSION),
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor CallableMethodTypeIsNested
			= new(CALLABLE_METHOD_TYPE_IS_NESTED_ID,
				$"{nameof(Server.Microservice)} Callable methods uses a Type that is Nested",
				$"{nameof(Server.Microservice)} Callable method {{1}} uses a Type that is Nested, which is not supported by the Source Code Generator. Please move {{0}} to outer scope.",
				Category_Services,
				DiagnosticSeverity.Error,
				helpLinkUri: DocsPageHelper.GetCliDocsPageUrl($"{TROUBLESHOOTING_GUIDE_BASE_URL}#callable-method-types-usage-are-nested", Constants.CLI_CURRENT_DOCS_VERSION),
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor ClassBeamGenerateSchemaAttributeIsNested
			= new(CLASS_BEAM_GENERATE_SCHEMA_ATTRIBUTE_IS_NESTED_ID,
				$"Type with [BeamGenerateSchema] attribute cannot be nested type",
				$"Type {{0}} contains [BeamGenerateSchema] attribute and is a Nested type, which is not supported by the Source Code Generator. Please move {{0}} to outer scope.",
				Category_Services,
				DiagnosticSeverity.Error,
				helpLinkUri: DocsPageHelper.GetCliDocsPageUrl($"{TROUBLESHOOTING_GUIDE_BASE_URL}#beam-generated-schema-class-is-a-nested-type", Constants.CLI_CURRENT_DOCS_VERSION),
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor MicroserviceIdInvalidFromCsProj
			= new(MICROSERVICE_ID_INVALID_FROM_CS_PROJ_ID,
				$"{nameof(Server.Microservice)} ID is invalid, it needs to be the same as <BeamId> csharp property (or as csproj name if none exists)",
				$"{nameof(Server.Microservice)} ID: `{{0}}` is invalid, it needs to be the same as <BeamId> csharp property (or as csproj name if none exists): `{{1}}`",
				Category_Services,
				DiagnosticSeverity.Error,
				helpLinkUri: DocsPageHelper.GetCliDocsPageUrl($"{TROUBLESHOOTING_GUIDE_BASE_URL}#invalid-microservice-id", Constants.CLI_CURRENT_DOCS_VERSION),
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor StaticFieldFoundInMicroservice
			= new(STATIC_FIELD_FOUND_IN_MICROSERVICE_ID,
				$"Its not recommended to have non-readonly static field on Microservice as those field doesn't work as expected when scale horizontally",
				$"Its not recommended to have non-readonly static field on Microservice as those field doesn't work as expected when scale horizontally. Consider making {{0}} a readonly field. Otherwise the value may be inconsistent in production environments.",
				Category_Services,
				DiagnosticSeverity.Warning,
				helpLinkUri: DocsPageHelper.GetCliDocsPageUrl($"{TROUBLESHOOTING_GUIDE_BASE_URL}#static-field-in-microservice", Constants.CLI_CURRENT_DOCS_VERSION),
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor MissingSerializableAttributeOnType
			= new(MISSING_SERIALIZABLE_ATTRIBUTE_ON_TYPE_ID,
				$"Types used in Microservice methods or marked with [BeamGenerateSchema] must be serializable",
				$"Types used in Microservice methods or marked with [BeamGenerateSchema] must be serializable. Add the [Serializable] attribute to type '{{0}}'.",
				Category_Services,
				DiagnosticSeverity.Error,
				helpLinkUri: DocsPageHelper.GetCliDocsPageUrl($"{TROUBLESHOOTING_GUIDE_BASE_URL}#missing-serializable-attribute-on-type", Constants.CLI_CURRENT_DOCS_VERSION),
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor MissingSerializableAttributeForArgument
			= new(MISSING_SERIALIZABLE_ATTRIBUTE_ON_TYPE_ID,
				"Argument type is not serializable",
				$"Type of argument '{{1}}' must be serializable or have [BeamGenerateSchema] attribute. Either add the [Serializable] attribute to type '{{0}}' or use different type.",
				Category_Services,
				DiagnosticSeverity.Error,
				helpLinkUri: DocsPageHelper.GetCliDocsPageUrl($"{TROUBLESHOOTING_GUIDE_BASE_URL}#missing-serializable-attribute-on-type", Constants.CLI_CURRENT_DOCS_VERSION),
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor PropertiesFoundInSerializableTypes
			= new(PROPERTIES_FOUND_IN_SERIALIZABLE_TYPES_ID,
				$"Properties in serializable types are ignored by the client code generator",
				$"Properties in serializable types are ignored by the client code generator. On {{0}} consider changing property '{{1}}' to a field to include it in client-generated code.",
				Category_Services,
				DiagnosticSeverity.Info,
				helpLinkUri: DocsPageHelper.GetCliDocsPageUrl($"{TROUBLESHOOTING_GUIDE_BASE_URL}#property-found-in-serializable-type", Constants.CLI_CURRENT_DOCS_VERSION),
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor NullableTypeFoundInMicroservice
			= new(NULLABLE_TYPE_FOUND_ID,
				$"Nullables on {nameof(Server.Microservice)} Callable methods or classes with [BeamGenerateSchema] are not supported",
				$"Nullables on {nameof(Server.Microservice)} Callable methods or classes with [BeamGenerateSchema] are not supported. On {{0}} change it's type to use an {nameof(Optional)} type instead of '{{1}}'.",
				Category_Services,
				DiagnosticSeverity.Error,
				helpLinkUri: DocsPageHelper.GetCliDocsPageUrl($"{TROUBLESHOOTING_GUIDE_BASE_URL}#nullable-field-in-serializable-type", Constants.CLI_CURRENT_DOCS_VERSION),
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor InvalidContentObject
			= new(INVALID_CONTENT_OBJECT_ID,
				$"{nameof(ContentObject)} type or its subtypes are not recommended to use as it can use a lot of data, use ContentRef<T> instead",
				$"{nameof(ContentObject)} type or its subtypes are not recommended to use as it can use a lot of data, use ContentRef<T> instead. Change {{0}} to use ContentRef<{{1}}> instead of {{1}}.",
				Category_Services,
				DiagnosticSeverity.Warning,
				helpLinkUri: DocsPageHelper.GetCliDocsPageUrl($"{TROUBLESHOOTING_GUIDE_BASE_URL}#invalid-contentobject-used", Constants.CLI_CURRENT_DOCS_VERSION),
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor TypeInBeamGeneratedIsMissingBeamGeneratedAttribute
			= new(TYPE_IN_BEAM_GENERATED_IS_MISSING_BEAM_GENERATED_ATTRIBUTE_ID,
				$"Types used in fields of [BeamGenerateSchema] classes must also be marked with [BeamGenerateSchema]",
				$"Types used in fields of [BeamGenerateSchema] classes must also be marked with [BeamGenerateSchema]. Add the [BeamGenerateSchema] attribute to type '{{0}}'.",
				Category_Services,
				DiagnosticSeverity.Error,
				helpLinkUri: DocsPageHelper.GetCliDocsPageUrl($"{TROUBLESHOOTING_GUIDE_BASE_URL}#type-used-in-beamgenerateschema-is-missing-attribute", Constants.CLI_CURRENT_DOCS_VERSION),
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor DictionaryKeyMustBeStringOnSerializableTypes
			= new(FIELD_DICTIONARY_IS_INVALID,
				$"Dictionary fields in serializable types are only allowed if the key is string",
				$"Dictionary fields in serializable types are only allowed if the key is string. On {{0}} change the dictionary key of field {{1}} to string instead of type {{2}}.",
				Category_Services,
				DiagnosticSeverity.Error,
				helpLinkUri: DocsPageHelper.GetCliDocsPageUrl($"{TROUBLESHOOTING_GUIDE_BASE_URL}#dictionary-key-must-be-string-on-serializable-types", Constants.CLI_CURRENT_DOCS_VERSION),
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor FieldOnSerializableTypeIsSubtypeFromDictionary
			= new(FIELD_IS_SUBTYPE_FROM_DICTIONARY_ID,
				$"Fields that are subtype from Dictionary are not supported in serializable types",
				$"Fields that are subtype from Dictionary are not supported in serializable types. On {{0}} replace field {{1}} to Dictionary instead of type {{2}}.",
				Category_Services,
				DiagnosticSeverity.Error,
				helpLinkUri: DocsPageHelper.GetCliDocsPageUrl($"{TROUBLESHOOTING_GUIDE_BASE_URL}#field-on-serializable-type-is-subtype-from-dictionary", Constants.CLI_CURRENT_DOCS_VERSION),
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor FieldOnSerializableTypeIsSubtypeFromList
			= new(FIELD_IS_SUBTYPE_FROM_LIST_ID,
				$"Fields that are subtype from List are not supported in serializable types",
				$"Fields that are subtype from List are not supported in serializable types. On {{0}} replace field {{1}} to List instead of type {{2}}.",
				Category_Services,
				DiagnosticSeverity.Error,
				helpLinkUri: DocsPageHelper.GetCliDocsPageUrl($"{TROUBLESHOOTING_GUIDE_BASE_URL}#field-on-serializable-type-is-subtype-from-list", Constants.CLI_CURRENT_DOCS_VERSION),
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor CallableMethodDeclarationTypeIsInvalidDictionary
			= new(CALLABLE_METHOD_DECLARATION_IS_INVALID_DICTIONARY,
				$"Dictionaries on {nameof(Server.Microservice)} Callable methods are only allowed if the key is string",
				$"Dictionaries on {nameof(Server.Microservice)} Callable methods are only allowed if the key is string. Change the dictionary key of {{0}} to string instead of type {{1}}.",
				Category_Services,
				DiagnosticSeverity.Error,
				helpLinkUri: DocsPageHelper.GetCliDocsPageUrl($"{TROUBLESHOOTING_GUIDE_BASE_URL}#callable-method-declaration-type-is-invalid-dictionary", Constants.CLI_CURRENT_DOCS_VERSION),
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor CallableMethodDeclarationTypeIsSubtypeFromDictionary
			= new(CALLABLE_METHOD_DECLARATION_TYPE_IS_SUBTYPE_FROM_DICTIONARY_ID,
				$"Types on {nameof(Server.Microservice)} Callable methods that inherits Dictionary are not supported",
				$"Types on {nameof(Server.Microservice)} Callable methods that inherits Dictionary are not supported. Replace {{0}} to Dictionary instead of type {{1}}.",
				Category_Services,
				DiagnosticSeverity.Error,
				helpLinkUri: DocsPageHelper.GetCliDocsPageUrl($"{TROUBLESHOOTING_GUIDE_BASE_URL}#callable-method-declaration-type-is-subtype-from-dictionary", Constants.CLI_CURRENT_DOCS_VERSION),
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor CallableMethodDeclarationTypeIsSubtypeFromList
			= new(CALLABLE_METHOD_DECLARATION_TYPE_IS_SUBTYPE_FROM_LIST_ID,
				$"Types on {nameof(Server.Microservice)} Callable methods that inherits List are not supported",
				$"Types on {nameof(Server.Microservice)} Callable methods that inherits List are not supported. Replace {{0}} to List instead of type {{1}}.",
				Category_Services,
				DiagnosticSeverity.Error,
				helpLinkUri: DocsPageHelper.GetCliDocsPageUrl($"{TROUBLESHOOTING_GUIDE_BASE_URL}#callable-method-declaration-type-is-subtype-from-list", Constants.CLI_CURRENT_DOCS_VERSION),
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor InvalidGenericTypeOnMicroservice
			= new(INVALID_GENERIC_TYPE_ID,
				$"Generic Types on {nameof(Server.Microservice)} Callable methods or classes with [BeamGenerateSchema] are not supported",
				$"Generic Types on {nameof(Server.Microservice)} Callable methods or classes with [BeamGenerateSchema] are not supported. The only generic types allowed are: {string.Join(", ", ServicesAnalyzer.AllowedGenericTypes)}. Please change {{0}} in {{1}} to a non-generic type.",
				Category_Services,
				DiagnosticSeverity.Error,
				helpLinkUri: DocsPageHelper.GetCliDocsPageUrl($"{TROUBLESHOOTING_GUIDE_BASE_URL}#invalid-generic-type-in-microservice", Constants.CLI_CURRENT_DOCS_VERSION),
				isEnabledByDefault: true);
	}

	public static class Fed
	{
		//public const string DECLARED_FEDERATION_MISSING_FROM_SOURCE_GEN_CONFIG_DIAGNOSTIC_ID = "BEAM_FED_0001"; This isn't being used anymore, but we kept it here to keep the Diagnostics IDs order.
		//public const string CONFIGURED_FEDERATION_MISSING_FROM_CODE_DIAGNOSTIC_ID = "BEAM_FED_0002"; This isn't being used anymore, but we kept it here to keep the Diagnostics IDs order.
		public const string FEDERATION_CODE_GENERATED_PROPERLY_DIAGNOSTIC_ID = "BEAM_FED_0003";
		public const string DECLARED_FEDERATION_INVALID_FEDERATION_ID_DIAGNOSTIC_ID = "BEAM_FED_0004";
		public const string FEDERATION_ID_MISSING_ATTRIBUTE_DIAGNOSTIC_ID = "BEAM_FED_0005";
		public const string FEDERATION_ID_MUST_BE_DEFAULT_DIAGNOSTIC_ID = "BEAM_FED_0006";
		//public const string FEDERATION_CONFIG_FILE_NOT_FOUND_ID = "BEAM_FED_0007"; This isn't being used anymore, but we kept it here to keep the Diagnostics IDs order.
		//public const string ERROR_PARSING_FEDERATION_CONFIG_FILE_ID = "BEAM_FED_0008"; This isn't being used anymore, but we kept it here to keep the Diagnostics IDs order.
		//public const string FEDERATION_ID_INVALID_CONFIG_FILE_ID = "BEAM_FED_0009"; This isn't being used anymore, but we kept it here to keep the Diagnostics IDs order.

		public const string PROP_FEDERATION_ID = "FederationId";
		public const string PROP_FEDERATION_INTERFACE = "FederationInterface";
		public const string PROP_MICROSERVICE_NAME = "MicroserviceName";
		public const string PROP_FEDERATION_CLASS_NAME = "FederationClassName";
		
		public const string FED_GUIDE_BASE_URL = "cli/guides/ms-federation";

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
				helpLinkUri: DocsPageHelper.GetCliDocsPageUrl($"{FED_GUIDE_BASE_URL}#invalid-federation-id-detected", Constants.CLI_CURRENT_DOCS_VERSION),
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor FederationIdMissingAttribute
			= new(FEDERATION_ID_MISSING_ATTRIBUTE_DIAGNOSTIC_ID,
				$"IFederationId is missing FederationIdAttribute",
				$"The following {nameof(IFederationId)} must be annotated with a {nameof(FederationIdAttribute)}. They must:" +
				$" Start with a letter." +
				$" Contain only alphanumeric characters and/or `_`.",
				Category_Federations,
				DiagnosticSeverity.Error,
				helpLinkUri: DocsPageHelper.GetCliDocsPageUrl($"{FED_GUIDE_BASE_URL}#ifederationid-is-missing-federationidattribute", Constants.CLI_CURRENT_DOCS_VERSION),
				isEnabledByDefault: true);
		
		public static readonly DiagnosticDescriptor FederationIdMustBeDefault
			= new(FEDERATION_ID_MUST_BE_DEFAULT_DIAGNOSTIC_ID,
				$"IFederationId must be \"default\"",
				$"The following {nameof(IFederationId)} must be annotated with a {nameof(FederationIdAttribute)} with a value of \"default\", Id={{0}}",
				Category_Federations,
				DiagnosticSeverity.Error,
				helpLinkUri: DocsPageHelper.GetCliDocsPageUrl($"{FED_GUIDE_BASE_URL}#ifederationid-must-be-default", Constants.CLI_CURRENT_DOCS_VERSION),
				isEnabledByDefault: true);
		
		
	}
}
