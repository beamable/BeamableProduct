using cli.Services.Web.CodeGen;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using static cli.Services.Web.Helpers.StringHelper;
using static cli.Services.Web.Helpers.OpenApiMethodNameGenerator;

namespace cli.Services.Web.Helpers;

public record BuildAndAddMethodParams(
	List<TsFunction> TsFunctions,
	Dictionary<string, TsImport> TsImports,
	List<TsEnum> Enums,
	string FunctionName,
	List<TsFunctionParameter> RequiredParams,
	TsFunctionParameter BodyParam,
	List<TsFunctionParameter> OptionalParams,
	string RequiresAuthRemarks,
	string DeprecatedDoc,
	List<string> ParamCommentList,
	string ResponseType,
	List<TsNode> MethodBodyStatements,
	List<string> Modules,
	List<OpenApiParameter> HeaderParams
);

public static class WebApi
{
	private static readonly HashSet<string> ConstantParamNames = new();

	public static readonly TsFile API_BARREL_FILE = new("index");
	public static readonly TsFile API_CONSTANT_FILE = new("constants");

	public static void BuildApiBarrel(GeneratedFileDescriptor fileDescriptor)
	{
		var fileName = Path.GetFileNameWithoutExtension(fileDescriptor.FileName);
		var tsExport = new TsExport($"./{fileName}");
		API_BARREL_FILE.AddExport(tsExport);
	}

	private static void BuildApiConstants()
	{
		var tsConstants = ConstantParamNames.Select(paramName =>
			new TsVariable($"{paramName}Placeholder").AddModifier(TsModifier.Export).AsConst()
				.WithInitializer(new TsLiteralExpression($"{{{paramName}}}")));

		foreach (var tsConstant in tsConstants)
			API_CONSTANT_FILE.AddDeclaration(tsConstant);
	}

	public static List<GeneratedFileDescriptor> GenerateApiModules(IReadOnlyList<OpenApiDocument> ctxDocuments,
		List<TsEnum> enums)
	{
		var resources = new List<GeneratedFileDescriptor>();
		var apiDeclarations = new Dictionary<string, (Dictionary<string, TsImport>, List<TsFunction>)>();
		var httpRequester = new TsIdentifier("HttpRequester");
		var httpResponse = new TsIdentifier("HttpResponse");

		foreach (var document in ctxDocuments)
		{
			var services = document.Info.Title.Split(' ');
			var serviceName = services.First();
			var serviceType = services.Last();
			var apiName = ToPascalCaseIdentifier(serviceName) + "Api";
			if (apiDeclarations.TryGetValue(apiName, out var declaration))
			{
				GenerateApiMethod(document, declaration.Item1, declaration.Item2, enums, serviceName, serviceType);
				continue;
			}

			var tsImports = new Dictionary<string, TsImport>();
			var tsFunctions = new List<TsFunction>();
			GenerateApiMethod(document, tsImports, tsFunctions, enums, serviceName, serviceType);
			apiDeclarations.Add(apiName, (tsImports, tsFunctions));
		}

		foreach ((string apiName, (Dictionary<string, TsImport> tsImports, List<TsFunction> tsFunctions)) in
		         apiDeclarations)
		{
			var tsFile = new TsFile(apiName);
			var tsImportHttpRequester =
				new TsImport($"@/network/http/types/{httpRequester.Identifier}", typeImportOnly: true).AddNamedImport(
					httpRequester.Identifier);
			var tsImportHttpResponse =
				new TsImport($"@/network/http/types/{httpResponse.Identifier}", typeImportOnly: true).AddNamedImport(
					httpResponse.Identifier);
			var tsImportMakeApiRequest =
				new TsImport("@/utils/makeApiRequest").AddNamedImport("makeApiRequest");

			tsImports.TryAdd("httpRequester", tsImportHttpRequester);
			tsImports.TryAdd("httpResponse", tsImportHttpResponse);
			tsImports.TryAdd("makeApiRequest", tsImportMakeApiRequest);

			// Order by: type-imports last (true → 1), non-type first (false → 0), then alphabetically
			var orderedTsImports = tsImports
				.OrderBy(kvp => kvp.Value.TypeImportOnly ? 1 : 0)
				.ThenBy(kvp => kvp.Key);

			foreach (var kvp in orderedTsImports)
				tsFile.AddImport(kvp.Value);

			foreach (var tsFunction in tsFunctions)
				tsFile.AddDeclaration(tsFunction);

			resources.Add(new GeneratedFileDescriptor
			{
				FileName = $"apis/{tsFile.FileName}.ts", Content = tsFile.Render()
			});
		}

		BuildApiConstants();
		return resources;
	}

