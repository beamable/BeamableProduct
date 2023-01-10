﻿using System.Text;

namespace cli.Unreal;

public struct UnrealEndpointDeclaration
{
	public string GlobalNamespacedEndpointName;
	public string SubsystemNamespacedEndpointName;
	public string IncludeStatementsUnrealTypes;

	public bool IsAuth;

	public string NamespacedOwnerServiceName;
	public string EndpointVerb;
	public string EndpointPath;
	public List<UnrealPropertyDeclaration> RequestPathParameters;
	public List<UnrealPropertyDeclaration> RequestQueryParameters;


	public List<UnrealPropertyDeclaration> RequestBodyParameters;
	public string ResponseBodyUnrealType;
	public string ResponseBodyNamespacedType;
	public string ResponseBodyNonPtrUnrealType;


	private string _capitalizedEndpointVerb;
	private string _buildBodyImpl;
	private string _buildRouteImpl;

	private string _makeParameterDeclaration;
	private string _makeHiddenParameterNames;
	private string _makeNonBodyImpl;
	private string _makeBodyImpl;


	public List<string> GetAllUnrealTypes()
	{
		return RequestPathParameters
			.Union(RequestQueryParameters)
			.Union(RequestBodyParameters)
			.DistinctBy(p => p.PropertyUnrealType)
			.Select(p => p.PropertyUnrealType)
			.Union(new[] { ResponseBodyUnrealType }).ToList();
	}

	public void IntoProcessMap(Dictionary<string, string> helperDict, List<UnrealSerializableTypeDeclaration> serializableTypes = null)
	{
		var pathParameters = string.Join("\n\t", RequestPathParameters.Select(p =>
		{
			p.IntoProcessMap(helperDict);
			var propertyDeclaration = UnrealPropertyDeclaration.U_PROPERTY_DECLARATION.ProcessReplacement(helperDict);
			helperDict.Clear();
			return propertyDeclaration;
		}));

		var queryParameters = string.Join("\n\t", RequestQueryParameters.Select(p =>
		{
			p.IntoProcessMap(helperDict);
			var propertyDeclaration = UnrealPropertyDeclaration.U_PROPERTY_DECLARATION.ProcessReplacement(helperDict);
			helperDict.Clear();
			return propertyDeclaration;
		}));

		var bodyParameters = string.Join("\n\t", RequestBodyParameters.Select(p =>
		{
			p.IntoProcessMap(helperDict);
			var propertyDeclaration = UnrealPropertyDeclaration.U_PROPERTY_DECLARATION.ProcessReplacement(helperDict);
			helperDict.Clear();
			return propertyDeclaration;
		}));

		// Handle Capitalizing the Verb so it matches UE's expected value
		_capitalizedEndpointVerb = EndpointVerb.ToUpper();
		// Handle BuildBody code generation
		_buildBodyImpl = string.Join("\n\t", RequestBodyParameters.Select(_ => BUILD_BODY_IMPLEMENTATION));

		// Handle Build Route implementation
		var stringBuilder = new StringBuilder(1024);
		stringBuilder.Append($"FString Route = TEXT(\"{EndpointPath}\");\n\t");
		stringBuilder.Append(string.Join("\n\t", RequestPathParameters.Select(BuildReplacePathParamsImpl)));

		stringBuilder.Append("\n\t");
		stringBuilder.Append("\n\t");

		// Builds a string containing all the query params that are set
		stringBuilder.Append("FString QueryParams = TEXT(\"\");\n\t");
		stringBuilder.Append("QueryParams.Reserve(1024);\n\t");
		stringBuilder.Append("bool bIsFirstQueryParam = true;\n\t");
		stringBuilder.Append(string.Join("\n\t", RequestQueryParameters.Select(BuildAppendQueryParamImpl)));

		// Add line that builds the route in its entirety
		stringBuilder.Append("\n\tRouteString.Appendf(TEXT(\"%s%s\"), *Route, *QueryParams);");
		_buildRouteImpl = stringBuilder.ToString();
		stringBuilder.Clear();


		// Handle Make____Request implementation. This one is so that the UX around creating the Request UObject in Blueprint is improved.
		if (serializableTypes != null)
		{
			string GetBodyParamName(List<(string ueType, string propertyName, string paramName)> valueTuples, UnrealPropertyDeclaration bodyParamDeclaration) =>
				valueTuples.Any(tuple => tuple.paramName == GetNonBodyParamName(bodyParamDeclaration)) ? $"Body_{bodyParamDeclaration.PropertyName}" : $"_{bodyParamDeclaration.PropertyName}";

			string GetNonBodyParamName(UnrealPropertyDeclaration bodyParamDeclaration) => $"_{bodyParamDeclaration.PropertyName}";

			string BuildNonBodyParamDeclarationImpl((string ueType, string propertyName, string paramName) nonBodyTypeDecl)
			{
				var (ueType, _, paramName) = nonBodyTypeDecl;
				return $"{ueType} {paramName}";
			}


			IEnumerable<string> BuildBodyParamDeclarationsImpl(UnrealPropertyDeclaration bodyParamDecl, List<(string ueType, string propertyName, string paramName)> list)
			{
				if (bodyParamDecl.PropertyUnrealType.StartsWith(UnrealSourceGenerator.UNREAL_U_OBJECT_PREFIX))
				{
					var serializableType = serializableTypes.First(t => t.NamespacedTypeName == bodyParamDecl.PropertyNamespacedType);
					return serializableType.UPropertyDeclarations.Select(tp => $"{tp.PropertyUnrealType} {GetBodyParamName(list, tp)}");
				}

				return new[] { $"{bodyParamDecl.PropertyUnrealType} {GetBodyParamName(list, bodyParamDecl)}" };
			}

			IEnumerable<string> BuildBodyAdvancedDisplayImpl(UnrealPropertyDeclaration bodyParamDeclaration, List<(string ueType, string propertyName, string paramName)> nonBodyParamsDeclarations1)
			{
				if (bodyParamDeclaration.PropertyUnrealType.StartsWith(UnrealSourceGenerator.UNREAL_U_OBJECT_PREFIX))
				{
					var serializableType = serializableTypes.First(t => t.NamespacedTypeName == bodyParamDeclaration.PropertyNamespacedType);
					return serializableType.UPropertyDeclarations
						.Where(tp => tp.PropertyUnrealType.StartsWith(UnrealSourceGenerator.UNREAL_OPTIONAL))
						.Select(tp => $"{GetBodyParamName(nonBodyParamsDeclarations1, tp)}");
				}

				return new[] { $"{GetBodyParamName(nonBodyParamsDeclarations1, bodyParamDeclaration)}" };
			}


			List<(string ueType, string propertyName, string paramName)> nonBodyParamsDeclarations = new List<(string ueType, string propertyName, string paramName)>();
			nonBodyParamsDeclarations.AddRange(RequestPathParameters.Select(p => (p.PropertyUnrealType, p.PropertyName, GetNonBodyParamName(p))));
			nonBodyParamsDeclarations.AddRange(RequestQueryParameters.Select(p => (p.PropertyUnrealType, p.PropertyName, GetNonBodyParamName(p))));

			// Handle Make parameter declaration
			{
				// UFunctions cannot have parameters in AdvancedDisplay tags that have the same name as fields of the UClass containing the function --- so, we append them with "_"
				var nonBodyParams = nonBodyParamsDeclarations.Select(BuildNonBodyParamDeclarationImpl).ToList();
				stringBuilder.Append(string.Join(", ", nonBodyParams));
				stringBuilder.Append(nonBodyParams.Count > 0 ? ", " : "");

				// Since we are unpacking the RequestBody type's field declarations here, we need to make sure there are no name collisions, that's what BuildBodyParamDeclarationsImpl effectively does.
				// ONLY when a collision happens, we then append the BodyParam's declaration with "Body_" instead of just "_"
				var bodyParams = RequestBodyParameters.SelectMany(p => BuildBodyParamDeclarationsImpl(p, nonBodyParamsDeclarations)).ToList();
				stringBuilder.Append(string.Join(", ", bodyParams));
				stringBuilder.Append(bodyParams.Count > 0 ? ", " : "");

				// Generate the declarations
				_makeParameterDeclaration = stringBuilder.ToString();
				stringBuilder.Clear();
			}


			// Handle Make Annotations
			{
				var nonBodyParams = nonBodyParamsDeclarations.Where(p => p.ueType.StartsWith(UnrealSourceGenerator.UNREAL_OPTIONAL)).Select(p => p.paramName).ToList();
				stringBuilder.Append(string.Join(",", nonBodyParams));
				stringBuilder.Append(nonBodyParams.Count > 0 ? "," : "");

				var bodyParams = RequestBodyParameters.SelectMany(p => BuildBodyAdvancedDisplayImpl(p, nonBodyParamsDeclarations)).ToList();
				stringBuilder.Append(string.Join(",", bodyParams));
				stringBuilder.Append(bodyParams.Count > 0 ? "," : "");

				_makeHiddenParameterNames = stringBuilder.ToString();
			}
			stringBuilder.Clear();

			// Handle Make Impls
			{
				var pathParams = nonBodyParamsDeclarations.Select(p => $"Req->{p.propertyName} = {p.paramName};").ToList();
				stringBuilder.Append(string.Join("\n\t", pathParams));
				stringBuilder.Append(pathParams.Count > 0 ? "\n\t" : "");
				_makeNonBodyImpl = stringBuilder.ToString();
				stringBuilder.Clear();

				// Add implementation for body param fields. We make a new object of the body's type
				var bodyParams = RequestBodyParameters.Select(p =>
				{
					if (p.PropertyUnrealType.StartsWith(UnrealSourceGenerator.UNREAL_U_OBJECT_PREFIX))
					{
						var serializableType = serializableTypes.First(t => t.NamespacedTypeName == p.PropertyNamespacedType);
						var code = $"Req->{p.PropertyName} = NewObject<{UnrealSourceGenerator.RemovePtrFromUnrealTypeIfAny(p.PropertyUnrealType)}>(Req);\n\t";

						code += string.Join("\n\t", serializableType.UPropertyDeclarations.Select(tp =>
						{
							if (tp.PropertyUnrealType.StartsWith(UnrealSourceGenerator.UNREAL_U_OBJECT_PREFIX))
							{
								return $"// Assumes the object is constructed and have the new request take ownership of the memory for it\n\t" +
									   $"Req->{p.PropertyName}->{tp.PropertyName} = {GetBodyParamName(nonBodyParamsDeclarations, tp)};\n\t" +
									   $"Req->{p.PropertyName}->{tp.PropertyName}->Rename(nullptr, Req);";
							}

							return $"Req->{p.PropertyName}->{tp.PropertyName} = {GetBodyParamName(nonBodyParamsDeclarations, tp)};";
						}));

						return code;
					}

					throw new NotImplementedException(
						$"No definition for how to generate the instantiation code for a non-UObject body param ({p.PropertyUnrealType} {p.PropertyName}). Please add a conditional to handle this case.");
				}).ToList();
				stringBuilder.Append(string.Join("\n\t", bodyParams));
				stringBuilder.Append(bodyParams.Count > 0 ? "\n\t" : "");
				_makeBodyImpl = stringBuilder.ToString();
				stringBuilder.Clear();
			}
		}


		helperDict.Add(nameof(GlobalNamespacedEndpointName), GlobalNamespacedEndpointName);
		helperDict.Add(nameof(SubsystemNamespacedEndpointName), SubsystemNamespacedEndpointName);

		helperDict.Add(nameof(NamespacedOwnerServiceName), NamespacedOwnerServiceName);
		helperDict.Add(nameof(EndpointVerb), EndpointVerb);
		helperDict.Add(nameof(EndpointPath), EndpointPath);
		helperDict.Add(nameof(_capitalizedEndpointVerb), _capitalizedEndpointVerb);
		helperDict.Add(nameof(IncludeStatementsUnrealTypes), IncludeStatementsUnrealTypes);

		helperDict.Add(nameof(RequestPathParameters), pathParameters);
		helperDict.Add(nameof(RequestQueryParameters), queryParameters);
		helperDict.Add(nameof(RequestBodyParameters), bodyParameters);
		helperDict.Add(nameof(_buildBodyImpl), _buildBodyImpl);
		helperDict.Add(nameof(_buildRouteImpl), _buildRouteImpl);

		helperDict.Add(nameof(_makeParameterDeclaration), _makeParameterDeclaration);
		helperDict.Add(nameof(_makeHiddenParameterNames), _makeHiddenParameterNames);
		helperDict.Add(nameof(_makeNonBodyImpl), _makeNonBodyImpl);
		helperDict.Add(nameof(_makeBodyImpl), _makeBodyImpl);

		helperDict.Add(nameof(ResponseBodyUnrealType), ResponseBodyUnrealType);
		helperDict.Add(nameof(ResponseBodyNamespacedType), ResponseBodyNamespacedType);
		helperDict.Add(nameof(ResponseBodyNonPtrUnrealType), ResponseBodyNonPtrUnrealType);
	}

