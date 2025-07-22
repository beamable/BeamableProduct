using cli.Services.Web.CodeGen;
using static cli.Services.Web.Helpers.WebApi;
using static cli.Services.Web.Helpers.WebSchema;

namespace cli.Services.Web;

public class WebSourceGenerator : SwaggerService.ISourceGenerator
{
	public List<GeneratedFileDescriptor> Generate(IGenerationContext context)
	{
		var namedSchemas = context.OrderedSchemas;
		var resources = new List<GeneratedFileDescriptor>();
		var enums = new List<TsEnum>();

		// Generate a separate TypeScript enum file for each schema that defines enum values
		resources.AddRange(namedSchemas
			.Select(namedSchema => GenerateSchemaEnum(namedSchema, enums))
			.Where(resource => resource != null)
			.OrderBy(resource => resource.FileName)
			.Select(resource =>
			{
				BuildSchemaBarrel(resource, isEnum: true);
				return resource;
			}));

		// Generate a separate TypeScript class file for each non-enum schema definition
		resources.AddRange(namedSchemas
			.Select(namedSchema => GenerateSchema(namedSchema, enums))
			.Where(resource => resource != null)
			.OrderBy(resource => resource.FileName)
			.Select(resource =>
			{
				BuildSchemaBarrel(resource, isEnum: false);
				return resource;
			}));

		// Generate the schema barrel file (index.ts)
		resources.Add(new GeneratedFileDescriptor
		{
			FileName = $"schemas/{SCHEMA_BARREL_FILE.FileName}.ts", Content = SCHEMA_BARREL_FILE.Render()
		});

		// Generate a separate TypeScript file for each api module
		var apiModules = GenerateApiModules(context.Documents, enums);
		resources.AddRange(apiModules
			.OrderBy(resource => resource.FileName)
			.Select(resource =>
			{
				BuildApiBarrel(resource);
				return resource;
			}));

		// Generate the api barrel file (index.ts)
		resources.Add(new GeneratedFileDescriptor
		{
			FileName = $"apis/{API_BARREL_FILE.FileName}.ts", Content = API_BARREL_FILE.Render()
		});

		// Generate the api constants file (constants.ts)
		resources.Add(new GeneratedFileDescriptor
		{
			FileName = $"apis/{API_CONSTANT_FILE.FileName}.ts", Content = API_CONSTANT_FILE.Render()
		});

		return resources;
	}
}
