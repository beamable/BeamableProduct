using Beamable.Common;
using cli.Utils;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Exceptions;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Writers;
using Newtonsoft.Json;
using Serilog;
using System.Text;

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
		// BeamableApis.BasicService("content"),
		// BeamableApis.BasicService("trails"),

		// behold the list of Beamable Apis
		BeamableApis.BasicService("beamo"),
		BeamableApis.ObjectService("event-players"),
		BeamableApis.BasicService("events"),
		BeamableApis.ObjectService("events"),
		BeamableApis.ObjectService("group-users"),
		BeamableApis.ObjectService("groups"),
		BeamableApis.BasicService("inventory"),
		BeamableApis.ObjectService("inventory"),
		BeamableApis.BasicService("leaderboards"),
		BeamableApis.ObjectService("leaderboards"),
		BeamableApis.BasicService("accounts"),
		BeamableApis.ObjectService("accounts"),
		BeamableApis.BasicService("stats"),
		BeamableApis.ObjectService("stats"),
		BeamableApis.BasicService("tournaments"),
		BeamableApis.ObjectService("tournaments"),
		BeamableApis.BasicService("auth"),
		BeamableApis.BasicService("cloudsaving"),
		BeamableApis.BasicService("payments"),
		BeamableApis.ObjectService("payments"),
		BeamableApis.BasicService("push"),
		BeamableApis.BasicService("notification"),
		BeamableApis.BasicService("realms"),
		BeamableApis.BasicService("social"),
		BeamableApis.ObjectService("chatV2"),
		BeamableApis.ObjectService("matchmaking"),
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
	public async Task<List<GeneratedFileDescriptor>> Generate(BeamableApiFilter filter, GenerateSdkConflictResolutionStrategy resolutionStrategy)
	{
		// TODO: we should be able to specify if we want to generate from downloading, or from using a cached source.
		var openApiDocuments = await DownloadBeamableApis(filter);

		var allDocuments = openApiDocuments.Select(r => r.Document).ToList();
		var context = new DefaultGenerationContext
		{
			Documents = allDocuments,
			OrderedSchemas = ExtractAllSchemas(allDocuments, resolutionStrategy)
		};

		// TODO: FILTER we shouldn't really be using _all_ the given generators, we should be selecting between one based on an input argument.
		var files = new List<GeneratedFileDescriptor>();
		foreach (var generator in _generators)
		{
			files.AddRange(generator.Generate(context));
		}

		return files;
	}

	public static List<NamedOpenApiSchema> ExtractAllSchemas(IEnumerable<OpenApiDocument> documents, GenerateSdkConflictResolutionStrategy resolutionStrategy)
	{
		// TODO: sort all the schemas into a thing.
		var list = new List<NamedOpenApiSchema>();

		string SerializeSchema(OpenApiSchema schema)
		{
			using var sw = new StringWriter();
			var oldTitle = schema.Title;
			schema.Title = string.Empty;
			var writer = new OpenApiJsonWriter(sw);
			schema.SerializeAsV3WithoutReference(writer);
			var json = sw.ToString();
			schema.Title = oldTitle;
			return json;
		}

		foreach (var doc in documents)
		{
			foreach (var kvp in doc.Components.Schemas)
			{
				if (string.IsNullOrEmpty(kvp.Key)) continue;

				var reader = new OpenApiStreamReader();
				var originalJson = SerializeSchema(kvp.Value);
				var stream = new MemoryStream(Encoding.UTF8.GetBytes(originalJson));
				var raw = reader.ReadFragment<OpenApiSchema>(stream, OpenApiSpecVersion.OpenApi3_0, out _);
				// var raw = JsonConvert.DeserializeObject<OpenApiSchema>(originalJson);
				list.Add(new NamedOpenApiSchema
				{
					RawSchema = raw.GetEffective(doc),
					Name = kvp.Key,
					Schema = kvp.Value,
					Document = doc
				});
			}
		}

		void HandleResolution()
		{
			var groups = list.GroupBy(s => s.Name).ToList();
			foreach (var group in groups)
			{
				if (group.Count() <= 1) continue;

				// found dupe...
				var elements = group.ToList();

				var uniqueElementGroups = elements.GroupBy(e => SerializeSchema(e.Schema)).ToList();
				if (uniqueElementGroups.Count <= 1)
					continue; // there is only 1 variant of the serialization, so its "fine", and we don't need to do anything

				/*
					 * There are multiple serializations of the model, which means we need to re-wire each of them to point to
					 * their own specific implementation :(
					 *
					 * But if there is one variant that has distinctly _more_ references, we should assume it is a common one.
					 */

				// find the variant with the most entries...
				var variants = uniqueElementGroups.ToList();
				IGrouping<string, NamedOpenApiSchema> highest = null;
				foreach (var variant in variants)
				{
					var size = variant.Count();
					if (highest == null || size > highest.Count())
					{
						highest = variant;
					}
				}

				// if any other variant has the name number of entries as the highest, then there is no "highest"
				foreach (var variant in variants)
				{
					if (variant != highest && variant.Count() == highest.Count())
					{
						highest = null;
						break;
					}
				}

				foreach (var variant in variants)
				{
					if (variant == highest && resolutionStrategy == GenerateSdkConflictResolutionStrategy.RenameUncommonConflicts)
						continue; // if this is the variant with the most entries, then it wins the naming war and doesn't need to change name.

					foreach (var instance in variant)
					{
						var serviceTitle = string.Concat(instance.Document.Info.Title
							.Split(' ')
							.Select(w => char.ToUpper(w[0]) + w.Substring(1)));
						var newName = serviceTitle + instance.Name;
						var oldName = instance.Name;
						instance.Name = newName;

						void RewireSchema(OpenApiSchema schema)
						{
							if (schema.Reference?.Id == oldName)
							{
								schema.Reference.Id = newName;
							}

							if (schema.AdditionalProperties?.Reference?.Id == oldName)
							{
								schema.AdditionalProperties.Reference.Id = newName;
							}

							if (schema.Items?.Reference?.Id == oldName)
							{
								schema.Items.Reference.Id = newName;
							}

							foreach (var property in schema.Properties)
							{
								if (property.Value.Reference?.Id == oldName)
								{
									property.Value.Reference.Id = newName;
								}
							}
						}
						// any reference to the old name in the document needs to be re-mapped.
						//

						foreach (var path in instance.Document.Paths)
						{
							foreach (var op in path.Value.Operations)
							{
								foreach (var res in op.Value.Responses)
								{
									foreach (var content in res.Value.Content)
									{
										RewireSchema(content.Value.Schema);
									}
								}
							}
						}

						foreach (var res in instance.Document.Components.Responses.Values)
						{
							if (res.Reference?.Id == oldName)
							{
								res.Reference.Id = newName;
							}

							foreach (var content in res.Content)
							{
								RewireSchema(content.Value.Schema);
							}
						}

						foreach (var schema in instance.Document.Components.Schemas.Values)
						{
							RewireSchema(schema);
						}
					}
				}
			}

		}

		if (resolutionStrategy != GenerateSdkConflictResolutionStrategy.None)
		{
			HandleResolution();
		}
		list = list.DistinctBy(x => x.Name).ToList();

		return list;
	}

	public async Task<IEnumerable<OpenApiDocumentResult>> DownloadBeamableApis(BeamableApiFilter filter)
	{
		var selected = Apis.Where(filter.Accepts).ToList();
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
						throw new OpenApiException($"invalid document {url} - {warning.Message} - {warning.Pointer}");
					}

					foreach (var error in res.Diagnostic.Errors)
					{
						Log.Error("found ERROR for {url}. {message} . from {pointer}", url, error.Message,
							error.Pointer);
						throw new OpenApiException($"invalid document {url} - {error.Message} - {error.Pointer}");
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
	public string FileName => $"{BeamableApiSourceExtensions.ToDisplay(Source)}_{Service}.json";
}

