using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Server;
using Beamable.Tooling.Common.OpenAPI;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;

namespace beamable.tooling.common.Microservice;


public interface IServiceOpenApiDocsCache
{
	OpenApiDocument GetServiceDocument(Type type);
	string GetCachedDocs(string publicHost);
}

public class ServiceOpenApiDocsCache : IServiceOpenApiDocsCache
{
	public ServiceOpenApiDocsCache(IDependencyProvider dependencyProvider)
	{
		_dependencyProvider = dependencyProvider;
	}
	public bool HasCachedDocs => _cachedDocs.HasValue;
	private readonly Optional<string> _cachedDocs = new Optional<string>();
	private readonly IDependencyProvider _dependencyProvider;
	private Dictionary<Type, OpenApiDocument> _serviceDocuments = new Dictionary<Type, OpenApiDocument>();

	public OpenApiDocument GetServiceDocument(Type type)
	{
		if(_serviceDocuments.TryGetValue(type, out var doc))
		{
			return doc;
		}
		var gen = new ServiceDocGenerator();
		doc = gen.GenerateDocumentByType(_dependencyProvider, type);
		_serviceDocuments[type] = doc;
		return doc;
	}

	public string GetCachedDocs(string publicHost)
	{
		if (HasCachedDocs)
		{
			return _cachedDocs.Value;
		}

		var docs = new ServiceDocGenerator();
		var ctx = _dependencyProvider.GetService<StartupContext>();
		var doc = docs.Generate(ctx, _dependencyProvider);
		if (!string.IsNullOrEmpty(publicHost))
		{
			doc.Servers.Add(new OpenApiServer { Url = publicHost });
		}

		var outputString = doc.Serialize(OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Json);
		_cachedDocs.Set(outputString);
		
		return outputString;
	}
}