	private static void GenerateApiMethod(OpenApiDocument document, Dictionary<string, TsImport> tsImports,
		List<TsFunction> tsFunctions, List<TsEnum> enums, string serviceName, string serviceType)
	{
		foreach (var (apiEndpoint, pathItem) in document.Paths)
		{
			var pathLevelParams = pathItem.Parameters ?? Enumerable.Empty<OpenApiParameter>();
			var headerParams = pathLevelParams
				.Where(p => p.In == ParameterLocation.Header)
				// Exclude 'X-BEAM-SCOPE' header; it is set by default via the Beam Web SDK.
				.Where(p => p.Name != "X-BEAM-SCOPE")
				.ToList();

			foreach (var (httpMethod, operation) in pathItem.Operations)
			{
				ProcessOperation(apiEndpoint, httpMethod, operation, headerParams, tsImports, tsFunctions, enums,
					serviceName, serviceType);
			}
		}
	}

	private static void ProcessOperation(string apiEndpoint, OperationType httpMethod, OpenApiOperation operation,
		List<OpenApiParameter> headerParams, Dictionary<string, TsImport> tsImports, List<TsFunction> tsFunctions,
		List<TsEnum> enums, string serviceName, string serviceType)
	{
		if (!TryGetMediaTypeAndResponseType(operation, out var responseType))
			return;

		AddResponseTypeImport(tsImports, responseType);

		var (requiresAuth, requiresAuthRemarks) = DetermineAuth(operation, serviceName, serviceType);
		var deprecatedDoc = DetermineIfDeprecated(operation);
		var apiMethodType = httpMethod.ToString().ToUpper();
		var methodName = GenerateMethodName(apiEndpoint, apiMethodType);

		var payload = new TsIdentifier("payload");
		var modules = new List<string>();
		var requiredParams = new List<TsFunctionParameter>();
		var optionalParams = new List<TsFunctionParameter>();
		var paramCommentList = new List<string>
		{
			$"@param requester - The `HttpRequester` type to use for the API request."
		};
		var methodBodyStatements = new List<TsNode>();
		var queriesObjectLiteral = new TsObjectLiteralExpression();
		var endpointVariable = new TsVariable("endpoint");

		var hasRequestBody = false;
		var bodyParam = AddBodyParameterIfExist(operation, payload, modules, paramCommentList, ref hasRequestBody,
			out var requestType);

		var apiParameters = operation.Parameters.ToList();
		SortApiParameters(apiParameters);
		ProcessParameters(apiParameters, modules, paramCommentList, requiredParams, optionalParams);

		AddPathParameterStatements(apiParameters, methodBodyStatements, endpointVariable, apiEndpoint, tsImports);
		AddQueryParameterStatements(apiParameters, queriesObjectLiteral);

		tsImports.TryAdd($"{apiMethodType}", new TsImport("@/constants").AddNamedImport($"{apiMethodType}"));
		tsImports.TryAdd("makeApiRequest", new TsImport("@/utils/makeApiRequest").AddNamedImport("makeApiRequest"));

		var propsLiteral = new TsObjectLiteralExpression()
			.AddMember(new TsObjectLiteralMember(new TsIdentifier("r"), new TsIdentifier("requester")))
			.AddMember(new TsObjectLiteralMember(new TsIdentifier("e"), endpointVariable.Identifier))
			.AddMember(new TsObjectLiteralMember(new TsIdentifier("m"), new TsIdentifier(apiMethodType)));

		if (apiParameters.Any(p => p.In == ParameterLocation.Query))
			propsLiteral.AddMember(new TsObjectLiteralMember(new TsIdentifier("q"), queriesObjectLiteral));

		if (hasRequestBody)
			propsLiteral.AddMember(new TsObjectLiteralMember(new TsIdentifier("p"), payload));

		if (headerParams.Count > 0)
		{
			var gamertag = new TsIdentifier("gamertag");
			headerParams.ForEach(headerParam =>
			{
				paramCommentList.Add(
					$"@param {gamertag.Identifier} - {headerParam.Description ?? $"The `{headerParam.Name}` header to include in the API request."}");
			});
			propsLiteral.AddMember(
				new TsObjectLiteralMember(new TsIdentifier("g"), gamertag));
		}

		if (requiresAuth)
			propsLiteral.AddMember(new TsObjectLiteralMember(new TsIdentifier("w"),
				new TsLiteralExpression(true)));

		var apiMethodCall = new TsInvokeExpression(new TsIdentifier("makeApiRequest"), propsLiteral)
			.AddTypeArgument(TsType.Of(responseType));

		if (requestType != null)
			apiMethodCall.AddTypeArgument(requestType);

		methodBodyStatements.Add(new TsBlankLine());
		methodBodyStatements.Add(new TsComment("Make the API request"));
		methodBodyStatements.Add(new TsReturnStatement(apiMethodCall));

		var @params = new BuildAndAddMethodParams(tsFunctions, tsImports, enums, methodName, requiredParams, bodyParam,
			optionalParams, requiresAuthRemarks, deprecatedDoc, paramCommentList, responseType, methodBodyStatements,
			modules, headerParams);
		BuildAndAddMethod(@params);
	}

