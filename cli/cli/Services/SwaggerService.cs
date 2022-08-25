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

	// TODO: make a PLAT ticket to give us back all openAPI spec info
	public static readonly BeamableApiDescriptor[] Apis = new BeamableApiDescriptor[]
	{
		// these are currently broken...
		// BeamableApis.BasicService("beamo"),
		// BeamableApis.BasicService("trails"),
		// BeamableApis.BasicService("content"),
		// BeamableApis.ObjectService("event-players"),

		// produces a bad fileName...
		// BeamableApis.ObjectService("group-users"),

		BeamableApis.BasicService("inventory"),
		BeamableApis.ObjectService("inventory"),

		BeamableApis.BasicService("leaderboards"),
		BeamableApis.ObjectService("leaderboards"),

		BeamableApis.BasicService("accounts"),
		BeamableApis.ObjectService("accounts"),

		BeamableApis.BasicService("stats"),
		BeamableApis.ObjectService("stats"),
		//
		BeamableApis.BasicService("events"),
		BeamableApis.ObjectService("events"),
		//
		BeamableApis.BasicService("tournaments"),
		BeamableApis.ObjectService("tournaments"),
		//
		//
		BeamableApis.BasicService("auth"),
		BeamableApis.BasicService("cloudsaving"),
		BeamableApis.BasicService("payments"),
		BeamableApis.ObjectService("payments"),
		BeamableApis.BasicService("push"),
		BeamableApis.BasicService("notification"),
		BeamableApis.BasicService("realms"),
		BeamableApis.BasicService("social"),
		//
		BeamableApis.ObjectService("chatV2"),
		BeamableApis.ObjectService("matchmaking"),
		BeamableApis.ObjectService("groups"),
		BeamableApis.BasicService("commerce"),
		BeamableApis.ObjectService("commerce"),
		BeamableApis.ObjectService("calendars"),
		BeamableApis.BasicService("announcements"),
		BeamableApis.ObjectService("announcements"),
		BeamableApis.BasicService("mail"),
		BeamableApis.ObjectService("mail"),
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
	public async Task<List<GeneratedFileDescriptor>> Generate(BeamableApiFilter filter)
	{
		// TODO: we should be able to specify if we want to generate from downloading, or from using a cached source.
		var openApiDocuments = await DownloadBeamableApis(filter);

		var allDocuments = openApiDocuments.Select(r => r.Document).ToList();
		var context = new DefaultGenerationContext
		{
			Documents = allDocuments,
			OrderedSchemas = ExtractAllSchemas(allDocuments)
		};

		// TODO: FILTER we shouldn't really be using _all_ the given generators, we should be selecting between one based on an input argument.
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
				list.Add(new NamedOpenApiSchema { Name = kvp.Key, Schema = kvp.Value, Document = doc });
			}
		}

		// TODO: there may be duplicate schemas when we are getting lots of documents... we can just de-dupe based on name, but technically, that could be wrong if the server has namespaces...
		list = list.DistinctBy(x => x.Name).ToList();

		return list;
	}

	public async Task<IEnumerable<OpenApiDocumentResult>> DownloadBeamableApis(BeamableApiFilter filter)
	{
		var selected = Apis.Where(filter.Accepts);
		return await DownloadOpenApis(_downloader, selected).ToPromise();//.ShowLoading("fetching swagger docs...");
	}

	/// <summary>
	/// Download a set of open api documents given the <see cref="openApiUrls"/>
	/// </summary>
	/// <param name="downloader"></param>
	/// <param name="openApiUrls"></param>
	/// <returns>A task that represents the completion of all downloads, and returns the open api docs as the result</returns>
	private async Task<IEnumerable<OpenApiDocumentResult>> DownloadOpenApis(ISwaggerStreamDownloader downloader, IEnumerable<BeamableApiDescriptor> openApis)
	{
		var tasks = new List<Task<OpenApiDocumentResult>>();
		foreach (var api in openApis)
		{
			tasks.Add(Task.Run(async () =>
			{
				var url = $"{_context.Host}/{api.RelativeUrl}";
				try
				{
					var stream = await downloader.GetStreamAsync(url);


					var res = new OpenApiDocumentResult();
					res.Document = new OpenApiStreamReader().Read(stream, out res.Diagnostic);
					foreach (var warning in res.Diagnostic.Warnings)
					{
						Log.Warning("found warning for {url}. {message} . from {pointer}", url, warning.Message,
							warning.Pointer);
					}

					foreach (var error in res.Diagnostic.Errors)
					{
						Log.Error("found ERROR for {url}. {message} . from {pointer}", url, error.Message,
							error.Pointer);
					}

					res.Descriptor = api;
					return res;
				}
				catch (Exception ex)
				{

					Log.Fatal(url + " / " + ex.Message);
					throw;
				}
			}));
		}

		var output = await Task.WhenAll(tasks);
		return output;
	}

	public class OpenApiDocumentResult
	{
		public OpenApiDocument Document;
		public OpenApiDiagnostic Diagnostic;
		public BeamableApiDescriptor Descriptor;
	}

	public class DefaultGenerationContext : IGenerationContext
	{
		public IReadOnlyList<OpenApiDocument> Documents { get; init; }
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

public class BeamableApiDescriptor
{
	public BeamableApiSource Source;
	public string RelativeUrl;
	public string Service;
}

public class BeamableApiFilter : DefaultQuery
{
	// TODO: add service type filtering...
	public bool Accepts(BeamableApiDescriptor descriptor)
	{
		return AcceptIdContains(descriptor.Service);
	}

	public static BeamableApiFilter Parse(string text)
	{
		return DefaultQueryParser.Parse(text, StandardRules);
	}

	protected static readonly Dictionary<string, DefaultQueryParser.ApplyParseRule<BeamableApiFilter>> StandardRules = new Dictionary<string, DefaultQueryParser.ApplyParseRule<BeamableApiFilter>>
	{
		// {"t", ApplyTypeParse},
		{"id", DefaultQueryParser.ApplyIdParse},
		// {"tag", ApplyTagParse},
	};
}

public static class BeamableApis
{
	public static BeamableApiDescriptor ProtoActor(string service)
	{
		return new BeamableApiDescriptor
		{
			Source = BeamableApiSource.PLAT_PROTO,
			RelativeUrl = $"basic/{service}/platform/docs",
			Service = service
		};
	}

	public static BeamableApiDescriptor ObjectService(string service)
	{
		return new BeamableApiDescriptor
		{
			Source = BeamableApiSource.PLAT_THOR_OBJECT,
			RelativeUrl = $"object/{service}/platform/docs",
			Service = service
		};
	}

	public static BeamableApiDescriptor BasicService(string service)
	{
		return new BeamableApiDescriptor
		{
			Source = BeamableApiSource.PLAT_THOR_BASIC,
			RelativeUrl = $"basic/{service}/platform/docs",
			Service = service
		};
	}
}

public enum BeamableApiSource
{
	PLAT_THOR_OBJECT,
	PLAT_THOR_BASIC,
	PLAT_PROTO
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