	private static string BuildReplacePathParamsImpl(UnrealPropertyDeclaration routeParameterDeclaration)
	{
		// Based on the unreal property we may need to cast it to a string before appending it.
		switch (routeParameterDeclaration.PropertyUnrealType)
		{
			case UnrealSourceGenerator.UNREAL_STRING:
				return $"Route = Route.Replace(TEXT(\"{{{routeParameterDeclaration.RawFieldName}}}\"), *{routeParameterDeclaration.PropertyName});";
			case UnrealSourceGenerator.UNREAL_BYTE or UnrealSourceGenerator.UNREAL_SHORT or UnrealSourceGenerator.UNREAL_INT or UnrealSourceGenerator.UNREAL_LONG:
				return $"Route = Route.Replace(TEXT(\"{{{routeParameterDeclaration.RawFieldName}}}\"), *FString::FromInt({routeParameterDeclaration.PropertyName}));";
			default:
			{
				// We handle the enum case that we can't pattern match here
				if (routeParameterDeclaration.PropertyUnrealType.StartsWith(UnrealSourceGenerator.UNREAL_U_ENUM_PREFIX))
					return
						$"Route = Route.Replace(TEXT(\"{{{routeParameterDeclaration.RawFieldName}}}\"), " +
						$"*U{routeParameterDeclaration.PropertyNamespacedType}Library::{routeParameterDeclaration.PropertyNamespacedType}ToSerializableName({routeParameterDeclaration.PropertyName}));";

				// We fail the gen loudly if we ever see a type that doesn't match this. It should be impossible.
				throw new NotImplementedException("No definition for how to embed a path parameter of this type into the route string. Please add a conditional to handle this case.");
			}
		}
	}

