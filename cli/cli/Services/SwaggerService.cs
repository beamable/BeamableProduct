using Beamable.Common;
using cli.Utils;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace cli;

public class SwaggerService
{
	private readonly IAppContext _context;
	private readonly ISwaggerStreamDownloader _downloader;
	private readonly List<ISourceGenerator> _generators;
	private const string URL = "https://dev.api.beamable.com/basic/inventory/platform/docs";

	public static readonly string[] BeamableOpenApis = new[]
	{
		// "basic/inventory/platform/docs",
		"object/inventory/platform/docs",
		// "basic/accounts/platform/docs",
		// "object/accounts/platform/docs",
		// "object/leaderboards/platform/docs",
		// "basic/leaderboards/platform/docs",
	};

	public SwaggerService(IAppContext context, ISwaggerStreamDownloader downloader, IEnumerable<ISourceGenerator> generators)
	{
		_context = context;
		_downloader = downloader;
		_generators = generators.ToList();
	}

	public async Task<List<GeneratedFileDescriptors>> Generate()
	{
		var results = await DownloadBeamableApis();

		var allDocuments = results.Select(r => r.Document).ToList();
		var context = new DefaultGenerationContext
		{
			Documents = allDocuments, OrderedSchemas = ExtractAllSchemas(allDocuments)
		};

		var files = new List<GeneratedFileDescriptors>();
		foreach (var generator in _generators)
		{
			files.AddRange(generator.Generate(context));
		}

		return files;
	}

	public static List<NamedOpenApiSchema> ExtractAllSchemas(IEnumerable<OpenApiDocument> documents)
	{
		// TODO: sort all the schemas into a thing.
		var list = new List<NamedOpenApiSchema>();

		foreach (var doc in documents)
		{
			foreach (var kvp in doc.Components.Schemas)
			{
				if (string.IsNullOrEmpty(kvp.Key)) continue;
				list.Add(new NamedOpenApiSchema { Name = kvp.Key, Schema = kvp.Value, Document = doc});
			}
		}

		// TODO: there may be duplicate schemas when we are getting lots of documents... we can just de-dupe based on name, but technically, that could be wrong if the server has namespaces...
		list = list.DistinctBy(x => x.Name).ToList();

		return list;
	}

	public async Task<IEnumerable<OpenApiDocumentResult>> DownloadBeamableApis()
	{
		var urls = BeamableOpenApis.Select(api => $"{_context.Host}/{api}");
		return await DownloadOpenApis(_downloader, urls).ToPromise().ShowLoading("fetching swagger docs...");
	}

	public static async Task<IEnumerable<OpenApiDocumentResult>> DownloadOpenApis(ISwaggerStreamDownloader downloader, IEnumerable<string> openApiUrls)
	{
		var tasks = new List<Task<OpenApiDocumentResult>>();
		foreach (var url in openApiUrls)
		{
			tasks.Add(Task.Run(async () =>
			{
				var stream = await downloader.GetStreamAsync(url);
				var res = new OpenApiDocumentResult();
				res.Document = new OpenApiStreamReader().Read(stream, out res.Diagnostic);
				if (res.Diagnostic.Errors.Count > 0 || res.Diagnostic.Warnings.Count > 0)
				{

				}
				return res;
			}));
		}

		var output = await Task.WhenAll(tasks);
		return output;
	}

	public class OpenApiDocumentResult
	{
		public OpenApiDocument Document;
		public OpenApiDiagnostic Diagnostic;
	}

	public class DefaultGenerationContext : IGenerationContext
	{
		public IReadOnlyList<OpenApiDocument> Documents { get; init;  }
		public IReadOnlyList<NamedOpenApiSchema> OrderedSchemas { get; init; }
	}

	public interface ISourceGenerator
	{
		List<GeneratedFileDescriptors> Generate(IGenerationContext context);
	}

}

public class GeneratedFileDescriptors
{
	public string FileName;
	public string Content;
}

public interface IGenerationContext
{
	IReadOnlyList<OpenApiDocument> Documents { get; }
	IReadOnlyList<NamedOpenApiSchema> OrderedSchemas { get; }
}

public class NamedOpenApiSchema
{
	public string Name;
	public OpenApiDocument Document;
	public OpenApiSchema Schema;
}