	private static bool TryGetMediaTypeAndResponseType(OpenApiOperation operation, out string responseType)
	{
		if (!operation.Responses.TryGetValue("200", out var response))
		{
			responseType = null;
			return false;
		}

		var mediaType = response.Content.TryGetValue("application/json", out var jsonMedia)
			? jsonMedia
			: response.Content.Values.FirstOrDefault();
		if (mediaType == null)
		{
			responseType = null;
			return false;
		}

		responseType = mediaType.Schema.Reference.Id;
		return true;
	}

	private static void AddResponseTypeImport(Dictionary<string, TsImport> tsImports, string responseType)
	{
		tsImports.TryAdd(responseType,
			new TsImport($"@/__generated__/schemas/{responseType}", typeImportOnly: true).AddNamedImport(responseType));
	}

	private static (bool requiresAuth, string requiresAuthRemarks) DetermineAuth(
		OpenApiOperation operation, string serviceName, string serviceType)
	{
		var requiresAuth = !serviceType.Equals("basic", StringComparison.InvariantCultureIgnoreCase) ||
		                   serviceName.Contains("inventory", StringComparison.InvariantCultureIgnoreCase) ||
		                   (operation.Security.Count >= 1 &&
		                    operation.Security[0].Any(kvp => kvp.Key.Reference.Id == "user"));
		var remarks = requiresAuth
			? "@remarks\n**Authentication:**\nThis method requires a valid bearer token in the `Authorization` header.\n\n"
			: string.Empty;
		return (requiresAuth, remarks);
	}

	private static string DetermineIfDeprecated(OpenApiOperation operation)
	{
		if (!operation.Deprecated)
			return string.Empty;

		const string deprecatedComment =
			"@deprecated\nThis API method is deprecated and may be removed in future versions.\n\n";
		return deprecatedComment;
	}