	private static StringBuilder BuildAppendQueryParamImpl(UnrealPropertyDeclaration queryParameterDeclaration)
	{
		var queryAppend = new StringBuilder(1024);

		var q = queryParameterDeclaration;

		// Checks if it is an optional type since we only add optionals if they are set.
		var isOptional = q.PropertyUnrealType.StartsWith(UnrealSourceGenerator.UNREAL_OPTIONAL);

		// Open the if in case it's an optional
		if (isOptional)
			queryAppend.Append($"if({q.PropertyName}.IsSet){{\n\t\t");

		// Append a line to check which symbol we should add
		queryAppend.Append("bIsFirstQueryParam ? QueryParams.Append(TEXT(\"?\")) : QueryParams.Append(TEXT(\"&\"));\n\t");

		// Pattern match with the non-optional type name so that we know if we need to cast the underlying value or not.
		switch (q.NonOptionalTypeName)
		{
			case UnrealSourceGenerator.UNREAL_STRING when isOptional:
				queryAppend.Append($"\tQueryParams.Appendf(TEXT(\"%s=%s\"), TEXT(\"{q.RawFieldName}\"), *{q.PropertyName}.Val);\n\t");
				break;
			case UnrealSourceGenerator.UNREAL_STRING:
				queryAppend.Append($"QueryParams.Appendf(TEXT(\"%s=%s\"), TEXT(\"{q.RawFieldName}\"), *{q.PropertyName});\n\t");
				break;
			case UnrealSourceGenerator.UNREAL_BYTE or UnrealSourceGenerator.UNREAL_SHORT or UnrealSourceGenerator.UNREAL_INT or UnrealSourceGenerator.UNREAL_LONG when isOptional:
				queryAppend.Append($"\tQueryParams.Appendf(TEXT(\"%s=%s\"), TEXT(\"{q.RawFieldName}\"), *FString::FromInt({q.PropertyName}.Val));\n\t");
				break;
			case UnrealSourceGenerator.UNREAL_BYTE or UnrealSourceGenerator.UNREAL_SHORT or UnrealSourceGenerator.UNREAL_INT or UnrealSourceGenerator.UNREAL_LONG:
				queryAppend.Append($"QueryParams.Appendf(TEXT(\"%s=%s\"), TEXT(\"{q.RawFieldName}\"), *FString::FromInt({q.PropertyName}));\n\t");
				break;
			case UnrealSourceGenerator.UNREAL_BOOL when isOptional:
				queryAppend.Append($"\tQueryParams.Appendf(TEXT(\"%s=%s\"), TEXT(\"{q.RawFieldName}\"), {q.PropertyName}.Val ? TEXT(\"true\") : TEXT(\"false\"));\n\t");
				break;
			case UnrealSourceGenerator.UNREAL_BOOL:
				queryAppend.Append($"\tQueryParams.Appendf(TEXT(\"%s=%s\"), TEXT(\"{q.RawFieldName}\"), {q.PropertyName} ? TEXT(\"true\") : TEXT(\"false\"));\n\t");
				break;
			default:
			{
				// Handle the enum case that we can't pattern match with
				if (q.NonOptionalTypeName.StartsWith(UnrealSourceGenerator.UNREAL_U_ENUM_PREFIX))
				{
					if (isOptional)
						queryAppend.Append(
							$"\tQueryParams.Appendf(TEXT(\"%s=%s\"), TEXT(\"{q.RawFieldName}\"), *U{q.PropertyNamespacedType}Library::{q.PropertyNamespacedType}ToSerializationName({q.PropertyName}.Val));\n\t");
					else
						queryAppend.Append(
							$"QueryParams.Appendf(TEXT(\"%s=%s\"), TEXT(\"{q.RawFieldName}\"), *U{q.PropertyNamespacedType}Library::{q.PropertyNamespacedType}ToSerializationName({q.PropertyName}));\n\t");
				}
				// https://disruptorbeam.atlassian.net/browse/PLAT-4672
				// TODO This is a known issue --- so we are ignoring this case for now. Once this gets fixed, remove this thing.
				else if (!q.NonOptionalTypeName.StartsWith(UnrealSourceGenerator.UNREAL_MAP) && !q.NonOptionalTypeName.StartsWith(UnrealSourceGenerator.UNREAL_ARRAY))
				{
					throw new NotImplementedException(
						$"No definition for how to embed a query parameter of this type {q.NonOptionalTypeName} {q.PropertyName} into the route string. Please add a conditional to handle this case.");
				}

				break;
			}
		}

		// Fix indentation due to being inside the if statement for optional params 
		if (isOptional)
			queryAppend.Append("\t");

		// Add a line to inform future query params that one has already been set.
		queryAppend.Append("bIsFirstQueryParam = false;\n\t");

		// Close the if statement if necessary.
		if (isOptional)
			queryAppend.Append("}\n");

		return queryAppend;
	}

	public const string U_ENDPOINT_TEMPLATE_SPECIALIZATIONS = $@"

template TUnrealRequestPtr UBeamBackend::CreateRequest(int64&, const FBeamRealmHandle&, const FBeamRetryConfig&, const U₢{nameof(GlobalNamespacedEndpointName)}₢Request*);
template TUnrealRequestPtr UBeamBackend::CreateAuthenticatedRequest(int64&, const FBeamRealmHandle&, const FBeamRetryConfig&, const FBeamAuthToken&, const U₢{nameof(GlobalNamespacedEndpointName)}₢Request*);

template FBeamRequestProcessor UBeamBackend::MakeBlueprintRequestProcessor<U₢{nameof(GlobalNamespacedEndpointName)}₢Request, ₢{nameof(ResponseBodyNonPtrUnrealType)}₢>(
	const int64&, U₢{nameof(GlobalNamespacedEndpointName)}₢Request*, FOn₢{nameof(GlobalNamespacedEndpointName)}₢Success, FOn₢{nameof(GlobalNamespacedEndpointName)}₢Error, FOn₢{nameof(GlobalNamespacedEndpointName)}₢Complete);

template FBeamRequestProcessor UBeamBackend::MakeAuthenticatedBlueprintRequestProcessor<U₢{nameof(GlobalNamespacedEndpointName)}₢Request, ₢{nameof(ResponseBodyNonPtrUnrealType)}₢>(
	const int64&, const FBeamRealmHandle&, const FBeamAuthToken&, U₢{nameof(GlobalNamespacedEndpointName)}₢Request*, FOn₢{nameof(GlobalNamespacedEndpointName)}₢Success, FOn₢{nameof(GlobalNamespacedEndpointName)}₢Error, FOn₢{nameof(GlobalNamespacedEndpointName)}₢Complete);

template FBeamRequestProcessor UBeamBackend::MakeCodeRequestProcessor(const int64&, U₢{nameof(GlobalNamespacedEndpointName)}₢Request*, TBeamFullResponseHandler<U₢{nameof(GlobalNamespacedEndpointName)}₢Request*, ₢{nameof(ResponseBodyUnrealType)}₢>);
template FBeamRequestProcessor UBeamBackend::MakeAuthenticatedCodeRequestProcessor(const int64&, const FBeamRealmHandle&, const FBeamAuthToken&, U₢{nameof(GlobalNamespacedEndpointName)}₢Request*,
                                                                                   TBeamFullResponseHandler<U₢{nameof(GlobalNamespacedEndpointName)}₢Request*, ₢{nameof(ResponseBodyUnrealType)}₢>);

";

