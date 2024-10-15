using Beamable.Common;
using Microsoft.CodeAnalysis;

namespace Beamable.Server;

public static class Diagnostics
{
	public const string Category_Config = "BeamableSourceGenerator_Config";
	public const string Category_Services = "BeamableSourceGenerator_Microservices";
	public const string Category_Federations = "BeamableSourceGenerator_Federations";

	public static class Cfg
	{
		public static readonly DiagnosticDescriptor NoSourceGenConfigFound
			= new("BEAM_CFG_O001",
				$"No {nameof(MicroserviceSourceGenConfig)} file found",
				$"No {nameof(MicroserviceSourceGenConfig)} file found. Please run `dotnet beam init` again so this will be created for you.",
				Category_Config,
				DiagnosticSeverity.Error,
				true);

		public static readonly DiagnosticDescriptor MultipleSourceGenConfigsFound
			= new("BEAM_CFG_O002",
				$"Multiple {nameof(MicroserviceSourceGenConfig)} files found",
				$"Multiple {nameof(MicroserviceSourceGenConfig)} files found: {{0}}",
				Category_Config,
				DiagnosticSeverity.Error,
				true);

		public static readonly DiagnosticDescriptor FailedToDeserializeSourceGenConfig
			= new("BEAM_CFG_O003",
				$"{nameof(MicroserviceSourceGenConfig)} could not be deserialized",
				$"{nameof(MicroserviceSourceGenConfig)} could not be deserialized. Ex={{0}}. JSON={{1}}.",
				Category_Config,
				DiagnosticSeverity.Error,
				true);

		public static readonly DiagnosticDescriptor DeserializedSourceGenConfig
			= new("BEAM_CFG_O004",
				$"Loaded {nameof(MicroserviceSourceGenConfig)}",
				$"Loaded {nameof(MicroserviceSourceGenConfig)}. Text={{1}}.",
				Category_Config,
				DiagnosticSeverity.Hidden,
				true);
	}

	public static class Srv
	{
		public static readonly DiagnosticDescriptor NoMicroserviceClassesDetected
			= new("BEAM_SRV_O001",
				$"No {nameof(Microservice)} classes detected",
				$"No {nameof(Microservice)} classes detected. Make sure only a single class implementing {nameof(Microservice)} exists in each service project.",
				Category_Services,
				DiagnosticSeverity.Error,
				true);

		public static readonly DiagnosticDescriptor MultipleMicroserviceClassesDetected
			= new("BEAM_SRV_O002",
				$"Multiple {nameof(Microservice)} classes detected",
				$"Multiple Microservice classes detected. Make sure only a single class implementing {nameof(Microservice)} exists in each service project. ClassNames={{1}}.",
				Category_Services,
				DiagnosticSeverity.Error,
				true);

		public static readonly DiagnosticDescriptor NonPartialMicroserviceClassDetected
			= new("BEAM_SRV_O003",
				$"Non-Partial {nameof(Microservice)} classes detected",
				$"Non-Partial Microservice class detected. Make sure your {nameof(Microservice)} class is marked as `partial`.",
				Category_Services,
				DiagnosticSeverity.Error,
				true);
		
		public static readonly DiagnosticDescriptor MissingMicroserviceId
			= new("BEAM_SRV_O004",
				$"{nameof(Microservice)} class is missing the microservice id",
				$"{nameof(Microservice)} class is missing the microservice id",
				Category_Services,
				DiagnosticSeverity.Error,
				true);
		
		public static readonly DiagnosticDescriptor InvalidMicroserviceId
			= new("BEAM_SRV_O005",
				$"{nameof(Microservice)} ids can only contain alphanumeric characters",
				$"{nameof(Microservice)} ids can only contain alphanumeric characters",
				Category_Services,
				DiagnosticSeverity.Error,
				true);
	}

	public static class Fed
	{
		public static readonly DiagnosticDescriptor DeclaredFederationMissingFromSourceGenConfig
			= new("BEAM_FED_O001",
				$"Missing declared Federation in {nameof(MicroserviceSourceGenConfig)}",
				$"Missing declared Federation in {nameof(MicroserviceSourceGenConfig)}. Microservice={{0}}, Id={{1}}, Interface={{2}}." +
				$" Please add this Id by running `dotnet beam fed add {{0}} {{1}} {{2}}` from your project's root directory." +
				$" Once you do, you may also remove the interface declaration (` : {{2}}`) if you wish as this source generator takes care of adding it for you." +
				$" You can also leave it there, it makes no difference.",
				Category_Federations,
				DiagnosticSeverity.Error,
				true);
		
		public static readonly DiagnosticDescriptor DeclaredFederationMissingFederationId
			= new("BEAM_FED_O002",
				$"Hand-written {nameof(IFederationId)}.{nameof(IFederationId.UniqueName)} cannot be found",
				$"You have a federation using a hand-written {nameof(IFederationId)}. Microservice={{0}}, Id={{1}}, Interface={{2}}." +
				$" You can only declare {nameof(IFederationId.UniqueName)} in the following formats:" +
				$" `public string UniqueName => \"my_name\"`, " +
				$" `public string UniqueName {{ get => \"my_name\" }}`," +
				$" `public string UniqueName {{ get {{ return \"my_name\"; }} }} `." +
				$" Otherwise, our source generator cannot find it.",
				
				Category_Federations,
				DiagnosticSeverity.Error,
				true);
		
		public static readonly DiagnosticDescriptor FederationCodeGeneratedProperly
			= new("BEAM_FED_O003",
				$"All federations we found are valid and we code-gen properly",
				$"All federations we found are valid and we code-gen properly",
				Category_Federations,
				DiagnosticSeverity.Hidden,
				true);
		
		public static readonly DiagnosticDescriptor DeclaredFederationInvalidFederationId
			= new("BEAM_FED_O004",
				$"Invalid Federation Id detected",
				$"The following {nameof(IFederationId)} is invalid. They must:" +
				$" Start with a letter." +
				$" Contain only alphanumeric characters and/or `_`. Microservice={{0}}, Id={{1}}.",
				Category_Federations,
				DiagnosticSeverity.Error,
				true);
		
	}
}
