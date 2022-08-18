using Microsoft.OpenApi.Models;
using System.CodeDom;

namespace cli;

public class UnitySourceGenerator2 : SwaggerService.ISourceGenerator
{
	public List<GeneratedFileDescriptors> Generate(IGenerationContext context)
	{
		var res = new List<GeneratedFileDescriptors>();

		return res;
	}
}

public class GenCodeTypeReference : CodeTypeReference
{
	public string DisplayName { get; set; }

	public string UpperDisplayName => char.ToUpper(DisplayName[0]) + DisplayName.Substring(1);

	public GenCodeTypeReference()
	{

	}

	public GenCodeTypeReference(Type runtimeType) : base(runtimeType)
	{
		DisplayName = GetDisplayName(runtimeType);
	}

	public GenCodeTypeReference(string typeName) : base(typeName)
	{
		DisplayName = typeName;
	}

	public GenCodeTypeReference(GenCodeTypeReference type, int arrayRank) : base(type, arrayRank)
	{
		DisplayName = type.DisplayName + "Array";
	}


	private static Dictionary<Type, string> _typeToDisplayName = new Dictionary<Type, string>
	{
		[typeof(int)] = "int",
		[typeof(long)] = "long",
		[typeof(short)] = "short",
		[typeof(byte)] = "byte",
		[typeof(double)] = "double",
		[typeof(float)] = "float",
		[typeof(bool)] = "bool",
		[typeof(string)] = "string",
		[typeof(Guid)] = "Guid",
	};
	public static string GetDisplayName(Type runtimeType)
	{
		if (_typeToDisplayName.TryGetValue(runtimeType, out var name))
		{
			return name;
		}

		return runtimeType.Name;
	}
}

public class GenSchema
{
	public OpenApiSchema Schema;

	public GenSchema(OpenApiSchema schema)
	{
		Schema = schema;
	}

	public GenCodeTypeReference GetOptionalTypeReference()
	{
		var innerType = GetTypeReference();
		var clazzName = $"Optional{innerType.UpperDisplayName}";
		return new GenCodeTypeReference(clazzName);
	}

	public GenCodeTypeReference GetTypeReference()
	{
		switch (Schema.Type, Schema.Format)
		{
			case ("array", _) when Schema.Items.Reference == null:
				var genElem = new GenSchema(Schema.Items);
				var elemType = genElem.GetTypeReference();
				return new GenCodeTypeReference(elemType, 1);
			case ("array", _) when Schema.Items.Reference != null:
				var referenceType = new GenCodeTypeReference(Schema.Items.Reference.Id);
				return new GenCodeTypeReference(referenceType, 1);
				break;
			case ("object", _) when Schema.AdditionalPropertiesAllowed:
				var genValues = new GenSchema(Schema.AdditionalProperties);
				var genType = genValues.GetTypeReference();
				var mapTypeName = $"MapOf{genType.UpperDisplayName}";
				var type = new GenCodeTypeReference(mapTypeName);
				// type.TypeArguments.Add(typeof(string));
				// type.TypeArguments.Add(genType);
				return type;
			case ("object", _) when Schema.Reference == null && Schema.AdditionalPropertiesAllowed == false:
				throw new Exception("Cannot build a reference to a schema that is just an object...");
			case ("object", _) when Schema.Reference != null:
				return new GenCodeTypeReference(Schema.Reference.Id);
				break;
			case ("number", "float"):
				return new GenCodeTypeReference(typeof(float));
			case ("number", "double"):
			case ("number", _):
				return new GenCodeTypeReference(typeof(double));
			case ("boolean", _):
				return new GenCodeTypeReference(typeof(bool));
			case ("string", "uuid"):
				return new GenCodeTypeReference(typeof(Guid));
			case ("string", "byte"):
				return new GenCodeTypeReference(typeof(byte));
			case ("System.String", _):
			case ("string", _):
				return new GenCodeTypeReference(typeof(string));
			case ("integer", "int16"):
				return new GenCodeTypeReference(typeof(short));
			case ("integer", "int32"):
				return new GenCodeTypeReference(typeof(int));
			case ("integer", "int64"):
				return new GenCodeTypeReference(typeof(long));
			case ("integer", _):
				return new GenCodeTypeReference(typeof(int));
			default:
				return new GenCodeTypeReference();
		}

		return new GenCodeTypeReference();
	}
}
