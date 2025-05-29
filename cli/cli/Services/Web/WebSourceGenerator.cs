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
			.Where(resource => resource != null));

		// Generate a separate TypeScript class file for each non-enum schema definition
		resources.AddRange(namedSchemas
			.Select(namedSchema => GenerateSchema(namedSchema, enums))
			.Where(resource => resource != null));

		// Generate a separate TypeScript file for each api module
		var apiModules = GenerateApiModules(context.Documents, enums);
		resources.AddRange(apiModules);

		return resources;
	}
}