	private static TsFunctionParameter AddBodyParameterIfExist(OpenApiOperation operation,
		TsIdentifier payloadIdentifier, List<string> modules, List<string> paramCommentList,
		ref bool hasRequestBody, out TsType requestType)
	{
		TsFunctionParameter bodyParam = null;
		if (operation.RequestBody?.Content?.TryGetValue("application/json", out var requestMediaType) ??
		    false)
		{
			var requestSchema = requestMediaType.Schema;
			requestType = OpenApiTsTypeMapper.Map(requestSchema, ref modules);
			hasRequestBody = true;
			paramCommentList.Add(
				$"@param {payloadIdentifier.Identifier} - The `{requestType.Render()}` instance to use for the API request");
			bodyParam = new TsFunctionParameter(payloadIdentifier.Identifier, requestType);
		}
		else
			requestType = null;

		return bodyParam;
	}

	private static void SortApiParameters(List<OpenApiParameter> apiParameters)
	{
		// Sort parameters:
		//   1) required before optional,
		//   2) within each group, path params before query params,
		//   3) then alphabetical by Name.
		apiParameters.Sort((a, b) =>
		{
			if (a.Name == b.Name)
				return 0; // a and b are the same parameter → a ⪰ b

			switch (a.Required)
			{
				case true when !b.Required:
					return -1; // a is required, b isn’t → a ⪯ b
				case false when b.Required:
					return 1; // a is optional, b is required → a ⪰ b
			}

			if (a.In == ParameterLocation.Path && b.In != ParameterLocation.Path)
				return -1; // a is a path param, b isn’t → a ⪯ b

			if (a.In != ParameterLocation.Path && b.In == ParameterLocation.Path)
				return 1; // a isn’t path, b is → a ⪰ b

			// both are path params or both are query params, sort by name
			return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
		});
	}

	private static void ProcessParameters(List<OpenApiParameter> apiParameters, List<string> modules,
		List<string> paramCommentList, List<TsFunctionParameter> requiredParams,
		List<TsFunctionParameter> optionalParams)
	{
		foreach (var param in apiParameters)
		{
			var paramName = param.Name;
			var paramSchema = param.Schema;
			var paramDescription = !string.IsNullOrEmpty(param.Description)
				? param.Description
				: $"The `{paramName}` parameter to include in the API request.";

			if (paramName == "objectId" && param.Extensions.TryGetValue("x-beamable-object-id", out var ext) &&
			    ext is OpenApiObject obj &&
			    obj.TryGetValue("type", out var customType) && customType is OpenApiString typeStr)
			{
				var customSchema = new OpenApiSchema { Type = typeStr.Value };
				if (obj.TryGetValue("format", out var customFormat) && customFormat is OpenApiString formatStr)
				{
					customSchema.Format = formatStr.Value;
				}

				paramSchema = customSchema;
			}

			var tsType = OpenApiTsTypeMapper.Map(paramSchema, ref modules);
			paramCommentList.Add($"@param {paramName} - {paramDescription}");

			if (param.Required)
				requiredParams.Add(new TsFunctionParameter(paramName, tsType));
			else
				optionalParams.Add(new TsFunctionParameter(paramName, tsType).AsOptional());
		}
	}

