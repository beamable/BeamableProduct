#pragma warning disable CS0649
namespace cli.Unreal;

public struct UnrealApiSubsystemDeclaration
{
	public string ServiceName;
	public string SubsystemName;

	public List<UnrealFederationDeclaration> DeclaredFederations;

	public List<string> IncludeStatements;

	public List<UnrealEndpointDeclaration> EndpointRawFunctionDeclarations;
	public List<UnrealEndpointDeclaration> AuthenticatedEndpointRawFunctionDeclarations;

	public List<UnrealEndpointDeclaration> EndpointLambdaBindableFunctionDeclarations;
	public List<UnrealEndpointDeclaration> AuthenticatedEndpointLambdaBindableFunctionDeclarations;

	public List<UnrealEndpointDeclaration> EndpointUFunctionDeclarations;
	public List<UnrealEndpointDeclaration> AuthenticatedEndpointUFunctionDeclarations;

	public List<UnrealEndpointDeclaration> EndpointUFunctionWithRetryDeclarations;
	public List<UnrealEndpointDeclaration> AuthenticatedEndpointUFunctionWithRetryDeclarations;

	private string _baseTypeDeclaration;
	private string _assignMicroserviceId;

	public List<UnrealEndpointDeclaration> GetAllEndpoints() => EndpointRawFunctionDeclarations
		.Union(AuthenticatedEndpointRawFunctionDeclarations)
		.Union(EndpointLambdaBindableFunctionDeclarations)
		.Union(AuthenticatedEndpointLambdaBindableFunctionDeclarations)
		.Union(EndpointUFunctionDeclarations)
		.Union(AuthenticatedEndpointUFunctionDeclarations)
		.DistinctBy(d => d.GlobalNamespacedEndpointName).ToList();


	public void IntoProcessMapHeader(Dictionary<string, string> helperDict)
	{
		var endpointRawFunctions = string.Join("\n\t\t", EndpointRawFunctionDeclarations.Select(d =>
		{
			d.IntoProcessMap(helperDict);
			var bpDeclaration = UnrealEndpointDeclaration.RAW_BP_DECLARATION.ProcessReplacement(helperDict);
			var cppDeclaration = UnrealEndpointDeclaration.RAW_CPP_DECLARATION.ProcessReplacement(helperDict);
			helperDict.Clear();

			return bpDeclaration + cppDeclaration;
		}));

		var authEndpointRawFunctions = string.Join("\n\t\t", AuthenticatedEndpointRawFunctionDeclarations.Select(d =>
		{
			d.IntoProcessMap(helperDict);
			var bpDeclaration = UnrealEndpointDeclaration.RAW_AUTH_BP_DECLARATION.ProcessReplacement(helperDict);
			var cppDeclaration = UnrealEndpointDeclaration.RAW_AUTH_CPP_DECLARATION.ProcessReplacement(helperDict);
			helperDict.Clear();
			return bpDeclaration + cppDeclaration;
		}));

		var lambdaBindableFunctions = string.Join("\n\t\t", EndpointLambdaBindableFunctionDeclarations.Select(d =>
		{
			d.IntoProcessMap(helperDict);
			var lambdaBindableDeclaration = UnrealEndpointDeclaration.LAMBDA_BINDABLE_DECLARATIONS.ProcessReplacement(helperDict);
			helperDict.Clear();
			return lambdaBindableDeclaration;
		}));

		var lambdaBindableAuthFunctions = string.Join("\n\t\t", AuthenticatedEndpointLambdaBindableFunctionDeclarations.Select(d =>
		{
			d.IntoProcessMap(helperDict);
			var lambdaBindableDeclaration = UnrealEndpointDeclaration.LAMBDA_BINDABLE_AUTH_DECLARATION.ProcessReplacement(helperDict);
			helperDict.Clear();
			return lambdaBindableDeclaration;
		}));


		var ufunctions = string.Join("\n\t\t", EndpointUFunctionDeclarations.Select(d =>
		{
			d.IntoProcessMap(helperDict);
			var ufunctionDeclaration = UnrealEndpointDeclaration.U_FUNCTION_DECLARATION.ProcessReplacement(helperDict);
			helperDict.Clear();
			return ufunctionDeclaration;
		}));

		var authUFunctions = string.Join("\n\t\t", AuthenticatedEndpointUFunctionDeclarations.Select(d =>
		{
			d.IntoProcessMap(helperDict);
			var ufunctionDeclaration = UnrealEndpointDeclaration.U_FUNCTION_AUTH_DECLARATION.ProcessReplacement(helperDict);
			helperDict.Clear();
			return ufunctionDeclaration;
		}));

		var federations = string.Join("\n\t\t", DeclaredFederations.Select(d =>
		{
			d.IntoProcessMap(helperDict);
			var federationsDeclarations = UnrealFederationDeclaration.FEDERATION_GETTERS_DEFINITION.ProcessReplacement(helperDict);
			helperDict.Clear();
			return federationsDeclarations;
		}));

		var isMsGen = UnrealSourceGenerator.genType == UnrealSourceGenerator.GenerationType.Microservice;

		helperDict.Add(nameof(UnrealSourceGenerator.exportMacro), UnrealSourceGenerator.exportMacro);
		helperDict.Add(nameof(_assignMicroserviceId), isMsGen ? $"MicroserviceName = TEXT(\"{ServiceName}\");" : "");
		helperDict.Add(nameof(_baseTypeDeclaration), isMsGen ? "UBeamMicroserviceClientSubsystem" : "UEngineSubsystem");
		helperDict.Add(nameof(SubsystemName), SubsystemName);

		if (isMsGen) IncludeStatements.Add(@"#include ""BeamBackend/BeamMicroserviceClientSubsystem.h""");
		helperDict.Add(nameof(IncludeStatements), string.Join("\n", IncludeStatements));

		helperDict.Add(nameof(EndpointRawFunctionDeclarations), endpointRawFunctions);
		helperDict.Add(nameof(AuthenticatedEndpointRawFunctionDeclarations), authEndpointRawFunctions);

		helperDict.Add(nameof(EndpointLambdaBindableFunctionDeclarations), lambdaBindableFunctions);
		helperDict.Add(nameof(AuthenticatedEndpointLambdaBindableFunctionDeclarations), lambdaBindableAuthFunctions);

		helperDict.Add(nameof(EndpointUFunctionDeclarations), ufunctions);
		helperDict.Add(nameof(AuthenticatedEndpointUFunctionDeclarations), authUFunctions);

		// Handle federations for microservices.
		helperDict.Add(nameof(DeclaredFederations), !isMsGen ? "// This section is only used in microservice code generation." : federations);
	}