	public const string U_ENDPOINT_HEADER = $@"
#pragma once

#include ""CoreMinimal.h""
#include ""BeamBackend/BeamBaseRequestInterface.h""
#include ""BeamBackend/BeamRequestContext.h""
#include ""BeamBackend/BeamErrorResponse.h""
#include ""BeamBackend/BeamFullResponse.h""

₢{nameof(IncludeStatementsUnrealTypes)}₢

#include ""₢{nameof(GlobalNamespacedEndpointName)}₢Request.generated.h""

UCLASS(BlueprintType)
class BEAMABLECORE_API U₢{nameof(GlobalNamespacedEndpointName)}₢Request : public UObject, public IBeamBaseRequestInterface
{{
	GENERATED_BODY()
	
public:

	// Path Params
	₢{nameof(RequestPathParameters)}₢
	
	// Query Params
	₢{nameof(RequestQueryParameters)}₢

	// Body Params
	₢{nameof(RequestBodyParameters)}₢

	// Beam Base Request Declaration
	U₢{nameof(GlobalNamespacedEndpointName)}₢Request() = default;

	virtual void BuildVerb(FString& VerbString) const override;
	virtual void BuildRoute(FString& RouteString) const override;
	virtual void BuildBody(FString& BodyString) const override;

	UFUNCTION(BlueprintPure, BlueprintInternalUseOnly, Category=""Beam|Backend|₢{nameof(NamespacedOwnerServiceName)}₢"", DisplayName=""Beam - Make ₢{nameof(GlobalNamespacedEndpointName)}₢"",  meta=(DefaultToSelf=""RequestOwner"", AdvancedDisplay=""₢{nameof(_makeHiddenParameterNames)}₢RequestOwner""))
	static U₢{nameof(GlobalNamespacedEndpointName)}₢Request* Make(₢{nameof(_makeParameterDeclaration)}₢UObject* RequestOwner);
}};

UDELEGATE(BlueprintAuthorityOnly)
DECLARE_DYNAMIC_DELEGATE_ThreeParams(FOn₢{nameof(GlobalNamespacedEndpointName)}₢Success, FBeamRequestContext, Context, U₢{nameof(GlobalNamespacedEndpointName)}₢Request*, Request, ₢{nameof(ResponseBodyUnrealType)}₢, Response);

UDELEGATE(BlueprintAuthorityOnly)
DECLARE_DYNAMIC_DELEGATE_ThreeParams(FOn₢{nameof(GlobalNamespacedEndpointName)}₢Error, FBeamRequestContext, Context, U₢{nameof(GlobalNamespacedEndpointName)}₢Request*, Request, FBeamErrorResponse, Error);

UDELEGATE(BlueprintAuthorityOnly)
DECLARE_DYNAMIC_DELEGATE_TwoParams(FOn₢{nameof(GlobalNamespacedEndpointName)}₢Complete, FBeamRequestContext, Context, U₢{nameof(GlobalNamespacedEndpointName)}₢Request*, Request);

using F₢{nameof(GlobalNamespacedEndpointName)}₢FullResponse = FBeamFullResponse<U₢{nameof(GlobalNamespacedEndpointName)}₢Request*, ₢{nameof(ResponseBodyUnrealType)}₢>;
DECLARE_DELEGATE_OneParam(FOn₢{nameof(GlobalNamespacedEndpointName)}₢FullResponse, F₢{nameof(GlobalNamespacedEndpointName)}₢FullResponse);
";

	public const string U_ENDPOINT_CPP = $@"
#include ""AutoGen/SubSystems/₢{nameof(NamespacedOwnerServiceName)}₢/₢{nameof(GlobalNamespacedEndpointName)}₢Request.h""

void U₢{nameof(GlobalNamespacedEndpointName)}₢Request::BuildVerb(FString& VerbString) const
{{
	VerbString = TEXT(""₢{nameof(_capitalizedEndpointVerb)}₢"");
}}

void U₢{nameof(GlobalNamespacedEndpointName)}₢Request::BuildRoute(FString& RouteString) const
{{
	₢{nameof(_buildRouteImpl)}₢		
}}

void U₢{nameof(GlobalNamespacedEndpointName)}₢Request::BuildBody(FString& BodyString) const
{{
	₢{nameof(_buildBodyImpl)}₢
}}

U₢{nameof(GlobalNamespacedEndpointName)}₢Request* U₢{nameof(GlobalNamespacedEndpointName)}₢Request::Make(₢{nameof(_makeParameterDeclaration)}₢UObject* RequestOwner)
{{
	U₢{nameof(GlobalNamespacedEndpointName)}₢Request* Req = NewObject<U₢{nameof(GlobalNamespacedEndpointName)}₢Request>(RequestOwner);

	// Pass in Path and Query Parameters (Blank if no path parameters exist)
	₢{nameof(_makeNonBodyImpl)}₢
	
	// Makes a body and fill up with parameters (Blank if no body parameters exist)
	₢{nameof(_makeBodyImpl)}₢

	return Req;
}}
";


	public const string RAW_BP_DECLARATION = $@"
	/**
	 * @brief Private implementation that all overloaded BP UFunctions call.	  
	 */
	void BP_₢{nameof(SubsystemNamespacedEndpointName)}₢Impl(const FBeamRealmHandle& TargetRealm, const FBeamRetryConfig& RetryConfig, FBeamConnectivity& ConnectivityStatus, U₢{nameof(GlobalNamespacedEndpointName)}₢Request* RequestData,
	                                const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Success& OnSuccess, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Error& OnError, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Complete& OnComplete,
	                                int64& OutRequestId, FBeamOperationHandle OpHandle = FBeamOperationHandle()) const;";

	public const string RAW_BP_DEFINITION = $@"
void UBeam₢{nameof(NamespacedOwnerServiceName)}₢Api::BP_₢{nameof(SubsystemNamespacedEndpointName)}₢Impl(const FBeamRealmHandle& TargetRealm, const FBeamRetryConfig& RetryConfig, FBeamConnectivity& ConnectivityStatus, U₢{nameof(GlobalNamespacedEndpointName)}₢Request* RequestData,
                                                  const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Success& OnSuccess, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Error& OnError, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Complete& OnComplete,
                                                  int64& OutRequestId, FBeamOperationHandle OpHandle) const
{{
	// AUTO-GENERATED...	
	const auto Request = Backend->CreateRequest(OutRequestId, TargetRealm, RetryConfig, RequestData);

	// Binds the handler to the static response handler (pre-generated)
	const auto BeamRequestProcessor = Backend->MakeBlueprintRequestProcessor<U₢{nameof(GlobalNamespacedEndpointName)}₢Request, ₢{nameof(ResponseBodyNonPtrUnrealType)}₢, FOn₢{nameof(GlobalNamespacedEndpointName)}₢Success, FOn₢{nameof(GlobalNamespacedEndpointName)}₢Error, FOn₢{nameof(GlobalNamespacedEndpointName)}₢Complete>
		(OutRequestId, RequestData, OnSuccess, OnError, OnComplete);
	Request->OnProcessRequestComplete().BindLambda(BeamRequestProcessor);

	// Logic that actually talks to the backend --- if you pass in some other delegate, that means you can avoid making the actual back-end call.
	Backend->ExecuteRequestDelegate.ExecuteIfBound(OutRequestId, ConnectivityStatus);
	
	// If we are making this request as part of an operation, we add it to it.
	if(OpHandle.OperationId >= 0)
		RequestTracker->AddRequestToOperation(OpHandle, OutRequestId);
}}
";


	public const string RAW_AUTH_BP_DECLARATION = $@"
	/**
	 * @brief Private implementation for requests that require authentication that all overloaded BP UFunctions call.	  
	 */
	void BP_₢{nameof(SubsystemNamespacedEndpointName)}₢Impl(const FBeamRealmHandle& TargetRealm, const FBeamRetryConfig& RetryConfig, const FBeamAuthToken& AuthToken, FBeamConnectivity& ConnectivityStatus, U₢{nameof(GlobalNamespacedEndpointName)}₢Request* RequestData,
	                  const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Success& OnSuccess, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Error& OnError, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Complete& OnComplete, 
					  int64& OutRequestId, FBeamOperationHandle OpHandle = FBeamOperationHandle()) const;";


