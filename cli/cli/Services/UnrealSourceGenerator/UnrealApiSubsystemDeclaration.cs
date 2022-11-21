namespace cli.Unreal;

public struct UnrealApiSubsystemDeclaration
{
	public string SubsystemName;

	public List<string> IncludeStatements;

	public List<UnrealEndpointDeclaration> EndpointRawFunctionDeclarations;
	public List<UnrealEndpointDeclaration> AuthenticatedEndpointRawFunctionDeclarations;

	public List<UnrealEndpointDeclaration> EndpointLambdaBindableFunctionDeclarations;
	public List<UnrealEndpointDeclaration> AuthenticatedEndpointLambdaBindableFunctionDeclarations;

	public List<UnrealEndpointDeclaration> EndpointUFunctionDeclarations;
	public List<UnrealEndpointDeclaration> AuthenticatedEndpointUFunctionDeclarations;

	public List<UnrealEndpointDeclaration> EndpointUFunctionWithRetryDeclarations;
	public List<UnrealEndpointDeclaration> AuthenticatedEndpointUFunctionWithRetryDeclarations;

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

		var ufunctionsWithRetry = string.Join("\n\t\t", EndpointUFunctionDeclarations.Select(d =>
		{
			d.IntoProcessMap(helperDict);
			var ufunction = UnrealEndpointDeclaration.U_FUNCTION_WITH_RETRY_DECLARATION.ProcessReplacement(helperDict);
			helperDict.Clear();
			return ufunction;
		}));

		var authUfunctionsWithRetry = string.Join("\n\t\t", AuthenticatedEndpointUFunctionDeclarations.Select(d =>
		{
			d.IntoProcessMap(helperDict);
			var ufunction = UnrealEndpointDeclaration.U_FUNCTION_WITH_RETRY_AUTH_DECLARATION.ProcessReplacement(helperDict);
			helperDict.Clear();
			return ufunction;
		}));


		helperDict.Add(nameof(SubsystemName), SubsystemName);

		helperDict.Add(nameof(IncludeStatements), string.Join("\n", IncludeStatements));

		helperDict.Add(nameof(EndpointRawFunctionDeclarations), endpointRawFunctions);
		helperDict.Add(nameof(AuthenticatedEndpointRawFunctionDeclarations), authEndpointRawFunctions);

		helperDict.Add(nameof(EndpointLambdaBindableFunctionDeclarations), lambdaBindableFunctions);
		helperDict.Add(nameof(AuthenticatedEndpointLambdaBindableFunctionDeclarations), lambdaBindableAuthFunctions);

		helperDict.Add(nameof(EndpointUFunctionDeclarations), ufunctions);
		helperDict.Add(nameof(AuthenticatedEndpointUFunctionDeclarations), authUFunctions);

