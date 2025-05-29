using cli.Services.Web.CodeGen;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using static cli.Services.Web.Helpers.StringHelper;
using static cli.Services.Web.Helpers.OpenApiMethodNameGenerator;

namespace cli.Services.Web.Helpers;

public static class WebApi
{
	public static List<GeneratedFileDescriptor> GenerateApiModules(IReadOnlyList<OpenApiDocument> ctxDocuments,
		List<TsEnum> enums)
	{
		var resources = new List<GeneratedFileDescriptor>();
		var apiDeclarations = new Dictionary<string, (Dictionary<string, TsImport>, TsClass)>();
		var httpMethod = new TsIdentifier("HttpMethod");
		var httpRequester = new TsIdentifier("HttpRequester");
		var httpRequest = new TsIdentifier("HttpRequest");
		var httpResponse = new TsIdentifier("HttpResponse");
		var requester = new TsIdentifier("requester");

		foreach (var document in ctxDocuments)
		{
			var apiName = ToPascalCaseIdentifier(document.Info.Title.Split(' ').First()) + "Api";
			if (apiDeclarations.TryGetValue(apiName, out var declaration))
			{
				GenerateApiMethod(document, declaration.Item1, declaration.Item2, enums);
				continue;
			}

			var tsImports = new Dictionary<string, TsImport>();
			var tsClass = new TsClass(apiName).AddModifier(TsModifier.Export);
			var tsConstructorParam =
				new TsConstructorParameter(requester.Identifier, TsType.Of(httpRequester.Identifier))
					.AddModifier(TsModifier.Private | TsModifier.Readonly);
			var tsConstructor = new TsConstructor().AddParameter(tsConstructorParam);
			tsClass.SetConstructor(tsConstructor);

			GenerateApiMethod(document, tsImports, tsClass, enums);
			apiDeclarations.Add(apiName, (tsImports, tsClass));
		}

		foreach ((string apiName, (Dictionary<string, TsImport> tsImports, TsClass tsApiClass)) in apiDeclarations)
		{
			var tsFile = new TsFile(apiName);
			var tsImportHttpMethod =
				new TsImport($"@/http/types/{httpMethod.Identifier}").AddNamedImport(httpMethod.Identifier);
			var tsImportHttpRequester =
				new TsImport($"@/http/types/{httpRequester.Identifier}").AddNamedImport(httpRequester.Identifier);
			var tsImportHttpRequest =
				new TsImport($"@/http/types/{httpRequest.Identifier}").AddNamedImport(httpRequest.Identifier);
			var tsImportHttpResponse =
				new TsImport($"@/http/types/{httpResponse.Identifier}").AddNamedImport(httpResponse.Identifier);

			tsFile.AddImport(tsImportHttpMethod);
			tsFile.AddImport(tsImportHttpRequester);
			tsFile.AddImport(tsImportHttpRequest);
			tsFile.AddImport(tsImportHttpResponse);
			foreach (var kvp in tsImports)
			{
				tsFile.AddImport(kvp.Value);
			}

			tsFile.AddDeclaration(tsApiClass);

			resources.Add(new GeneratedFileDescriptor
			{
				FileName = $"apis/{tsFile.FileName}.ts", Content = tsFile.Render()
			});
		}

		return resources;
	}

	private static void GenerateApiMethod(OpenApiDocument document, Dictionary<string, TsImport> tsImports,
		TsClass tsClass, List<TsEnum> enums)
	{
		foreach (var (apiEndpoint, pathItem) in document.Paths)
		{
			foreach (var (httpMethod, operation) in pathItem.Operations)
			{
				ProcessOperation(apiEndpoint, httpMethod, operation, tsImports, tsClass, enums);
			}
		}
	}