	public const string RAW_AUTH_BP_DEFINITION = $@"
void UBeam₢{nameof(NamespacedOwnerServiceName)}₢Api::BP_₢{nameof(SubsystemNamespacedEndpointName)}₢Impl(const FBeamRealmHandle& TargetRealm, const FBeamRetryConfig& RetryConfig, const FBeamAuthToken& AuthToken, FBeamConnectivity& ConnectivityStatus,
                                U₢{nameof(GlobalNamespacedEndpointName)}₢Request* RequestData, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Success& OnSuccess, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Error& OnError, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Complete& OnComplete, 
								int64& OutRequestId, FBeamOperationHandle OpHandle) const
{{
	// AUTO-GENERATED...	
	const auto Request = Backend->CreateAuthenticatedRequest(OutRequestId, TargetRealm, RetryConfig, AuthToken, RequestData);

	// Binds the handler to the static response handler (pre-generated)
	const auto BeamRequestProcessor = Backend->MakeAuthenticatedBlueprintRequestProcessor<U₢{nameof(GlobalNamespacedEndpointName)}₢Request, ₢{nameof(ResponseBodyNonPtrUnrealType)}₢, FOn₢{nameof(GlobalNamespacedEndpointName)}₢Success, FOn₢{nameof(GlobalNamespacedEndpointName)}₢Error, FOn₢{nameof(GlobalNamespacedEndpointName)}₢Complete>
		(OutRequestId, TargetRealm, AuthToken, RequestData, OnSuccess, OnError, OnComplete);
	Request->OnProcessRequestComplete().BindLambda(BeamRequestProcessor);

	// Logic that actually talks to the backend --- if you pass in some other delegate, that means you can avoid making the actual back-end call.	
	Backend->ExecuteRequestDelegate.ExecuteIfBound(OutRequestId, ConnectivityStatus);

	// If we are making this request as part of an operation, we add it to it.
	if(OpHandle.OperationId >= 0)
		RequestTracker->AddRequestToOperation(OpHandle, OutRequestId);
}}
";


	public const string RAW_CPP_DECLARATION = $@"
	/**
	 * @brief Overload version for binding lambdas when in C++ land. Prefer the BP version whenever possible, this is here mostly for quick experimentation purposes.	 
	 */
	void CPP_₢{nameof(SubsystemNamespacedEndpointName)}₢Impl(const FBeamRealmHandle& TargetRealm, const FBeamRetryConfig& RetryConfig, FBeamConnectivity& ConnectivityStatus, U₢{nameof(GlobalNamespacedEndpointName)}₢Request* RequestData,
	                                 const FOn₢{nameof(GlobalNamespacedEndpointName)}₢FullResponse& Handler, int64& OutRequestId, FBeamOperationHandle OpHandle = FBeamOperationHandle()) const;
";

	public const string RAW_CPP_DEFINITION = $@"
void UBeam₢{nameof(NamespacedOwnerServiceName)}₢Api::CPP_₢{nameof(SubsystemNamespacedEndpointName)}₢Impl(const FBeamRealmHandle& TargetRealm, const FBeamRetryConfig& RetryConfig, FBeamConnectivity& ConnectivityStatus,
                                               U₢{nameof(GlobalNamespacedEndpointName)}₢Request* RequestData, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢FullResponse& Handler, int64& OutRequestId, FBeamOperationHandle OpHandle) const
{{
	// AUTO-GENERATED...	
	const auto Request = Backend->CreateRequest(OutRequestId, TargetRealm, RetryConfig, RequestData);

	// Binds the handler to the static response handler (pre-generated)	
	auto ResponseProcessor = Backend->MakeCodeRequestProcessor<U₢{nameof(GlobalNamespacedEndpointName)}₢Request, ₢{nameof(ResponseBodyNonPtrUnrealType)}₢>(OutRequestId, RequestData, Handler);
	Request->OnProcessRequestComplete().BindLambda(ResponseProcessor);

	// Logic that actually talks to the backend --- if you pass in some other delegate, that means you can avoid making the actual back-end call.	
	Backend->ExecuteRequestDelegate.ExecuteIfBound(OutRequestId, ConnectivityStatus);

	// If we are making this request as part of an operation, we add it to it.
	if(OpHandle.OperationId >= 0)
		RequestTracker->AddRequestToOperation(OpHandle, OutRequestId);
}}
";


	public const string RAW_AUTH_CPP_DECLARATION = $@"
	/**
	 * @brief Overload version for binding lambdas when in C++ land. Prefer the BP version whenever possible, this is here mostly for quick experimentation purposes.	 
	 */
	void CPP_₢{nameof(SubsystemNamespacedEndpointName)}₢Impl(const FBeamRealmHandle& TargetRealm, const FBeamRetryConfig& RetryConfig, const FBeamAuthToken& AuthToken, FBeamConnectivity& ConnectivityStatus, U₢{nameof(GlobalNamespacedEndpointName)}₢Request* RequestData,
	                   const FOn₢{nameof(GlobalNamespacedEndpointName)}₢FullResponse& Handler, int64& OutRequestId, FBeamOperationHandle OpHandle = FBeamOperationHandle()) const;";


	public const string RAW_AUTH_CPP_DEFINITION = $@"
void UBeam₢{nameof(NamespacedOwnerServiceName)}₢Api::CPP_₢{nameof(SubsystemNamespacedEndpointName)}₢Impl(const FBeamRealmHandle& TargetRealm, const FBeamRetryConfig& RetryConfig, const FBeamAuthToken& AuthToken, FBeamConnectivity& ConnectivityStatus,
                              U₢{nameof(GlobalNamespacedEndpointName)}₢Request* RequestData, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢FullResponse& Handler, int64& OutRequestId, FBeamOperationHandle OpHandle) const
{{
	// AUTO-GENERATED...
	const auto Request = Backend->CreateAuthenticatedRequest(OutRequestId, TargetRealm, RetryConfig, AuthToken, RequestData);

	// Binds the handler to the static response handler (pre-generated)	
	auto ResponseProcessor = Backend->MakeAuthenticatedCodeRequestProcessor<U₢{nameof(GlobalNamespacedEndpointName)}₢Request, ₢{nameof(ResponseBodyNonPtrUnrealType)}₢>(OutRequestId, TargetRealm, AuthToken, RequestData, Handler);
	Request->OnProcessRequestComplete().BindLambda(ResponseProcessor);

	// Logic that actually talks to the backend --- if you pass in some other delegate, that means you can avoid making the actual back-end call.	
	Backend->ExecuteRequestDelegate.ExecuteIfBound(OutRequestId, ConnectivityStatus);

	// If we are making this request as part of an operation, we add it to it.
	if(OpHandle.OperationId >= 0)
		RequestTracker->AddRequestToOperation(OpHandle, OutRequestId);
}}
";