	public void IntoProcessMapCpp(Dictionary<string, string> helperDict)
	{
		var isMsGen = UnrealSourceGenerator.genType == UnrealSourceGenerator.GenerationType.Microservice;
		var endpointRawFunctions = string.Join("\n\t\t", EndpointRawFunctionDeclarations.Select(d =>
		{
			d.IntoProcessMap(helperDict);

			var bpTemplate = isMsGen ? UnrealEndpointDeclaration.RAW_MS_BP_DEFINITION : UnrealEndpointDeclaration.RAW_BP_DEFINITION;
			var cppTemplate = isMsGen ? UnrealEndpointDeclaration.RAW_MS_CPP_DEFINITION : UnrealEndpointDeclaration.RAW_CPP_DEFINITION;

			var bp = bpTemplate.ProcessReplacement(helperDict);
			var cpp = cppTemplate.ProcessReplacement(helperDict);
			helperDict.Clear();

			return bp + cpp;
		}));

		var authEndpointRawFunctions = string.Join("\n\t\t", AuthenticatedEndpointRawFunctionDeclarations.Select(d =>
		{
			d.IntoProcessMap(helperDict);

			var bpTemplate = isMsGen ? UnrealEndpointDeclaration.RAW_MS_AUTH_BP_DEFINITION : UnrealEndpointDeclaration.RAW_AUTH_BP_DEFINITION;
			var cppTemplate = isMsGen ? UnrealEndpointDeclaration.RAW_MS_AUTH_CPP_DEFINITION : UnrealEndpointDeclaration.RAW_AUTH_CPP_DEFINITION;

			var bp = bpTemplate.ProcessReplacement(helperDict);
			var cpp = cppTemplate.ProcessReplacement(helperDict);
			helperDict.Clear();
			return bp + cpp;
		}));

		var lambdaBindableFunctions = string.Join("\n\t\t", EndpointLambdaBindableFunctionDeclarations.Select(d =>
		{
			d.IntoProcessMap(helperDict);
			var lambdaBindable = UnrealEndpointDeclaration.LAMBDA_BINDABLE_DEFINITION.ProcessReplacement(helperDict);
			helperDict.Clear();
			return lambdaBindable;
		}));

		var lambdaBindableAuthFunctions = string.Join("\n\t\t", AuthenticatedEndpointLambdaBindableFunctionDeclarations.Select(d =>
		{
			d.IntoProcessMap(helperDict);
			var lambdaBindable = UnrealEndpointDeclaration.LAMBDA_BINDABLE_AUTH_DEFINITION.ProcessReplacement(helperDict);
			helperDict.Clear();
			return lambdaBindable;
		}));


		var ufunctions = string.Join("\n\t\t", EndpointUFunctionDeclarations.Select(d =>
		{
			d.IntoProcessMap(helperDict);
			var ufunction = UnrealEndpointDeclaration.U_FUNCTION_DEFINITION.ProcessReplacement(helperDict);
			helperDict.Clear();
			return ufunction;
		}));

		var authUFunctions = string.Join("\n\t\t", AuthenticatedEndpointUFunctionDeclarations.Select(d =>
		{
			d.IntoProcessMap(helperDict);
			var ufunction = UnrealEndpointDeclaration.U_FUNCTION_AUTH_DEFINITION.ProcessReplacement(helperDict);
			helperDict.Clear();
			return ufunction;
		}));

		var isMSGen = UnrealSourceGenerator.genType == UnrealSourceGenerator.GenerationType.Microservice;
		helperDict.Add(nameof(UnrealSourceGenerator.exportMacro), UnrealSourceGenerator.exportMacro);
		helperDict.Add(nameof(UnrealSourceGenerator.includeStatementPrefix), UnrealSourceGenerator.includeStatementPrefix);
		helperDict.Add(nameof(SubsystemName), SubsystemName);
		helperDict.Add(nameof(_assignMicroserviceId), isMSGen ? $"MicroserviceName = TEXT(\"{ServiceName}\");" : "");

		helperDict.Add(nameof(EndpointRawFunctionDeclarations), endpointRawFunctions);
		helperDict.Add(nameof(AuthenticatedEndpointRawFunctionDeclarations), authEndpointRawFunctions);

		helperDict.Add(nameof(EndpointLambdaBindableFunctionDeclarations), lambdaBindableFunctions);
		helperDict.Add(nameof(AuthenticatedEndpointLambdaBindableFunctionDeclarations), lambdaBindableAuthFunctions);

		helperDict.Add(nameof(EndpointUFunctionDeclarations), ufunctions);
		helperDict.Add(nameof(AuthenticatedEndpointUFunctionDeclarations), authUFunctions);
	}


