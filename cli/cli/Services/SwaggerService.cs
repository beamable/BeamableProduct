using Beamable.Common;
using Beamable.Common.Dependencies;
using cli.Unreal;
using cli.Utils;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Exceptions;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Writers;
using Newtonsoft.Json;
using Serilog;
using SharpYaml.Tokens;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace cli;



public class SwaggerService
{
	private readonly IAppContext _context;
	private readonly ISwaggerStreamDownloader _downloader;
	private readonly List<ISourceGenerator> _generators;

	// TODO: make a PLAT ticket to give us back all openAPI spec info
	public static readonly BeamableApiDescriptor[] Apis = new BeamableApiDescriptor[]
	{
		// the proto-actor stack!
		BeamableApis.ProtoActor(),

		// behold the list of Beamable scala Apis
		BeamableApis.BasicService("content"),
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
		BeamableApis.BasicService("session").WithRename("User", "SessionUser"),
		BeamableApis.BasicService("trials").WithRename("\"ref\"", "\"reference\""),
	};

	public SwaggerService(IAppContext context, ISwaggerStreamDownloader downloader, SourceGeneratorListProvider generators)
	{
		_context = context;
		_downloader = downloader;
		_generators = generators.Generators.ToList();
	}

	/// <summary>
	/// Create a list of <see cref="GeneratedFileDescriptor"/> that contain the generated source code
	/// </summary>
	/// <returns></returns>
	public async Task<List<GeneratedFileDescriptor>> Generate(BeamableApiFilter filter, string targetEngine, GenerateSdkConflictResolutionStrategy resolutionStrategy)
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
		foreach (var generator in _generators.Where(g => string.IsNullOrEmpty(targetEngine) || g.GetType().Name.Contains(targetEngine, StringComparison.OrdinalIgnoreCase)))
		{
			// Set the paths to mirror the folder structure of the BeamableCore plugin's "Source" folder
			// The reason we do this is so that we can simply copy/paste the result of the generation over the Source folder.
			// For a "clean install" all the user has to do is go to these paths and delete the AutoGen folder, code-gen again and then copy/paste the results
			// on the "Source" folder of the plugin (or, in SAMS case, the project) 
			UnrealSourceGenerator.headerFileOutputPath = "BeamableCore/Public/";
			UnrealSourceGenerator.cppFileOutputPath = "BeamableCore/Private/";
			UnrealSourceGenerator.blueprintHeaderFileOutputPath = "BeamableCoreBlueprintNodes/Public/BeamFlow/ApiRequest/";
			UnrealSourceGenerator.blueprintCppFileOutputPath = "BeamableCoreBlueprintNodes/Private/BeamFlow/ApiRequest/";
			UnrealSourceGenerator.previousGenerationPassesData = new PreviousGenerationPassesData();
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

		var schemaRecursiveRefs = new Dictionary<NamedOpenApiSchemaHandle, List<OpenApiSchema>>();

		//
		// string SerializeSchemaSuperMode(OpenApiSchema schema)
		// {
		// 	// var sb = new StringBuilder();
		// 	// sb.
		// }

		foreach (var doc in documents)
		{
			foreach ((string schemaName, OpenApiSchema openApiSchema) in doc.Components.Schemas)
			{
				if (string.IsNullOrEmpty(schemaName)) continue;

				var reader = new OpenApiStreamReader();
				var originalJson = SerializeSchema(openApiSchema);
				var stream = new MemoryStream(Encoding.UTF8.GetBytes(originalJson));
				var raw = reader.ReadFragment<OpenApiSchema>(stream, OpenApiSpecVersion.OpenApi3_0, out _);
				// var raw = JsonConvert.DeserializeObject<OpenApiSchema>(originalJson);

				// Make the handle for this specific document's schema
				var namedOpenApiSchemaHandle = new NamedOpenApiSchemaHandle() { OwnerDoc = doc, SchemaName = schemaName, };

				// Build the list of schemas referenced by this specific document's schema
				var schemaRefCount = new List<OpenApiSchema>(64);
				GatherSchemaRefs(doc.Components.Schemas, schemaName, schemaName, schemaRefCount);

				// We fail loudly if we ever get two schemas with the same name in the same document.
				// This means we can't properly disambiguate between them.   
				if (!schemaRecursiveRefs.TryAdd(namedOpenApiSchemaHandle, schemaRefCount))
					throw new Exception($"Schema Name {schemaName} Clashing with another document's Schema!");

				// If there are blank schemas for whatever reason, we can simply skip them.
				if (string.IsNullOrEmpty(schemaName)) continue;

				// Log out the ref count found.
				Log.Logger.Verbose($"{namedOpenApiSchemaHandle.OwnerDoc.Info.Title}-{namedOpenApiSchemaHandle.SchemaName} Found Ref Count = {schemaRecursiveRefs[namedOpenApiSchemaHandle].Count}");

				list.Add(new NamedOpenApiSchema
				{
					RawSchema = raw.GetEffective(doc),
					Name = schemaName,
					Schema = openApiSchema,
					Document = doc,
					DependsOnSchema = schemaRecursiveRefs[namedOpenApiSchemaHandle]
				});
			}
		}

		// Find the number of recursive references to other schemas that each individual schema has (SchemaName => Recursive Refs)
		void GatherSchemaRefs(IDictionary<string, OpenApiSchema> allSchemas, string schemaName, string originalName, List<OpenApiSchema> outSchemaRefs, int lvl = 0)
		{
			var data = allSchemas[schemaName];
			var properties = data.Properties;

			foreach ((string _, OpenApiSchema propData) in properties)
			{
				var isPropArray = propData.Type == "array";

				if (isPropArray && propData.Items.Reference != null)
				{
					var schemaKey = propData.Items.Reference.Id;
					if (!outSchemaRefs.Contains(allSchemas[schemaKey]))
					{
						outSchemaRefs.Add(allSchemas[schemaKey]);
						GatherSchemaRefs(allSchemas, schemaKey, originalName, outSchemaRefs, lvl + 1);
					}
				}
				else if (propData.Reference != null)
				{
					var schemaKey = propData.Reference.Id;
					if (!outSchemaRefs.Contains(allSchemas[schemaKey]))
					{
						outSchemaRefs.Add(allSchemas[schemaKey]);
						GatherSchemaRefs(allSchemas, schemaKey, originalName, outSchemaRefs, lvl + 1);
					}
				}
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

				Dictionary<string, List<NamedOpenApiSchema>> similarSchemas = new Dictionary<string, List<NamedOpenApiSchema>>();
				var firstElement = elements.First();
				similarSchemas.Add(firstElement.UniqueName, new List<NamedOpenApiSchema> { firstElement });
				for (var i = 1; i < elements.Count; i++)
				{
					// check if there is an element match already
					var thisElement = elements[i];

					var found = false;
					foreach (var (_, existingGroup) in similarSchemas)
					{
						var groupSample = existingGroup.First();
						var areEqual = NamedOpenApiSchema.AreEqual(groupSample.Schema, thisElement.Schema, out _);
						if (areEqual)
						{
							existingGroup.Add(thisElement);
							found = true;
							break;
						}
					}

					if (!found)
					{
						similarSchemas.Add(thisElement.UniqueName, new List<NamedOpenApiSchema> { thisElement });
					}
				}

				if (similarSchemas.Count <= 1)
					continue; // there is only 1 variant of the serialization, so its "fine", and we don't need to do anything

				/*
		
					 * But if there is one variant that has distinctly _more_ references, we should assume it is a common one.
					 */

				// find the variant with the most entries...
				var variants = similarSchemas.Select(x => x.Value).ToList();
				List<NamedOpenApiSchema> highest = null;
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
			list = list.DistinctBy(x => x.Name).ToList();
		}

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
			var pinnedApi = api;
			tasks.Add(Task.Run(async () =>
			{
				var url = $"{_context.Host}/{api.RelativeUrl}";
				try
				{
					var stream = await downloader.GetStreamAsync(url);
					var sr = new StreamReader(stream);
					var content = await sr.ReadToEndAsync();

					foreach (var (oldName, newName) in pinnedApi.schemaRenames)
					{
						content = content.Replace(oldName, newName);
					}

					var res = new OpenApiDocumentResult();
					res.Document = new OpenApiStringReader().Read(content, out res.Diagnostic);
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


		var processors = new List<Func<OpenApiDocumentResult, List<OpenApiDocumentResult>>>
		{
			RewriteStatusCodesTo200,
			ReduceProtoActorMimeTypes,
			RewriteInlineResultSchemasAsReferences,
			SplitTagsIntoSeparateDocuments,
			AddTitlesToAllSchemasIfNone,
			RewriteObjectEnumsAsStrings,
			Reserailize
		};


		var final = output.ToList();
		foreach (var processor in processors)
		{
			var startingSet = final.ToList();
			final.Clear();
			foreach (var elem in startingSet)
			{
				foreach (var processed in processor(elem))
				{
					final.Add(processed);
				}
			}
		}

		return final;
	}

	public static string FormatPathNameAsMethodName(string input)
	{
		// Define the regular expression pattern
		string pattern = @"\{([^}]*)\}";

		// Replace all matches of the pattern with the captured text in uppercase, surrounded by "By"
		string output = Regex.Replace(input, pattern, "");

		// // Remove all non-alphanumeric characters and join the remaining parts of the string
		output = string.Join("", output.Split(new char[] { '/', '-' }).Select(part =>
		{
			if (part.Length > 0) // Check if the length of the part is greater than zero
			{
				return char.ToUpper(part[0]) + part.Substring(1);
			}
			else
			{
				return "";
			}
		}));

		return output;
	}

	private static string SerializeSchema(OpenApiSchema schema)
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

	private static List<OpenApiDocumentResult> RewriteObjectEnumsAsStrings(OpenApiDocumentResult swagger)
	{
		foreach (var schema in swagger.Document.Components.Schemas)
		{
			foreach (var kvp in schema.Value.Properties)
			{
				var propertySchema = kvp.Value;
				var isObject = propertySchema.Type == "object";
				var isFormatUnknown = propertySchema.Format == "unknown";
				var isEnum = propertySchema.Enum != null && propertySchema.Enum.Count > 0;

				if (isObject && isFormatUnknown && isEnum)
				{
					propertySchema.Type = "string";
					propertySchema.Format = null;
				}
			}
		}
		return new List<OpenApiDocumentResult> { swagger };
	}

	private static List<OpenApiDocumentResult> RewriteStatusCodesTo200(OpenApiDocumentResult swagger)
	{
		const string STATUS_200 = "200";
		const string STATUS_201 = "201";
		const string STATUS_204 = "204";
		const string APPLICATION_JSON = "application/json";

		var output = new List<OpenApiDocumentResult>();
		output.Add(swagger);

		// the 204 http status code means GOOD, but, void return. To keep generation simple, we'll map these to 200 status codes with empty returns.
		foreach (var path in swagger.Document.Paths)
		{
			foreach (var op in path.Value.Operations)
			{
				if (!op.Value.Responses.ContainsKey(STATUS_200) && op.Value.Responses.ContainsKey(STATUS_204))
				{
					op.Value.Responses.Remove(STATUS_204);
					op.Value.Responses[STATUS_200] = new OpenApiResponse
					{
						Content = new Dictionary<string, OpenApiMediaType>
						{
							[APPLICATION_JSON] = new OpenApiMediaType
							{
								Schema = new OpenApiSchema()
							}
						}
					};
				}

				if (!op.Value.Responses.ContainsKey(STATUS_200) && op.Value.Responses.ContainsKey(STATUS_201))
				{
					var content201 = op.Value.Responses[STATUS_201].Content;
					op.Value.Responses.Remove(STATUS_201);
					op.Value.Responses[STATUS_200] = new OpenApiResponse
					{
						Content = content201
					};
				}

				if (op.Value.Responses.ContainsKey(STATUS_200) && op.Value.Responses[STATUS_200].Content.Count == 0)
				{
					op.Value.Responses[STATUS_200] = new OpenApiResponse
					{
						Content = new Dictionary<string, OpenApiMediaType>
						{
							[APPLICATION_JSON] = new OpenApiMediaType
							{
								Schema = new OpenApiSchema()
							}
						}
					};
				}
			}
		}

		return output;
	}

	private static List<OpenApiDocumentResult> AddTitlesToAllSchemasIfNone(OpenApiDocumentResult swagger)
	{
		foreach (var schema in swagger.Document.Components.Schemas)
		{
			schema.Value.Title ??= schema.Key;
		}

		return new List<OpenApiDocumentResult> { swagger };
	}


	private static List<OpenApiDocumentResult> Reserailize(OpenApiDocumentResult swagger)
	{
		var outputString = swagger.Document.Serialize(OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Json);
		var clonedDocument = new OpenApiStringReader().Read(outputString, out var diag);
		return new List<OpenApiDocumentResult>
		{
			new OpenApiDocumentResult
			{
				Descriptor = swagger.Descriptor, Diagnostic = diag, Document = clonedDocument
			}
		};
	}

	private static List<OpenApiDocumentResult> ReduceProtoActorMimeTypes(OpenApiDocumentResult swagger)
	{
		const string APPLICATION_JSON = "application/json";
		const string TEXT_JSON = "text/json";
		const string APPLICATION_STAR_JSON = "application/*+json";
		// the proto-actor stack request objects have application/json, text/json, and application/*+json, which is too confusing.
		// this step will check for the existence of these 3 mime types together, and remove text/json and application/*+json
		var output = new List<OpenApiDocumentResult> { swagger };

		foreach (var path in swagger.Document.Paths)
		{
			foreach (var op in path.Value.Operations)
			{
				var content = op.Value?.RequestBody?.Content;
				if (content == null)
				{
					continue;
				}

				var hasAppJson = content.ContainsKey(APPLICATION_JSON);
				var hasTextJson = content.ContainsKey(TEXT_JSON);
				var hasAppStarJson = content.ContainsKey(APPLICATION_STAR_JSON);

				if (!hasAppJson || !hasTextJson || !hasAppStarJson)
				{
					continue;
				}

				// the reference must all be identical here
				var appJsonSchema = SerializeSchema(content[APPLICATION_JSON].Schema);
				var textJsonSchema = SerializeSchema(content[TEXT_JSON].Schema);
				var appStarJsonSchema = SerializeSchema(content[APPLICATION_STAR_JSON].Schema);

				if (appJsonSchema == textJsonSchema && textJsonSchema == appStarJsonSchema)
				{
					// hey, we can remove text and app_star, because they are essentially identical.
					op.Value.RequestBody.Content.Remove(TEXT_JSON);
					op.Value.RequestBody.Content.Remove(APPLICATION_STAR_JSON);
				}
			}
		}

		return output;
	}

	private static List<OpenApiDocumentResult> RewriteInlineResultSchemasAsReferences(OpenApiDocumentResult swagger)
	{
		var output = new List<OpenApiDocumentResult>();

		var addedComponents = new Dictionary<string, KeyValuePair<string, OpenApiSchema>>(); // name -> json --> schema
		foreach (var path in swagger.Document.Paths)
		{
			foreach (var op in path.Value.Operations)
			{
				foreach (var res in op.Value.Responses)
				{
					foreach (var content in res.Value.Content)
					{
						var schema = content.Value.Schema;
						var isInlineSchema = schema.Reference?.Id == null;

						if (!isInlineSchema)
						{
							continue;
						}
						var pathName = FormatPathNameAsMethodName(path.Key);
						var id = $"{pathName}{op.Key}{op.Value.Tags[0].Name}Response";
						var referencedSchema = new OpenApiSchema
						{
							Type = "object",
							Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = id }
						};
						content.Value.Schema = referencedSchema;

						var json = SerializeSchema(schema);

						schema.Title = id;
						if (!addedComponents.TryGetValue(id, out var existingForms))
						{
							existingForms = addedComponents[id] =
								new KeyValuePair<string, OpenApiSchema>(json, schema);
						}

						if (existingForms.Key == json)
						{
							// this is good, we can just reference this other schema!
						}
						else
						{
							// no good, we have a new form of serialization
							throw new CliException("Operations that return inline schemas may not have different schemas that collide under the operation method and tag");
						}

					}
				}
			}
		}

		foreach (var schema in addedComponents.Values)
		{
			swagger.Document.Components.Schemas.Add(schema.Value.Title, schema.Value);
		}

		output.Add(swagger);

		return output;
	}

	private static List<OpenApiDocumentResult> SplitTagsIntoSeparateDocuments(OpenApiDocumentResult swagger)
	{
		var tagsToSkip = Array.Empty<string>();
		var output = new List<OpenApiDocumentResult>();

		var opCount = 0;
		var opsWithTagsCount = 0;

		var tagToPaths = new Dictionary<string, List<KeyValuePair<string, OpenApiPathItem>>>();
		try
		{
			foreach (var path in swagger.Document.Paths)
			{
				string tag = null;
				foreach (var op in path.Value.Operations)
				{
					opCount++;
					var tags = op.Value?.Tags;
					var hasInvalidTags = tags?.Count > 1;
					if (hasInvalidTags)
					{
						throw new CliException("Swagger docs are not allowed to have more than 1 tag in each operation.");
					}

					var hasTags = tags?.Count == 1;
					if (!hasTags) continue;

					opsWithTagsCount++;

					if (tag == null)
					{
						tag = tags[0].Name;
					}
					else if (tag != tags[0].Name)
					{
						throw new CliException("If an operation has a tag, then all operations in the same path must have the same tag. ");
					}
				}

				if (tag == null)
				{
					continue;
				}
				if (!tagToPaths.TryGetValue(tag, out var tagCollection))
				{
					tagToPaths[tag] = tagCollection = new List<KeyValuePair<string, OpenApiPathItem>>();
				}

				tagCollection.Add(path);
			}


			if (tagToPaths.Count == 0)
			{
				output.Add(swagger);
				return output;
			}

			if (opsWithTagsCount != opCount)
			{
				throw new CliException("If a swagger doc has a tag in an operation, then every operation must have a tag.");
			}

		}
		catch (Exception ex)
		{
			Debug.LogError(ex);
			throw;
		}

		foreach (var tagToPathSet in tagToPaths)
		{
			var outputString = swagger.Document.Serialize(OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Json);
			var clonedDocument = new OpenApiStringReader().Read(outputString, out var diag);
			var referencedSchemas = new Dictionary<string, OpenApiSchema>();
			var schemasToExplore = new Queue<OpenApiSchema>();

			if (tagsToSkip.Contains(tagToPathSet.Key))
				continue;
			clonedDocument.Info.Title = $"{tagToPathSet.Key} Actor";
			clonedDocument.Components = new OpenApiComponents();
			clonedDocument.Components.Schemas = referencedSchemas;
			clonedDocument.Paths = new OpenApiPaths();
			foreach (var kvp in tagToPathSet.Value)
			{
				clonedDocument.Paths.Add(kvp.Key, kvp.Value);

				foreach (var op in kvp.Value.Operations)
				{
					if (op.Value?.RequestBody != null)
					{
						foreach (var content in op.Value.RequestBody.Content)
						{
							schemasToExplore.Enqueue(content.Value.Schema);
						}
					}


					if (op.Value?.Responses != null)
					{
						foreach (var response in op.Value.Responses)
						{
							if (response.Value?.Content != null)
							{
								foreach (var content in response.Value.Content)
								{
									schemasToExplore.Enqueue(content.Value.Schema);
								}
							}
						}
					}
				}
			}

			var seenSchemas = new HashSet<OpenApiSchema>();
			while (schemasToExplore.Count > 0)
			{
				var curr = schemasToExplore.Dequeue();
				if (curr == null) continue;
				if (seenSchemas.Contains(curr)) continue;
				seenSchemas.Add(curr);



				schemasToExplore.Enqueue(curr.AdditionalProperties);
				schemasToExplore.Enqueue(curr.Items);
				if (curr.Properties != null)
				{
					foreach (var prop in curr.Properties)
					{
						schemasToExplore.Enqueue(prop.Value);
					}
				}

				if (curr.OneOf != null)
				{
					foreach (var option in curr.OneOf)
					{
						schemasToExplore.Enqueue(option);
					}
				}

				var isInlineSchema = string.IsNullOrEmpty(curr.Reference?.Id);
				if (isInlineSchema) continue;

				var referencedSchema = swagger.Document.Components.Schemas[curr.Reference.Id];
				referencedSchemas[curr.Reference.Id] = referencedSchema;
				schemasToExplore.Enqueue(referencedSchema);
			}

			output.Add(new OpenApiDocumentResult
			{
				Descriptor = swagger.Descriptor,
				Diagnostic = diag,
				Document = clonedDocument
			});

		}
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

	public class SourceGeneratorListProvider
	{
		public readonly ISourceGenerator[] Generators;

		public SourceGeneratorListProvider(IDependencyProviderScope scope)
		{
			var descriptors = scope.SingletonServices.Where(service => service.Interface.IsAssignableTo(typeof(ISourceGenerator))).ToList();
			Generators = descriptors.Select(descriptor => scope.GetService(descriptor.Interface)).Cast<ISourceGenerator>().ToArray();
		}

	}

}

public class BeamableApiDescriptor
{
	public BeamableApiSource Source;
	public string RelativeUrl;
	public string Service;

	public Dictionary<string, string> schemaRenames = new Dictionary<string, string>();

	public BeamableApiDescriptor WithRename(string old, string next)
	{
		schemaRenames[old] = next;
		return this;
	}

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
	public static BeamableApiDescriptor ProtoActor()
	{
		return new BeamableApiDescriptor
		{
			Source = BeamableApiSource.PLAT_PROTO,
			RelativeUrl = $"api/platform/docs",
			Service = "api"
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
		{BeamableApiSource.PLAT_PROTO, "api"},
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
			case BeamableApiSource.PLAT_PROTO: return "api";
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

public class OpenApiSchemaComparer : IEqualityComparer<OpenApiSchema>
{
	public bool Equals(OpenApiSchema x, OpenApiSchema y)
	{
		return NamedOpenApiSchema.AreEqual(x, y, out _);
	}

	public int GetHashCode(OpenApiSchema obj)
	{
		return obj.GetHashCode();
	}
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
	/// List of openAPI Schemas that this depends on. 
	/// </summary>
	public List<OpenApiSchema> DependsOnSchema;

	/// <summary>
	/// A combination of the <see cref="Document"/>'s title, and <see cref="Name"/>
	/// </summary>
	public string UniqueName => $"{Document.Info.Title}-{Name}";


	private static Dictionary<(OpenApiSchema, OpenApiSchema), List<string>> _equalityCache =
		new Dictionary<(OpenApiSchema, OpenApiSchema), List<string>>();

	public static bool AreEqual(OpenApiSchema a, OpenApiSchema b) => AreEqual(a, b, out _);
	public static bool AreEqual(OpenApiSchema a, OpenApiSchema b, out List<string> differences)
	{

		differences = new List<string>();

		if (_equalityCache.TryGetValue((a, b), out var equalityReasons))
		{
			differences = equalityReasons;
			return differences.Count == 0;
		}
		else
		{
			_equalityCache.Add((a, b), differences);
		}

		if (a == null && b == null) return true;
		if (a == null || b == null) return false;
		if (a == b) return true;

		// title must match.
		// if (!string.Equals(a.Title, b.Title))
		// {
		// 	differences.Add("titles are different");
		// }

		// type must match.
		if (!string.Equals(a.Type, b.Type))
		{
			differences.Add("types are different");
		}

		// format must match.
		if (!string.Equals(a.Format, b.Format))
		{
			differences.Add("formats are different");
		}

		// required fields must set equal
		if (!a.Required.SetEquals(b.Required))
		{
			differences.Add("required fields are different");
		}

		// additional properties must be same
		if (a.AdditionalPropertiesAllowed != b.AdditionalPropertiesAllowed)
		{
			differences.Add("additional properties don't match");
		}

		// additional property deep equal must match
		if (!AreEqual(a.AdditionalProperties, b.AdditionalProperties, out var moreReasons))
		{
			differences.AddRange(moreReasons.Select(x => $"additionalProperties - {x}"));
		}

		// items property deep equal must match
		if (!AreEqual(a.Items, b.Items, out moreReasons))
		{
			differences.AddRange(moreReasons.Select(x => $"items - {x}"));
		}

		// if this is a reference, it must reference the same thing.
		if (a.Reference != null && b.Reference != null)
		{
			// the reference id must be the same
			if (!string.Equals(a.Reference.Id, b.Reference.Id))
			{
				differences.Add("reference ids are different");
			}

			// the host document must be the same.
			// var aSchema = a.GetEffective(a.Reference.HostDocument);
			// var bSchema = b.GetEffective(b.Reference.HostDocument);



			if (!string.Equals(a.Reference.HostDocument?.Info?.Title, b.Reference.HostDocument?.Info?.Title))
			{
				var aSchema = a.Reference.HostDocument.Components.Schemas[a.Reference.Id];
				var bSchema = a.Reference.HostDocument.Components.Schemas[a.Reference.Id];
				if (!AreEqual(aSchema, bSchema, out moreReasons))
				{
					differences.AddRange(moreReasons.Select(x => $"reference - {x}"));
				}
				// differences.Add("reference host document titles are different");
			}
		}
		else if (a.Reference == null && b.Reference != null)
		{
			differences.Add("b has reference, but a doesn't");
		}
		else if (a.Reference != null && b.Reference == null)
		{
			differences.Add("a has reference, but b doesn't");
		}

		// properties must match
		if (a.Properties != null && b.Properties != null)
		{
			// the keys must be an exact match.
			var aKeys = new SortedSet<string>(a.Properties.Keys);
			var bKeys = new SortedSet<string>(b.Properties.Keys);

			if (!aKeys.SetEquals(bKeys))
			{
				differences.Add("property keys do not match");
			}
			else
			{
				// check that all property schemas match
				foreach (var key in aKeys)
				{
					var aSchema = a.Properties[key];
					var bSchema = b.Properties[key];
					if (!AreEqual(aSchema, bSchema, out moreReasons))
					{
						differences.AddRange(moreReasons.Select(x => $"property {key} - {x}"));
					}
				}
			}
		}
		else if (a.Properties == null && b.Properties != null)
		{
			differences.Add("b has properties, but a doesn't");
		}
		else if (a.Properties != null && b.Properties == null)
		{
			differences.Add("a has properties, but b doesn't");
		}


		return differences.Count == 0;
	}
}

public struct NamedOpenApiSchemaHandle
{
	public OpenApiDocument OwnerDoc;
	public string SchemaName;
}