	private static void AddPathParameterStatements(List<OpenApiParameter> apiParameters,
		List<TsNode> methodBodyStatements, TsVariable endpoint, string apiEndpoint,
		Dictionary<string, TsImport> tsImports)
	{
		TsExpression endpointReplaceExpression = new TsLiteralExpression(apiEndpoint);

		foreach (var param in apiParameters.Where(p => p.In == ParameterLocation.Path))
		{
			var paramName = param.Name;
			var encodeParamInvocation = new TsInvokeExpression(
				new TsIdentifier("endpointEncoder"), new TsIdentifier(param.Name));
			var tsImportParamNamePlaceholder =
				new TsImport("@/__generated__/apis/constants").AddNamedImport($"{paramName}Placeholder");
			var tsImportEndpointEncoder =
				new TsImport("@/utils/endpointEncoder").AddNamedImport("endpointEncoder");

			tsImports.TryAdd($"{paramName}Placeholder", tsImportParamNamePlaceholder);
			tsImports.TryAdd("endpointEncoder", tsImportEndpointEncoder);

			endpointReplaceExpression = new TsMemberAccessExpression(endpointReplaceExpression, "replace");
			endpointReplaceExpression = new TsInvokeExpression(endpointReplaceExpression,
				new TsIdentifier($"{paramName}Placeholder"), encodeParamInvocation);
			ConstantParamNames.Add(paramName);
		}

		endpoint.WithInitializer(endpointReplaceExpression);
		methodBodyStatements.Add(endpoint);
	}

	private static void AddQueryParameterStatements(List<OpenApiParameter> apiParameters,
		TsObjectLiteralExpression queriesObjectLiteral)
	{
		foreach (var param in apiParameters.Where(p => p.In == ParameterLocation.Query))
		{
			var member = new TsObjectLiteralMember(new TsIdentifier(param.Name), new TsIdentifier(param.Name));
			queriesObjectLiteral.AddMember(member);
		}
	}

	private static void BuildAndAddMethod(BuildAndAddMethodParams p)
	{
		var paramComments = p.ParamCommentList.Count > 0
			? string.Join("\n", p.ParamCommentList) + "\n"
			: string.Empty;
		var tsFunctionReturnType = TsType.Generic("Promise",
			TsType.Generic("HttpResponse", TsType.Of(p.ResponseType)));
		var tsFunctionComment = new TsComment(
			$"{p.RequiresAuthRemarks}{p.DeprecatedDoc}{paramComments}",
			TsCommentStyle.Doc);
		var tsFunction = new TsFunction(p.FunctionName)
			.SetReturnType(tsFunctionReturnType)
			.AddModifier(TsModifier.Export | TsModifier.Async)
			.AddComment(tsFunctionComment);

		var requester = new TsIdentifier("requester");
		tsFunction.AddParameter(new TsFunctionParameter(requester.Identifier, TsType.Of("HttpRequester")));

		// add required path and query parameters
		foreach (var requiredParam in p.RequiredParams)
			tsFunction.AddParameter(requiredParam);

		// add required header parameters
		foreach (var headerParam in p.HeaderParams.Where(param => param.Required))
			tsFunction.AddParameter(new TsFunctionParameter(headerParam.Name, TsType.String));

		// add body parameter if exists
		if (p.BodyParam != null)
			tsFunction.AddParameter(p.BodyParam);

		// add optional query parameters
		foreach (var optionalParam in p.OptionalParams)
			tsFunction.AddParameter(optionalParam);

		// add optional header parameters
		foreach (var headerParam in p.HeaderParams.Where(param => !param.Required))
			tsFunction.AddParameter(new TsFunctionParameter(headerParam.Name.Split('-').Last().ToLower(), TsType.String)
				.AsOptional());

		AddModuleImports(p.TsImports, p.Modules, p.Enums);
		tsFunction.AddBody(p.MethodBodyStatements.ToArray());
		p.TsFunctions.Add(tsFunction);
	}

	private static void AddModuleImports(Dictionary<string, TsImport> tsImports, List<string> modules,
		List<TsEnum> enums)
	{
		foreach (var mod in modules)
		{
			tsImports.TryAdd(mod,
				enums.Any(e => e.Name == mod)
					? new TsImport($"@/__generated__/schemas/enums/{mod}", typeImportOnly: true).AddNamedImport(mod)
					: new TsImport($"@/__generated__/schemas/{mod}", typeImportOnly: true).AddNamedImport(mod));
		}
	}
}