	public const string U_SUBSYSTEM_HEADER = $@"

#pragma once

#include ""CoreMinimal.h""
#include ""BeamBackend/BeamBackend.h""
#include ""BeamBackend/ResponseCache/BeamResponseCache.h""
#include ""RequestTracker/BeamRequestTracker.h""

₢{nameof(IncludeStatements)}₢

#include ""Beam₢{nameof(SubsystemName)}₢Api.generated.h""


/**
 * Subsystem containing request calls for the ₢{nameof(SubsystemName)}₢ service.
 */
UCLASS(NotBlueprintType)
class ₢{nameof(UnrealSourceGenerator.exportMacro)}₢ UBeam₢{nameof(SubsystemName)}₢Api : public ₢{nameof(_baseTypeDeclaration)}₢
{{
private:
	GENERATED_BODY()
	/** @brief Initializes the auto-increment Id */
	virtual void Initialize(FSubsystemCollectionBase& Collection) override;

	/** Cleans up the system.  */
	virtual void Deinitialize() override;

	UPROPERTY()
	UBeamBackend* Backend;

	UPROPERTY()
	UBeamRequestTracker* RequestTracker;

	UPROPERTY()
	UBeamResponseCache* ResponseCache;

public:
    ₢{nameof(DeclaredFederations)}₢

private:

	₢{nameof(EndpointRawFunctionDeclarations)}₢

	₢{nameof(AuthenticatedEndpointRawFunctionDeclarations)}₢

public:
	
	/** Used by a helper blueprint node so that you can easily chain requests in BP-land. */
	UFUNCTION(BlueprintPure, BlueprintInternalUseOnly)
	static UBeam₢{nameof(SubsystemName)}₢Api* GetSelf() {{ return GEngine->GetEngineSubsystem<UBeam₢{nameof(SubsystemName)}₢Api>(); }}

	₢{nameof(EndpointLambdaBindableFunctionDeclarations)}₢

	₢{nameof(AuthenticatedEndpointLambdaBindableFunctionDeclarations)}₢

	₢{nameof(EndpointUFunctionDeclarations)}₢

	₢{nameof(AuthenticatedEndpointUFunctionDeclarations)}₢
}};
";

	public const string U_SUBSYSTEM_CPP = $@"
#include ""₢{nameof(UnrealSourceGenerator.includeStatementPrefix)}₢AutoGen/SubSystems/Beam₢{nameof(SubsystemName)}₢Api.h""
#include ""BeamCoreSettings.h""


void UBeam₢{nameof(SubsystemName)}₢Api::Initialize(FSubsystemCollectionBase& Collection)
{{
	Super::Initialize(Collection);
	Backend = Cast<UBeamBackend>(Collection.InitializeDependency(UBeamBackend::StaticClass()));
	RequestTracker = Cast<UBeamRequestTracker>(Collection.InitializeDependency(UBeamRequestTracker::StaticClass()));
	ResponseCache = Cast<UBeamResponseCache>(Collection.InitializeDependency(UBeamResponseCache::StaticClass()));
	₢{nameof(_assignMicroserviceId)}₢
}}

void UBeam₢{nameof(SubsystemName)}₢Api::Deinitialize()
{{
	Super::Deinitialize();
}}

₢{nameof(EndpointRawFunctionDeclarations)}₢

₢{nameof(AuthenticatedEndpointRawFunctionDeclarations)}₢


₢{nameof(EndpointLambdaBindableFunctionDeclarations)}₢

₢{nameof(AuthenticatedEndpointLambdaBindableFunctionDeclarations)}₢


₢{nameof(EndpointUFunctionDeclarations)}₢

₢{nameof(AuthenticatedEndpointUFunctionDeclarations)}₢
";
}
