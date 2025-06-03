namespace Beamable.Tooling.Common.OpenAPI.Utils;

public class OpenApiUtils
{
	public static readonly Dictionary<string, string> OpenApiCSharpFullNameMap = new()
	{
		{ "int16", typeof(short).FullName },
		{ "int32", typeof(int).FullName },
		{ "int64", typeof(long).FullName },
		{ "integer", typeof(int).FullName },
		{ "float", typeof(float).FullName },
		{ "double", typeof(double).FullName },
		{ "decimal", typeof(decimal).FullName },
		{ "number", typeof(double).FullName },
		{ "boolean", typeof(bool).FullName },
		{ "date", typeof(DateTime).FullName },
		{ "date-time", typeof(DateTime).FullName },
		{ "uuid", typeof(Guid).FullName },
		{ "byte", typeof(byte).FullName },
		{ "string", typeof(string).FullName }
	};

	public static readonly Dictionary<string, string> OpenApiCSharpNameMap = new()
	{
		{ "int16", "short" },
		{ "int32", "int" },
		{ "int64", "long" },
		{ "integer", "int" },
		{ "float", "float" },
		{ "double", "double" },
		{ "decimal", "decimal" },
		{ "number", "double" },
		{ "boolean", "bool" },
		{ "uuid", typeof(Guid).FullName },
		{ "byte", "byte" },
		{ "string", "string" }
	};
}