	public const string LAMBDA_BINDABLE_DECLARATIONS = $@"
	/**
	 * @brief Makes a request to the ₢{nameof(EndpointVerb)}₢ ₢{nameof(EndpointPath)}₢ endpoint of the ₢{nameof(NamespacedOwnerServiceName)}₢ Service.
	 *
	 * PREFER THE UFUNCTION OVERLOAD AS OPPOSED TO THIS. THIS MAINLY EXISTS TO ALLOW LAMBDA BINDING THE HANDLER.
	 * (Dynamic delegates do not allow for that so... we autogen this one to make experimenting in CPP a bit faster and for whenever you need to capture variables).
	 * 
	 * @param Request The Request UObject. All (de)serialized data the request data creates is tied to the lifecycle of this object.
	 * @param Handler A callback that defines how to handle success, error and completion.
     * @param OutRequestContext The Request Context associated with this request -- used to query information about the request or to cancel it while it's in flight.
	 * @param OpHandle When made as part of an Operation, you can pass this in and it'll register the request with the operation automatically. 
	 */
	void CPP_₢{nameof(SubsystemNamespacedEndpointName)}₢(U₢{nameof(GlobalNamespacedEndpointName)}₢Request* Request, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢FullResponse& Handler, FBeamRequestContext& OutRequestContext, FBeamOperationHandle OpHandle = FBeamOperationHandle()) const;
";

	public const string LAMBDA_BINDABLE_DEFINITION = $@"
void UBeam₢{nameof(NamespacedOwnerServiceName)}₢Api::CPP_₢{nameof(SubsystemNamespacedEndpointName)}₢(U₢{nameof(GlobalNamespacedEndpointName)}₢Request* Request, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢FullResponse& Handler, FBeamRequestContext& OutRequestContext, FBeamOperationHandle OpHandle) const
{{
	FBeamRetryConfig RetryConfig;
	Backend->GetRetryConfigForRequestType(U₢{nameof(GlobalNamespacedEndpointName)}₢Request::StaticClass()->GetName(), RetryConfig);
	
    int64 OutRequestId;
	CPP_₢{nameof(SubsystemNamespacedEndpointName)}₢Impl(GetDefault<UBeamCoreSettings>()->TargetRealm, RetryConfig, Backend->CurrentConnectivityStatus, Request, Handler, OutRequestId, OpHandle);
	OutRequestContext = FBeamRequestContext{{OutRequestId, RetryConfig, GetDefault<UBeamCoreSettings>()->TargetRealm, -1, FUserSlot(), None}};
}}
";


	public const string LAMBDA_BINDABLE_AUTH_DECLARATION = $@"
	/**
	 * @brief Makes an authenticated request to the ₢{nameof(EndpointVerb)}₢ ₢{nameof(EndpointPath)}₢ endpoint of the ₢{nameof(NamespacedOwnerServiceName)}₢ Service.
	 *
	 * PREFER THE UFUNCTION OVERLOAD AS OPPOSED TO THIS. THIS MAINLY EXISTS TO ALLOW LAMBDA BINDING THE HANDLER.
	 * (Dynamic delegates do not allow for that so... we autogen this one to make experimenting in CPP a bit faster).
	 * 
	 * @param UserSlot The Authenticated User Slot that is making this request.
	 * @param Request The Request UObject. All (de)serialized data the request data creates is tied to the lifecycle of this object.
	 * @param Handler A callback that defines how to handle success, error and completion.
     * @param OutRequestContext The Request Context associated with this request -- used to query information about the request or to cancel it while it's in flight.
	 * @param OpHandle When made as part of an Operation, you can pass this in and it'll register the request with the operation automatically.
     * @param CallingContext A UObject managed by the world that's making the request. Used to support multiple PIEs (see UBeamUserSlot::GetNamespacedSlotId). 
	 */
	void CPP_₢{nameof(SubsystemNamespacedEndpointName)}₢(const FUserSlot& UserSlot, U₢{nameof(GlobalNamespacedEndpointName)}₢Request* Request, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢FullResponse& Handler, FBeamRequestContext& OutRequestContext, FBeamOperationHandle OpHandle = FBeamOperationHandle(), const UObject* CallingContext = nullptr) const;
";

	public const string LAMBDA_BINDABLE_AUTH_DEFINITION = $@"
void UBeam₢{nameof(NamespacedOwnerServiceName)}₢Api::CPP_₢{nameof(SubsystemNamespacedEndpointName)}₢(const FUserSlot& UserSlot, U₢{nameof(GlobalNamespacedEndpointName)}₢Request* Request, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢FullResponse& Handler, FBeamRequestContext& OutRequestContext, FBeamOperationHandle OpHandle, const UObject* CallingContext) const
{{
	// AUTO-GENERATED...
	FBeamRealmUser AuthenticatedUser;
	Backend->BeamUserSlots->GetUserDataAtSlot(UserSlot, AuthenticatedUser, CallingContext);

	FBeamRetryConfig RetryConfig;
	Backend->GetRetryConfigForUserSlotAndRequestType(U₢{nameof(GlobalNamespacedEndpointName)}₢Request::StaticClass()->GetName(), UserSlot, RetryConfig);

    int64 OutRequestId;
	CPP_₢{nameof(SubsystemNamespacedEndpointName)}₢Impl(AuthenticatedUser.RealmHandle, RetryConfig, AuthenticatedUser.AuthToken, Backend->CurrentConnectivityStatus, Request, Handler, OutRequestId, OpHandle);
	OutRequestContext = FBeamRequestContext{{OutRequestId, RetryConfig, AuthenticatedUser.RealmHandle, -1, UserSlot, None}};
}}
";


	public const string U_FUNCTION_DECLARATION = $@"
	/**
	 * @brief Makes a request to the ₢{nameof(EndpointVerb)}₢ ₢{nameof(EndpointPath)}₢ endpoint of the ₢{nameof(NamespacedOwnerServiceName)}₢ Service.
	 *
	 * @param Request The Request UObject. All (de)serialized data the request data creates is tied to the lifecycle of this object.
	 * @param OnSuccess What to do if the requests receives a successful response.
	 * @param OnError What to do if the request receives an error response.
	 * @param OnComplete What to after either OnSuccess or OnError have finished executing.
	 * @param OutRequestContext The Request Context associated with this request -- used to query information about the request or to cancel it while it's in flight. 
	 */
	UFUNCTION(BlueprintCallable, BlueprintInternalUseOnly, Category=""Beam|Backend|₢{nameof(NamespacedOwnerServiceName)}₢"", meta=(AdvancedDisplay=""OpHandle"", AutoCreateRefTerm=""OnSuccess,OnError,OnComplete,OpHandle"", BeamFlowFunction))
	void ₢{nameof(SubsystemNamespacedEndpointName)}₢(U₢{nameof(GlobalNamespacedEndpointName)}₢Request* Request, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Success& OnSuccess, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Error& OnError, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Complete& OnComplete, FBeamRequestContext& OutRequestContext, FBeamOperationHandle OpHandle = FBeamOperationHandle());
";

	public const string U_FUNCTION_DEFINITION = $@"
void UBeam₢{nameof(NamespacedOwnerServiceName)}₢Api::₢{nameof(SubsystemNamespacedEndpointName)}₢(U₢{nameof(GlobalNamespacedEndpointName)}₢Request* Request, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Success& OnSuccess, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Error& OnError, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Complete& OnComplete, FBeamRequestContext& OutRequestContext, FBeamOperationHandle OpHandle)
{{
	// AUTO-GENERATED...	
	FBeamRetryConfig RetryConfig;
	Backend->GetRetryConfigForRequestType(U₢{nameof(GlobalNamespacedEndpointName)}₢Request::StaticClass()->GetName(), RetryConfig);	
	
	int64 OutRequestId = 0;
	BP_₢{nameof(SubsystemNamespacedEndpointName)}₢Impl(GetDefault<UBeamCoreSettings>()->TargetRealm, RetryConfig, Backend->CurrentConnectivityStatus, Request, OnSuccess, OnError, OnComplete, OutRequestId, OpHandle);
	OutRequestContext = FBeamRequestContext{{OutRequestId, RetryConfig, GetDefault<UBeamCoreSettings>()->TargetRealm, -1, FUserSlot(), None}};
}}
";