	private static void ProcessOperation(string apiEndpoint, OperationType httpMethod, OpenApiOperation operation,
		Dictionary<string, TsImport> tsImports, TsClass tsClass, List<TsEnum> enums)
	{
		if (!TryGetMediaTypeAndResponseType(operation, out var responseType))
			return;

		AddResponseTypeImport(tsImports, responseType);

		var (requiresAuth, requiresAuthRemarks) = DetermineAuth(operation);
		var apiMethodType = httpMethod.ToString().ToUpper();
		var methodName = GenerateMethodName(apiEndpoint, apiMethodType);

		var payload = new TsIdentifier("payload");
		var queries = new TsIdentifier("queries");
		var modules = new List<string>();
		var requiredParams = new List<TsFunctionParameter>();
		var optionalParams = new List<TsFunctionParameter>();
		var paramCommentList = new List<string>();
		var methodBodyStatements = new List<TsNode>();
		var queryOptionalStatements = new List<TsNode>();
		var queriesObjectLiteral = new TsObjectLiteralExpression();

		var endpoint = new TsVariable("endpoint").WithInitializer(new TsLiteralExpression(apiEndpoint));
		methodBodyStatements.Add(endpoint);

		var hasRequestBody = false;
		var bodyParam = AddBodyParameterIfExist(operation, payload, modules, paramCommentList, ref hasRequestBody);

		var apiParameters = operation.Parameters.ToList();
		SortApiParameters(apiParameters);
		ProcessParameters(apiParameters, modules, paramCommentList, requiredParams, optionalParams);

		AddPathParameterStatements(apiParameters, methodBodyStatements, endpoint);
		AddQueryParameterStatements(apiParameters, queries, queryOptionalStatements, queriesObjectLiteral);

		TsVariable queryStringDeclaration = null;
		if (apiParameters.Any(p => p.In == ParameterLocation.Query))
		{
			AddQueryStringStatements(methodBodyStatements, queryOptionalStatements, queriesObjectLiteral,
				out queryStringDeclaration);
		}

		var requestDeclaration = AddHttpRequestStatements(methodBodyStatements, payload, hasRequestBody, requiresAuth,
			apiMethodType, endpoint, queryStringDeclaration);

		AddApiCallStatements(methodBodyStatements, responseType, requestDeclaration);

		BuildAndAddMethod(tsClass, tsImports, enums, methodName, requiredParams, bodyParam, optionalParams,
			requiresAuthRemarks, paramCommentList, responseType, methodBodyStatements, modules);
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
			new TsImport($"@/__generated__/schemas/{responseType}").AddNamedImport(responseType));
	}

	private static (bool requiresAuth, string requiresAuthRemarks) DetermineAuth(
		OpenApiOperation operation)
	{
		var requiresAuth = operation.Security.Count >= 1 &&
		                   operation.Security[0].Any(kvp => kvp.Key.Reference.Id == "user");
		var remarks = requiresAuth
			? "@remarks\n**Authentication:**\nThis method requires a valid bearer token in the `Authorization` header.\n\n"
			: string.Empty;
		return (requiresAuth, remarks);
	}

	private static TsFunctionParameter AddBodyParameterIfExist(OpenApiOperation operation,
		TsIdentifier payloadIdentifier, List<string> modules, List<string> paramCommentList,
		ref bool hasRequestBody)
	{
		TsFunctionParameter bodyParam = null;
		if (operation.RequestBody?.Content?.TryGetValue("application/json", out var requestMediaType) ??
		    false)
		{
			var requestSchema = requestMediaType.Schema;
			var requestType = OpenApiTsTypeMapper.Map(requestSchema, ref modules);
			hasRequestBody = true;
			paramCommentList.Add(
				$"@param {payloadIdentifier.Identifier} - The `{requestType.Render()}` instance to use for the API request");
			bodyParam = new TsFunctionParameter(payloadIdentifier.Identifier, requestType);
		}

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
			paramCommentList.Add($"@param {paramName} - {paramDescription}");

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
			if (param.Required)
				requiredParams.Add(new TsFunctionParameter(paramName, tsType));
			else
				optionalParams.Add(new TsFunctionParameter(paramName, tsType).AsOptional());
		}
	}

	private static void AddPathParameterStatements(List<OpenApiParameter> apiParameters,
		List<TsNode> methodBodyStatements, TsVariable endpoint)
	{
		foreach (var param in apiParameters.Where(p => p.In == ParameterLocation.Path))
		{
			var paramToStringExpression = new TsMemberAccessExpression(
				new TsIdentifier(param.Name),
				new TsInvokeExpression(new TsIdentifier("toString")).Render());
			var encodeParamInvocation = new TsInvokeExpression(
				new TsIdentifier("encodeURIComponent"), paramToStringExpression);
			var endpointReplaceExpression =
				new TsMemberAccessExpression(endpoint.Identifier, "replace");
			var endpointReplaceInvocation = new TsInvokeExpression(endpointReplaceExpression,
				new TsLiteralExpression($"{{{param.Name}}}"), encodeParamInvocation);
			var endpointReplaceAssignment =
				new TsAssignmentStatement(endpoint.Identifier, endpointReplaceInvocation);
			methodBodyStatements.Add(endpointReplaceAssignment);
		}
	}

	private static void AddQueryParameterStatements(List<OpenApiParameter> apiParameters,
		TsIdentifier queriesIdentifier, List<TsNode> queryOptionalStatements,
		TsObjectLiteralExpression queriesObjectLiteral)
	{
		foreach (var param in apiParameters.Where(p => p.In == ParameterLocation.Query))
		{
			var queryParamToString = new TsMemberAccessExpression(new TsIdentifier(param.Name),
				new TsInvokeExpression(new TsIdentifier("toString")).Render());
			if (param.Required)
			{
				var member = new TsObjectLiteralMember(new TsLiteralExpression(param.Name),
					queryParamToString);
				queriesObjectLiteral.AddMember(member);
			}
			else
			{
				var queryAssignment = new TsAssignmentStatement(
					new TsMemberAccessExpression(queriesIdentifier, param.Name, false),
					queryParamToString);
				var queryMemberOptional =
					new TsConditionalStatement(new TsBinaryExpression(new TsIdentifier(param.Name),
							TsBinaryOperatorType.NotEqualTo, new TsLiteralExpression("undefined")))
						.AddThen(queryAssignment);
				queryOptionalStatements.Add(queryMemberOptional);
			}
		}
	}

	private static void AddQueryStringStatements(List<TsNode> methodBodyStatements,
		List<TsNode> queryOptionalStatements, TsObjectLiteralExpression queriesObjectLiteral,
		out TsVariable queryStringDeclaration)
	{
		var queriesVar = new TsVariable("queries").AsConst()
			.AsType(TsUtilityType.Record(TsType.String, TsType.String))
			.WithInitializer(queriesObjectLiteral);
		methodBodyStatements.Add(new TsBlankLine());
		methodBodyStatements.Add(new TsComment("Create the query parameters object"));
		methodBodyStatements.Add(queriesVar);
		methodBodyStatements.AddRange(queryOptionalStatements);

		var encodeUriCall = new TsInvokeExpression(new TsIdentifier("encodeURIComponent"),
			new TsIdentifier("value"));
		var templateExpr = new TsTemplateLiteralExpression(
			head: string.Empty,
			new TsTemplateSpan(new TsIdentifier("key"), "="),
			new TsTemplateSpan(encodeUriCall, string.Empty));
		var mapArrow = new TsArrowExpression(
			new TsArrayDestructureParameter("key", "value"), templateExpr);
		var entriesCall = new TsInvokeExpression(
			new TsMemberAccessExpression(new TsIdentifier("Object"), "entries"),
			new TsIdentifier("queries"));
		var mapCall = new TsInvokeExpression(
			new TsMemberAccessExpression(entriesCall, "map"), mapArrow);
		var joinCall = new TsInvokeExpression(
			new TsMemberAccessExpression(mapCall, "join"), new TsLiteralExpression("&"));
		var concatCall = new TsInvokeExpression(
			new TsMemberAccessExpression(new TsLiteralExpression("?"), "concat"), joinCall);
		queryStringDeclaration = new TsVariable("queryString").AsConst()
			.WithInitializer(concatCall);
		methodBodyStatements.Add(new TsBlankLine());
		methodBodyStatements.Add(new TsComment("Create the query string from the query parameters"));
		methodBodyStatements.Add(queryStringDeclaration);
	}

	private static TsVariable AddHttpRequestStatements(List<TsNode> methodBodyStatements,
		TsIdentifier payloadIdentifier, bool hasRequestBody, bool requiresAuth, string apiMethodType,
		TsVariable endpoint, TsVariable queryStringDeclaration)
	{
		var urlKey = new TsIdentifier("url");
		var methodKey = new TsIdentifier("method");
		var bodyKey = new TsIdentifier("body");
		var withAuthKey = new TsIdentifier("withAuth");
		var requestUrl = queryStringDeclaration != null
			? new TsInvokeExpression(new TsMemberAccessExpression(endpoint.Identifier, "concat"),
				queryStringDeclaration.Identifier)
			: endpoint.Identifier;
		var requestMethod =
			new TsMemberAccessExpression(new TsIdentifier("HttpMethod"), apiMethodType);
		var requestObjectLiteral = new TsObjectLiteralExpression()
			.AddMember(new TsObjectLiteralMember(urlKey, requestUrl))
			.AddMember(new TsObjectLiteralMember(methodKey, requestMethod));

		if (hasRequestBody)
		{
			var stringifyPayload =
				new TsInvokeExpression(new TsMemberAccessExpression(new TsIdentifier("JSON"), "stringify"),
					payloadIdentifier);
			requestObjectLiteral.AddMember(bodyKey, stringifyPayload);
		}

		if (requiresAuth)
			requestObjectLiteral.AddMember(withAuthKey, new TsLiteralExpression(true));

		var requestDeclaration = new TsVariable("req").AsConst()
			.AsType(TsType.Of("HttpRequest"))
			.WithInitializer(requestObjectLiteral);
		methodBodyStatements.Add(new TsBlankLine());
		methodBodyStatements.Add(new TsComment("Create the HTTP request object"));
		methodBodyStatements.Add(requestDeclaration);
		return requestDeclaration;
	}

	private static void AddApiCallStatements(List<TsNode> methodBodyStatements, string responseType,
		TsVariable requestDeclaration)
	{
		var theRequester = new TsMemberAccessExpression(new TsIdentifier("this"), "requester");
		var theRequestFn = new TsMemberAccessExpression(theRequester, "request");
		var apiMethodCall = new TsInvokeExpression(theRequestFn, requestDeclaration.Identifier)
			.AddTypeArgument(TsType.Of(responseType));
		var apiMethodCallReturn = new TsReturnStatement(apiMethodCall);
		methodBodyStatements.Add(new TsBlankLine());
		methodBodyStatements.Add(new TsComment("Make the API request"));
		methodBodyStatements.Add(apiMethodCallReturn);
	}

	private static void BuildAndAddMethod(TsClass tsClass, Dictionary<string, TsImport> tsImports,
		List<TsEnum> enums, string methodName, List<TsFunctionParameter> requiredParams,
		TsFunctionParameter bodyParam, List<TsFunctionParameter> optionalParams, string requiresAuthRemarks,
		List<string> paramCommentList, string responseType, List<TsNode> methodBodyStatements,
		List<string> modules)
	{
		var paramComments = paramCommentList.Count > 0
			? string.Join("\n", paramCommentList) + "\n"
			: string.Empty;
		var tsMethodReturnType = TsType.Generic("Promise",
			TsType.Generic("HttpResponse", TsType.Of(responseType)));
		var tsMethodComment = new TsComment(
			$"{requiresAuthRemarks}{paramComments}@returns {{{tsMethodReturnType.Render()}}} A promise containing the HttpResponse of {responseType}",
			TsCommentStyle.Doc);
		var tsMethod = new TsMethod(methodName)
			.SetReturnType(tsMethodReturnType)
			.AddModifier(TsModifier.Async)
			.AddComment(tsMethodComment);
		foreach (var requiredParam in requiredParams)
			tsMethod.AddParameter(requiredParam);
		if (bodyParam != null)
			tsMethod.AddParameter(bodyParam);
		foreach (var optionalParam in optionalParams)
			tsMethod.AddParameter(optionalParam);

		AddModuleImports(tsImports, modules, enums);
		tsMethod.AddBody(methodBodyStatements.ToArray());
		tsClass.AddMethod(tsMethod);
	}

	private static void AddModuleImports(Dictionary<string, TsImport> tsImports, List<string> modules,
		List<TsEnum> enums)
	{
		foreach (var mod in modules)
		{
			tsImports.TryAdd(mod,
				enums.Any(e => e.Name == mod)
					? new TsImport($"@/__generated__/schemas/enums/{mod}").AddNamedImport(mod)
					: new TsImport($"@/__generated__/schemas/{mod}").AddNamedImport(mod));
		}
	}
}
