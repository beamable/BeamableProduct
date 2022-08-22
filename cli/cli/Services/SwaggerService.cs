using Beamable.Common;
using cli.Utils;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Serilog;

namespace cli;

public class SwaggerService
{
	private readonly IAppContext _context;
	private readonly ISwaggerStreamDownloader _downloader;
	private readonly List<ISourceGenerator> _generators;

	// TODO: add in all the other doc strings...
	public static readonly string[] BeamableOpenApis = new[]
	{
		"basic/inventory/platform/docs",
		"object/inventory/platform/docs",
		"basic/accounts/platform/docs",
		"object/accounts/platform/docs",
		"object/leaderboards/platform/docs",
		"basic/leaderboards/platform/docs",
	};

	public SwaggerService(IAppContext context, ISwaggerStreamDownloader downloader, IEnumerable<ISourceGenerator> generators)
	{
		_context = context;
		_downloader = downloader;
		_generators = generators.ToList();
	}

	/// <summary>
	/// Create a list of <see cref="GeneratedFileDescriptor"/> that contain the generated source code
	/// </summary>
	/// <returns></returns>
	public async Task<List<GeneratedFileDescriptor>> Generate()
	{
		// TODO: we should be able to specify if we want to generate from downloading, or from using a cached source.
		var openApiDocuments = await DownloadBeamableApis();

		var allDocuments = openApiDocuments.Select(r => r.Document).ToList();
		var context = new DefaultGenerationContext
		{
			Documents = allDocuments, OrderedSchemas = ExtractAllSchemas(allDocuments)
		};

		// TODO: we shouldn't really be using _all_ the given generators, we should be selecting between one based on an input argument.

		var files = new List<GeneratedFileDescriptor>();
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

	/// <summary>
	/// Download a set of open api documents given the <see cref="openApiUrls"/>
	/// </summary>
	/// <param name="downloader"></param>
	/// <param name="openApiUrls"></param>
	/// <returns>A task that represents the completion of all downloads, and returns the open api docs as the result</returns>
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
				foreach (var warning in res.Diagnostic.Warnings)
				{
					Log.Warning("found warning for {url}. {message} . from {pointer}", url, warning.Message, warning.Pointer);
				}
				foreach (var error in res.Diagnostic.Errors)
				{
					Log.Error("found ERROR for {url}. {message} . from {pointer}", url, error.Message, error.Pointer);
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

	/// <summary>
	/// An interface that abstracts away HOW we generate the source files.
	/// We could have an Unreal implementation, a Godot implementation, a Javascript one? Who knows...
	/// </summary>
	public interface ISourceGenerator
	{
		List<GeneratedFileDescriptor> Generate(IGenerationContext context);
	}

}

public class GeneratedFileDescriptor
{
	/// <summary>
	///  The file name that should be used if this content is written to disk
	/// </summary>
	public string FileName;

	/// <summary>
	/// The source code that should be written to disk
	/// </summary>
	public string Content;
}

public interface IGenerationContext
{
	/// <summary>
	/// All of the open API specs that are being considered for source generation
	/// </summary>
	IReadOnlyList<OpenApiDocument> Documents { get; }

	/// <summary>
	/// All of the open API schema objects across all of the <see cref="Documents"/>.
	/// </summary>
	IReadOnlyList<NamedOpenApiSchema> OrderedSchemas { get; }
}

public class NamedOpenApiSchema
{
	/// <summary>
	/// The name of the openAPI schema, which is not included in the schema itself
	/// </summary>
	public string Name;

	/// <summary>
	/// The document where the schema originated
	/// </summary>
	public OpenApiDocument Document;

	/// <summary>
	/// The openAPI schema itself
	/// </summary>
	public OpenApiSchema Schema;
}