	public const string U_FUNCTION_AUTH_DECLARATION = $@"
	/**
	 * @brief Makes an authenticated request to the ₢{nameof(EndpointVerb)}₢ ₢{nameof(EndpointPath)}₢ endpoint of the ₢{nameof(NamespacedOwnerServiceName)}₢ Service.
	 *
	 * @param UserSlot The authenticated UserSlot with the user making the request. 
	 * @param Request The Request UObject. All (de)serialized data the request data creates is tied to the lifecycle of this object.
	 * @param OnSuccess What to do if the requests receives a successful response.
	 * @param OnError What to do if the request receives an error response.
	 * @param OnComplete What to after either OnSuccess or OnError have finished executing.
	 * @param OutRequestContext The Request Context associated with this request -- used to query information about the request or to cancel it while it's in flight.
     * @param CallingContext A UObject managed by the world that's making the request. Used to support multiple PIEs (see UBeamUserSlot::GetNamespacedSlotId).
	 */
	UFUNCTION(BlueprintCallable, BlueprintInternalUseOnly, Category=""Beam|Backend|₢{nameof(NamespacedOwnerServiceName)}₢"", meta=(DefaultToSelf=""CallingContext"", AdvancedDisplay=""OpHandle,CallingContext"",AutoCreateRefTerm=""UserSlot,OnSuccess,OnError,OnComplete,OpHandle"", BeamFlowFunction))
	void ₢{nameof(SubsystemNamespacedEndpointName)}₢(FUserSlot UserSlot, U₢{nameof(GlobalNamespacedEndpointName)}₢Request* Request, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Success& OnSuccess, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Error& OnError, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Complete& OnComplete, FBeamRequestContext& OutRequestContext, FBeamOperationHandle OpHandle = FBeamOperationHandle(), const UObject* CallingContext = nullptr);
";

	public const string U_FUNCTION_AUTH_DEFINITION = $@"
void UBeam₢{nameof(NamespacedOwnerServiceName)}₢Api::₢{nameof(SubsystemNamespacedEndpointName)}₢(FUserSlot UserSlot, U₢{nameof(GlobalNamespacedEndpointName)}₢Request* Request, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Success& OnSuccess, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Error& OnError, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Complete& OnComplete,  FBeamRequestContext& OutRequestContext, FBeamOperationHandle OpHandle, const UObject* CallingContext)
{{
	// AUTO-GENERATED...
	FBeamRealmUser AuthenticatedUser;
	Backend->BeamUserSlots->GetUserDataAtSlot(UserSlot, AuthenticatedUser, CallingContext);

	FBeamRetryConfig RetryConfig;
	Backend->GetRetryConfigForUserSlotAndRequestType(U₢{nameof(GlobalNamespacedEndpointName)}₢Request::StaticClass()->GetName(), UserSlot, RetryConfig);

	int64 OutRequestId;
	BP_₢{nameof(SubsystemNamespacedEndpointName)}₢Impl(AuthenticatedUser.RealmHandle, RetryConfig, AuthenticatedUser.AuthToken, Backend->CurrentConnectivityStatus, Request, OnSuccess, OnError, OnComplete, OutRequestId, OpHandle);	
	OutRequestContext = FBeamRequestContext{{OutRequestId, RetryConfig, AuthenticatedUser.RealmHandle, -1, UserSlot, None}};
}}
";

	public const string U_FUNCTION_WITH_RETRY_DECLARATION = $@"
	/**
	 * @brief Makes a request to the ₢{nameof(EndpointVerb)}₢ ₢{nameof(EndpointPath)}₢ endpoint of the ₢{nameof(NamespacedOwnerServiceName)}₢ Service.
	 *	 
	 * @param RetryConfig The retry config for this specific request. 
	 * @param Request The Request UObject. All (de)serialized data the request data creates is tied to the lifecycle of this object.
	 * @param OnSuccess What to do if the requests receives a successful response.
	 * @param OnError What to do if the request receives an error response.
	 * @param OnComplete What to after either OnSuccess or OnError have finished executing.
	 * @param OutRequestContext The Request Context -- used to query information about the request or to cancel it while it's in flight.	  
	 */
	UFUNCTION(BlueprintCallable, BlueprintInternalUseOnly, Category=""Beam|Backend|₢{nameof(NamespacedOwnerServiceName)}₢"", meta=(AutoCreateRefTerm=""OnSuccess,OnError,OnComplete"", BeamFlowFunction))
	void ₢{nameof(SubsystemNamespacedEndpointName)}₢WithRetry(const FBeamRetryConfig& RetryConfig, U₢{nameof(GlobalNamespacedEndpointName)}₢Request* Request, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Success& OnSuccess, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Error& OnError, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Complete& OnComplete, FBeamRequestContext& OutRequestContext);";

	public const string U_FUNCTION_WITH_RETRY_DEFINITION = $@"
void UBeam₢{nameof(NamespacedOwnerServiceName)}₢Api::₢{nameof(SubsystemNamespacedEndpointName)}₢WithRetry(const FBeamRetryConfig& RetryConfig, U₢{nameof(GlobalNamespacedEndpointName)}₢Request* Request, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Success& OnSuccess, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Error& OnError, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Complete& OnComplete, FBeamRequestContext& OutRequestContext)
{{
	int64 OutRequestId = 0;
	BP_₢{nameof(SubsystemNamespacedEndpointName)}₢Impl(GetDefault<UBeamCoreSettings>()->TargetRealm, RetryConfig, Backend->CurrentConnectivityStatus, Request, OnSuccess, OnError, OnComplete, OutRequestId);
	OutRequestContext = FBeamRequestContext{{OutRequestId, RetryConfig, GetDefault<UBeamCoreSettings>()->TargetRealm, -1, FUserSlot(), None}}; 
}}
";

	public const string U_FUNCTION_WITH_RETRY_AUTH_DECLARATION = $@"
	/**
	 * @brief Makes an authenticated request to the ₢{nameof(EndpointVerb)}₢ ₢{nameof(EndpointPath)}₢ endpoint of the ₢{nameof(NamespacedOwnerServiceName)}₢ Service.
	 *
	 * @param UserSlot The authenticated UserSlot with the user making the request.
	 * @param RetryConfig The retry config for this specific request. 
	 * @param Request The Request UObject. All (de)serialized data the request data creates is tied to the lifecycle of this object.
	 * @param OnSuccess What to do if the requests receives a successful response.
	 * @param OnError What to do if the request receives an error response.
	 * @param OnComplete What to after either OnSuccess or OnError have finished executing.
	 * @param OutRequestContext The Request Context -- used to query information about the request or to cancel it while it's in flight.
	 * @param CallingContext A UObject managed by the UWorld that's making the request. Used to support multiple PIEs (see UBeamUserSlot::GetNamespacedSlotId). 
	 */
	UFUNCTION(BlueprintCallable, BlueprintInternalUseOnly, Category=""Beam|Backend|₢{nameof(NamespacedOwnerServiceName)}₢"", meta=(DefaultToSelf=""CallingContext"", AdvancedDisplay=""CallingContext"", AutoCreateRefTerm=""UserSlot,OnSuccess,OnError,OnComplete"", BeamFlowFunction))
	void ₢{nameof(SubsystemNamespacedEndpointName)}₢WithRetry(FUserSlot UserSlot, const FBeamRetryConfig& RetryConfig, U₢{nameof(GlobalNamespacedEndpointName)}₢Request* Request, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Success& OnSuccess, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Error& OnError, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Complete& OnComplete, FBeamRequestContext& OutRequestContext, const UObject* CallingContext = nullptr);";