public class BeamableApiFilter : DefaultQuery
{
	public bool HasApiTypeConstraint;
	public BeamableApiSource ApiTypeConstraint;

	public bool Accepts(BeamableApiDescriptor descriptor)
	{
		return AcceptIdContains(descriptor.Service) && AcceptsApiType(descriptor.Source);
	}

	public bool AcceptsApiType(BeamableApiSource apiType)
	{
		if (!HasApiTypeConstraint) return true;
		return apiType.ContainsAllFlags(ApiTypeConstraint);
	}

	private static void ApplyApiTypeRule(string raw, BeamableApiFilter query)
	{
		query.HasApiTypeConstraint = false;
		if (BeamableApiSourceExtensions.TryParse(raw, out var apiCons))
		{
			query.HasApiTypeConstraint = true;
			query.ApiTypeConstraint = apiCons;
		}
	}
	private static bool SerializeApiTypeRule(BeamableApiFilter query, out string str)
	{
		str = string.Empty;
		if (query.HasApiTypeConstraint)
		{
			str = $"t:{query.ApiTypeConstraint.Serialize()}";
			return true;
		}
		return false;
	}

	public static BeamableApiFilter Parse(string text)
	{
		return DefaultQueryParser.Parse(text, StandardRules);
	}

	protected static readonly Dictionary<string, DefaultQueryParser.ApplyParseRule<BeamableApiFilter>> StandardRules = new Dictionary<string, DefaultQueryParser.ApplyParseRule<BeamableApiFilter>>
	{
		{"id", DefaultQueryParser.ApplyIdParse},
		{"t", ApplyApiTypeRule},

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

[Flags]
public enum BeamableApiSource
{
	PLAT_THOR_OBJECT = 1,
	PLAT_THOR_BASIC = 2,
	PLAT_PROTO = 4
}

public static class BeamableApiSourceExtensions
{
	static Dictionary<BeamableApiSource, string> enumToString = new Dictionary<BeamableApiSource, string>
	{
		{BeamableApiSource.PLAT_PROTO, "proto"},
		{BeamableApiSource.PLAT_THOR_BASIC, "basic"},
		{BeamableApiSource.PLAT_THOR_OBJECT, "object"},
	};
	static Dictionary<string, BeamableApiSource> stringToEnum = new Dictionary<string, BeamableApiSource>();
	static BeamableApiSourceExtensions()
	{
		foreach (var kvp in enumToString)
		{
			stringToEnum.Add(kvp.Value, kvp.Key);
		}
	}
	public static bool TryParse(string str, out BeamableApiSource status)
	{
		var parts = str.Split(new[] { ' ' }, StringSplitOptions.None);
		status = BeamableApiSource.PLAT_THOR_BASIC;

		var any = false;
		foreach (var part in parts)
		{
			if (stringToEnum.TryGetValue(part, out var subStatus))
			{
				if (!any)
				{
					status = subStatus;
				}
				else
				{
					status |= subStatus;
				}
				any = true;
			}
		}
		return any;
	}
	public static string Serialize(this BeamableApiSource self)
	{
		var str = self.ToString();
		foreach (var kvp in stringToEnum)
		{
			str = str.Replace(kvp.Value.ToString(), kvp.Key);
		}
		str = str.Replace(",", "");
		return str;
	}

	public static string ToDisplay(BeamableApiSource source)
	{
		switch (source)
		{
			case BeamableApiSource.PLAT_PROTO: return "proto";
			case BeamableApiSource.PLAT_THOR_BASIC: return "basic";
			case BeamableApiSource.PLAT_THOR_OBJECT: return "object";
		}

		return "unknown";
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
	/// The openAPI schema itself, after it has gone through conflict resolution
	/// </summary>
	public OpenApiSchema Schema;

	/// <summary>
	/// the openAPI schema itself, before it went through conflict resolution. This schema may have conflicts with other RawSchema properties.
	/// </summary>
	public OpenApiSchema RawSchema;

	/// <summary>
	/// A combination of the <see cref="Document"/>'s title, and <see cref="Name"/>
	/// </summary>
	public string UniqueName => $"{Document.Info.Title}-{Name}";
}