		helperDict.Add(nameof(EndpointUFunctionWithRetryDeclarations), ufunctionsWithRetry);
		helperDict.Add(nameof(AuthenticatedEndpointUFunctionWithRetryDeclarations), authUfunctionsWithRetry);
	}

	public void IntoProcessMapCpp(Dictionary<string, string> helperDict)
	{
		var endpointRawFunctions = string.Join("\n\t\t", EndpointRawFunctionDeclarations.Select(d =>
		{
			d.IntoProcessMap(helperDict);
			var bp = UnrealEndpointDeclaration.RAW_BP_DEFINITION.ProcessReplacement(helperDict);
			var cpp = UnrealEndpointDeclaration.RAW_CPP_DEFINITION.ProcessReplacement(helperDict);
			helperDict.Clear();

			return bp + cpp;
		}));

		var authEndpointRawFunctions = string.Join("\n\t\t", AuthenticatedEndpointRawFunctionDeclarations.Select(d =>
		{
			d.IntoProcessMap(helperDict);
			var bp = UnrealEndpointDeclaration.RAW_AUTH_BP_DEFINITION.ProcessReplacement(helperDict);
			var cpp = UnrealEndpointDeclaration.RAW_AUTH_CPP_DEFINITION.ProcessReplacement(helperDict);
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

		var ufunctionsWithRetry = string.Join("\n\t\t", EndpointUFunctionDeclarations.Select(d =>
		{
			d.IntoProcessMap(helperDict);
			var ufunction = UnrealEndpointDeclaration.U_FUNCTION_WITH_RETRY_DEFINITION.ProcessReplacement(helperDict);
			helperDict.Clear();
			return ufunction;
		}));

		var authUfunctionsWithRetry = string.Join("\n\t\t", AuthenticatedEndpointUFunctionDeclarations.Select(d =>
		{
			d.IntoProcessMap(helperDict);
			var ufunction = UnrealEndpointDeclaration.U_FUNCTION_WITH_RETRY_AUTH_DEFINITION.ProcessReplacement(helperDict);
			helperDict.Clear();
			return ufunction;
		}));

		helperDict.Add(nameof(SubsystemName), SubsystemName);

		helperDict.Add(nameof(EndpointRawFunctionDeclarations), endpointRawFunctions);
		helperDict.Add(nameof(AuthenticatedEndpointRawFunctionDeclarations), authEndpointRawFunctions);

		helperDict.Add(nameof(EndpointLambdaBindableFunctionDeclarations), lambdaBindableFunctions);
		helperDict.Add(nameof(AuthenticatedEndpointLambdaBindableFunctionDeclarations), lambdaBindableAuthFunctions);

		helperDict.Add(nameof(EndpointUFunctionDeclarations), ufunctions);
		helperDict.Add(nameof(AuthenticatedEndpointUFunctionDeclarations), authUFunctions);

		helperDict.Add(nameof(EndpointUFunctionWithRetryDeclarations), ufunctionsWithRetry);
		helperDict.Add(nameof(AuthenticatedEndpointUFunctionWithRetryDeclarations), authUfunctionsWithRetry);
	}


	public const string U_SUBSYSTEM_HEADER = $@"

#pragma once

#include ""CoreMinimal.h""
#include ""BeamBackend/BeamBackend.h""
#include ""RequestTracker/BeamRequestTracker.h""

₢{nameof(IncludeStatements)}₢

#include ""Beam₢{nameof(SubsystemName)}₢Api.generated.h""


/**
 * Subsystem containing request calls for the ₢{nameof(SubsystemName)}₢ service.
 */
UCLASS(NotBlueprintType)
class BEAMABLECORE_API UBeam₢{nameof(SubsystemName)}₢Api : public UEngineSubsystem
{{
private:
	GENERATED_BODY()
	/** @brief Initializes the auto-increment Id and binds the ExecuteRequestDelegate to DefaultExecuteRequestImpl  */
	virtual void Initialize(FSubsystemCollectionBase& Collection) override;

	/** Cleans up the system.  */
	virtual void Deinitialize() override;

	UPROPERTY()
	UBeamBackend* Backend;

	UPROPERTY()
	UBeamRequestTracker* RequestTracker;

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

	₢{nameof(EndpointUFunctionWithRetryDeclarations)}₢

	₢{nameof(AuthenticatedEndpointUFunctionWithRetryDeclarations)}₢
}};
";

	public const string U_SUBSYSTEM_CPP = $@"
#include ""AutoGen/SubSystems/Beam₢{nameof(SubsystemName)}₢Api.h""
#include ""BeamCoreSettings.h""


void UBeam₢{nameof(SubsystemName)}₢Api::Initialize(FSubsystemCollectionBase& Collection)
{{
	Super::Initialize(Collection);
	Backend = Cast<UBeamBackend>(Collection.InitializeDependency(UBeamBackend::StaticClass()));
	RequestTracker = Cast<UBeamRequestTracker>(Collection.InitializeDependency(UBeamRequestTracker::StaticClass()));
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

₢{nameof(EndpointUFunctionWithRetryDeclarations)}₢

₢{nameof(AuthenticatedEndpointUFunctionWithRetryDeclarations)}₢

";
}