	public const string U_FUNCTION_WITH_RETRY_AUTH_DEFINITION = $@"
void UBeam₢{nameof(NamespacedOwnerServiceName)}₢Api::₢{nameof(SubsystemNamespacedEndpointName)}₢WithRetry(FUserSlot UserSlot, const FBeamRetryConfig& RetryConfig, U₢{nameof(GlobalNamespacedEndpointName)}₢Request* Request, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Success& OnSuccess, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Error& OnError, const FOn₢{nameof(GlobalNamespacedEndpointName)}₢Complete& OnComplete, FBeamRequestContext& OutRequestContext, const UObject* CallingContext)
{{
	// AUTO-GENERATED...
	FBeamRealmUser AuthenticatedUser;
	Backend->BeamUserSlots->GetUserDataAtSlot(UserSlot, AuthenticatedUser, CallingContext);	

	int64 OutRequestId;
	BP_₢{nameof(SubsystemNamespacedEndpointName)}₢Impl(AuthenticatedUser.RealmHandle, RetryConfig, AuthenticatedUser.AuthToken, Backend->CurrentConnectivityStatus, Request, OnSuccess, OnError, OnComplete, OutRequestId);	
	OutRequestContext = FBeamRequestContext{{OutRequestId, RetryConfig, AuthenticatedUser.RealmHandle, -1, UserSlot, None}}; 
}}
";

	private const string BUILD_BODY_IMPLEMENTATION = @"ensureAlways(Body);

	TUnrealJsonSerializer JsonSerializer = TJsonStringWriter<TCondensedJsonPrintPolicy<wchar_t>>::Create(&BodyString);
	Body->BeamSerialize(JsonSerializer);
	JsonSerializer->Close();";

	// application/csv --- ignore for now, but will require a separate code path for deserialization.... Probably into DataTables.


	public const string BEAM_FLOW_BP_NODE_HEADER = $@"
#pragma once

#include ""CoreMinimal.h""
#include ""BeamFlow/ApiRequest/K2BeamNode_ApiRequest.h""

#include ""K2BeamNode_ApiRequest_₢{nameof(GlobalNamespacedEndpointName)}₢.generated.h""

#define LOCTEXT_NAMESPACE ""K2BeamNode_ApiRequest_₢{nameof(GlobalNamespacedEndpointName)}₢""

/**
* This is the code-gen'ed declaration for the Beam Flow's Endpoint: ₢{nameof(EndpointVerb)}₢ ₢{nameof(EndpointPath)}₢  of the ₢{nameof(NamespacedOwnerServiceName)}₢ Service. 
*/
UCLASS(meta=(BeamFlow))
class BEAMABLECOREBLUEPRINTNODES_API UK2BeamNode_ApiRequest_₢{nameof(GlobalNamespacedEndpointName)}₢ : public UK2BeamNode_ApiRequest
{{
	GENERATED_BODY()

public:
	virtual FName GetSelfFunctionName() const override;
	virtual FName GetRequestFunctionName() const override;
	virtual FName GetMakeFunctionName() const override;
	virtual FString GetServiceName() const override;
	virtual FString GetEndpointName() const override;
	virtual UClass* GetApiClass() const override;
	virtual UClass* GetRequestClass() const override;
	virtual UClass* GetResponseClass() const override;
	virtual FString GetRequestSuccessDelegateName() const override;
	virtual FString GetRequestErrorDelegateName() const override;
	virtual FString GetRequestCompleteDelegateName() const override;
}};

#undef LOCTEXT_NAMESPACE
";

	public const string BEAM_FLOW_BP_NODE_CPP = $@"

#include ""BeamFlow/ApiRequest/AutoGen/₢{nameof(NamespacedOwnerServiceName)}₢/K2BeamNode_ApiRequest_₢{nameof(GlobalNamespacedEndpointName)}₢.h""

#include ""BeamK2.h""

#include ""AutoGen/SubSystems/Beam₢{nameof(NamespacedOwnerServiceName)}₢Api.h""
#include ""AutoGen/SubSystems/₢{nameof(NamespacedOwnerServiceName)}₢/₢{nameof(GlobalNamespacedEndpointName)}₢Request.h""
#include ""AutoGen/₢{nameof(ResponseBodyNamespacedType)}₢.h""

#define LOCTEXT_NAMESPACE ""K2BeamNode_ApiRequest_₢{nameof(GlobalNamespacedEndpointName)}₢""

using namespace BeamK2;

FName UK2BeamNode_ApiRequest_₢{nameof(GlobalNamespacedEndpointName)}₢::GetSelfFunctionName() const
{{
	return GET_FUNCTION_NAME_CHECKED(UBeam₢{nameof(NamespacedOwnerServiceName)}₢Api, GetSelf);
}}

FName UK2BeamNode_ApiRequest_₢{nameof(GlobalNamespacedEndpointName)}₢::GetRequestFunctionName() const
{{
	return GET_FUNCTION_NAME_CHECKED(UBeam₢{nameof(NamespacedOwnerServiceName)}₢Api, ₢{nameof(SubsystemNamespacedEndpointName)}₢);
}}

FName UK2BeamNode_ApiRequest_₢{nameof(GlobalNamespacedEndpointName)}₢::GetMakeFunctionName() const
{{
	return GET_FUNCTION_NAME_CHECKED(U₢{nameof(GlobalNamespacedEndpointName)}₢Request, Make);
}}

FString UK2BeamNode_ApiRequest_₢{nameof(GlobalNamespacedEndpointName)}₢::GetServiceName() const
{{
	return TEXT(""₢{nameof(NamespacedOwnerServiceName)}₢"");
}}

FString UK2BeamNode_ApiRequest_₢{nameof(GlobalNamespacedEndpointName)}₢::GetEndpointName() const
{{
	return TEXT(""₢{nameof(SubsystemNamespacedEndpointName)}₢"");
}}

UClass* UK2BeamNode_ApiRequest_₢{nameof(GlobalNamespacedEndpointName)}₢::GetApiClass() const
{{
	return UBeam₢{nameof(NamespacedOwnerServiceName)}₢Api::StaticClass();
}}

UClass* UK2BeamNode_ApiRequest_₢{nameof(GlobalNamespacedEndpointName)}₢::GetRequestClass() const
{{
	return U₢{nameof(GlobalNamespacedEndpointName)}₢Request::StaticClass();
}}

UClass* UK2BeamNode_ApiRequest_₢{nameof(GlobalNamespacedEndpointName)}₢::GetResponseClass() const
{{
	return ₢{nameof(ResponseBodyNonPtrUnrealType)}₢::StaticClass();
}}

FString UK2BeamNode_ApiRequest_₢{nameof(GlobalNamespacedEndpointName)}₢::GetRequestSuccessDelegateName() const
{{
	return TEXT(""On₢{nameof(GlobalNamespacedEndpointName)}₢Success"");
}}

FString UK2BeamNode_ApiRequest_₢{nameof(GlobalNamespacedEndpointName)}₢::GetRequestErrorDelegateName() const
{{
	return TEXT(""On₢{nameof(GlobalNamespacedEndpointName)}₢Error"");
}}

FString UK2BeamNode_ApiRequest_₢{nameof(GlobalNamespacedEndpointName)}₢::GetRequestCompleteDelegateName() const
{{
	return TEXT(""On₢{nameof(GlobalNamespacedEndpointName)}₢Complete"");
}}

#undef LOCTEXT_NAMESPACE
";


}
