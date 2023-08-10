using Beamable.Serialization;
using cli;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using NUnit.Framework;
using System;
using System.CodeDom;
using System.Collections.Generic;

namespace tests;
public static class TestExtensions
{
	public static string Sanitize(this string str) => str.Trim().ReplaceLineEndings().Replace("\t", "    ");

	public static void AssertSrc(this string src, string expected) =>
		Assert.AreEqual(expected.Sanitize(), src.Sanitize());
}

public class ModelGenerationTests
{

	[Test]
	public void OptionalObjectType()
	{
		var type = UnityHelper.GenerateOptionalDecl("Tuna", new OpenApiSchema
		{
			Type = "object",
		});

		Assert.IsNotNull(type);
		var unit = new CodeCompileUnit();
		unit.Namespaces.Add(new CodeNamespace("Test") { Types = { type } });
		var src = UnityHelper.GenerateCsharp(unit);

		src.AssertSrc(@"
namespace Test
{
    
	[System.SerializableAttribute()]
	public class OptionalTuna : Beamable.Common.Content.Optional<Tuna>
	{
		public OptionalTuna()
		{
		}
		public OptionalTuna(Tuna value)
		{
			HasValue = true;
			Value = value;
		}
	}
}
");
	}

	// 	[Test]
	// 	public void OptionalObjectArrayType()
	// 	{
	// 		var type = UnityHelper.GenerateOptionalDecl("Tuna", new OpenApiSchema
	// 		{
	// 			Type = "object",
	// 		});
	//
	// 		Assert.IsNotNull(type);
	// 		var unit = new CodeCompileUnit();
	// 		unit.Namespaces.Add(new CodeNamespace("Test") { Types = { type } });
	// 		var src = UnityHelper.GenerateCsharp(unit);
	//
	// 		src.AssertSrc(@"
	// namespace Test
	// {
	//
	// 	[System.SerializableAttribute()]
	// 	public class OptionalTuna : Beamable.Common.Content.Optional<Tuna>
	// 	{
	// 		public OptionalTuna()
	// 		{
	// 		}
	// 		public OptionalTuna(Tuna value)
	// 		{
	// 			HasValue = true;
	// 			Value = value;
	// 		}
	// 	}
	// }
	// ");
	// 	}

	[TestCase("integer", "")]
	[TestCase("integer", "int16")]
	[TestCase("integer", "int32")]
	[TestCase("integer", "int64")]
	[TestCase("number", "")]
	[TestCase("number", "float")]
	[TestCase("number", "double")]
	[TestCase("string", "")]
	[Test]
	public void OptionalPrimitiveNullsOut(string typeName, string format)
	{
		var type = UnityHelper.GenerateOptionalDecl("Tuna", new OpenApiSchema
		{
			Type = typeName,
			Format = format,
		});

		Assert.IsNull(type);
	}

	[Test]
	public void LongField()
	{
		var type = UnityHelper.GenerateModelDecl("Tuna", new OpenApiSchema
		{
			Type = "object",
			Properties = new Dictionary<string, OpenApiSchema>
			{
				["foo"] = new OpenApiSchema
				{
					Type = "integer",
					Format = "int64"
				}
			},
			Required = new HashSet<string> { "foo" }
		});

		Assert.IsNotNull(type);
		var unit = new CodeCompileUnit();
		unit.Namespaces.Add(new CodeNamespace("Test") { Types = { type } });
		var src = UnityHelper.GenerateCsharp(unit);

		src.AssertSrc(@"
namespace Test
{
    
	[System.SerializableAttribute()]
	public partial class Tuna : Beamable.Serialization.JsonSerializable.ISerializable
	{
		public long foo;
        public virtual void Serialize(Beamable.Serialization.JsonSerializable.IStreamSerializer s)
        {
			s.Serialize(""foo"", ref foo);
		}
	}
}
");
	}


	[Test]
	public void EnumFieldWithoutReference()
	{

		var type = UnityHelper.GenerateModelDecl("Tuna", new OpenApiSchema
		{
			Type = "object",
			Properties = new Dictionary<string, OpenApiSchema>
			{
				["foo"] = new OpenApiSchema
				{
					Type = "string",
					Enum = new List<IOpenApiAny> { new OpenApiString("text") },
					Default = new OpenApiString("text")
				}
			},
			Required = new HashSet<string> { "foo" }
		});

		Assert.IsNotNull(type);
		var unit = new CodeCompileUnit();
		unit.Namespaces.Add(new CodeNamespace("Test") { Types = { type } });
		var src = UnityHelper.GenerateCsharp(unit);

		Console.WriteLine(src);
		src.AssertSrc(@"
namespace Test
{
    
	[System.SerializableAttribute()]
	public partial class Tuna : Beamable.Serialization.JsonSerializable.ISerializable
	{
		public Tuna_foo foo;
        public virtual void Serialize(Beamable.Serialization.JsonSerializable.IStreamSerializer s)
        {
			s.SerializeEnum(""foo"", ref foo, Tuna_fooExtensions.ToEnumString, Tuna_fooExtensions.FromEnumString);
		}
		public enum Tuna_foo
		{
			Text,
		}
		public class Tuna_fooExtensions
        {
            public static string ToEnumString(Tuna_foo val)
            {
                if ((Tuna_foo.Text == val))
                {
                    return ""text"";
				}
				throw new System.ArgumentException(""Unknown enum value"");
			}
			public static Tuna_foo FromEnumString(string str)
			{
				if ((""text"" == str))
				{
					return Tuna_foo.Text;
				}
				throw new System.ArgumentException(""Unknown string value"");
			}
		}
	}
}
");
	}


	public class Example
	{
		public BaseTest[]? objs;
		public void Test(Beamable.Serialization.JsonSerializable.IStreamSerializer s)
		{
			s.SerializeArray("x", ref objs);
		}
	}

	public class BaseTest : Beamable.Serialization.JsonSerializable.ISerializable
	{
		public void Serialize(JsonSerializable.IStreamSerializer s)
		{
			throw new NotImplementedException();
		}
	}

	public class SubA : BaseTest
	{

	}


	public class SubB : BaseTest
	{

	}

	[Test]
	public void EnumField()
	{

		var type = UnityHelper.GenerateModelDecl("Tuna", new OpenApiSchema
		{
			Type = "object",
			Properties = new Dictionary<string, OpenApiSchema>
			{
				["foo"] = new OpenApiSchema
				{
					Type = "string",
					Enum = new List<IOpenApiAny> { new OpenApiString("incoming"), new OpenApiString("outgoing") },
					Reference = new OpenApiReference
					{
						Type = ReferenceType.Schema,
						Id = "InvitationDirection"
					}
				}
			},
			Required = new HashSet<string> { "foo" }
		});

		Assert.IsNotNull(type);
		var unit = new CodeCompileUnit();
		unit.Namespaces.Add(new CodeNamespace("Test") { Types = { type } });
		var src = UnityHelper.GenerateCsharp(unit);

		src.AssertSrc(@"
namespace Test
{
    
	[System.SerializableAttribute()]
	public partial class Tuna : Beamable.Serialization.JsonSerializable.ISerializable
	{
		public InvitationDirection foo = new InvitationDirection();
        public virtual void Serialize(Beamable.Serialization.JsonSerializable.IStreamSerializer s)
        {
			s.SerializeEnum(""foo"", ref foo, InvitationDirectionExtensions.ToEnumString, InvitationDirectionExtensions.FromEnumString);
		}
	}
}
");
	}


	[Test]
	public void StringField()
	{
		var type = UnityHelper.GenerateModelDecl("Tuna", new OpenApiSchema
		{
			Type = "object",
			Properties = new Dictionary<string, OpenApiSchema>
			{
				["foo"] = new OpenApiSchema
				{
					Type = "string"
				}
			},
			Required = new HashSet<string> { "foo" }
		});

		Assert.IsNotNull(type);
		var unit = new CodeCompileUnit();
		unit.Namespaces.Add(new CodeNamespace("Test") { Types = { type } });
		var src = UnityHelper.GenerateCsharp(unit);

		src.AssertSrc(@"
namespace Test
{
    
	[System.SerializableAttribute()]
	public partial class Tuna : Beamable.Serialization.JsonSerializable.ISerializable
	{
		public string foo;
        public virtual void Serialize(Beamable.Serialization.JsonSerializable.IStreamSerializer s)
        {
			s.Serialize(""foo"", ref foo);
		}
	}
}
");
	}

	[TestCase("params", "paramsKey")]
	[TestCase("if", "ifKey")]
	[TestCase("while", "whileKey")]
	[TestCase("do", "doKey")]
	[TestCase("as", "asKey")]
	[TestCase("long", "longKey")]
	[TestCase("string", "stringKey")]
	[TestCase("int", "intKey")]
	[TestCase("var", "varKey")]
	[Test]
	public void ReservedKeyWords(string keyWord, string expected)
	{
		var type = UnityHelper.GenerateModelDecl("Tuna", new OpenApiSchema
		{
			Type = "object",
			Properties = new Dictionary<string, OpenApiSchema>
			{
				[keyWord] = new OpenApiSchema
				{
					Type = "string"
				}
			},
			Required = new HashSet<string> { keyWord }
		});

		Assert.IsNotNull(type);
		var unit = new CodeCompileUnit();
		unit.Namespaces.Add(new CodeNamespace("Test") { Types = { type } });
		var src = UnityHelper.GenerateCsharp(unit);

		src.AssertSrc(@"
namespace Test
{
    
	[System.SerializableAttribute()]
	public partial class Tuna : Beamable.Serialization.JsonSerializable.ISerializable
	{
		public string FIELD;
        public virtual void Serialize(Beamable.Serialization.JsonSerializable.IStreamSerializer s)
        {
			s.Serialize(""API"", ref FIELD);
		}
	}
}
".Replace("FIELD", expected).Replace("API", keyWord));
	}

	[Test]
	public void LongArrayField()
	{
		var type = UnityHelper.GenerateModelDecl("Tuna", new OpenApiSchema
		{
			Type = "object",
			Properties = new Dictionary<string, OpenApiSchema>
			{
				["foo"] = new()
				{
					Type = "array",
					Items = new OpenApiSchema()
					{
						Type = "integer",
						Format = "int64"
					}
				}
			},
			Required = new HashSet<string> { "foo" }
		});

		Assert.IsNotNull(type);
		var unit = new CodeCompileUnit();
		unit.Namespaces.Add(new CodeNamespace("Test") { Types = { type } });
		var src = UnityHelper.GenerateCsharp(unit);

		src.AssertSrc(@"
namespace Test
{
    
	[System.SerializableAttribute()]
	public partial class Tuna : Beamable.Serialization.JsonSerializable.ISerializable
	{
		public long[] foo;
        public virtual void Serialize(Beamable.Serialization.JsonSerializable.IStreamSerializer s)
        {
			s.SerializeArray(""foo"", ref foo);
		}
	}
}
");
	}


	[Test]
	public void LongMapField()
	{
		var type = UnityHelper.GenerateModelDecl("Tuna", new OpenApiSchema
		{
			Type = "object",
			Properties = new Dictionary<string, OpenApiSchema>
			{
				["foo"] = new()
				{
					Type = "object",
					AdditionalProperties = new OpenApiSchema
					{
						Type = "integer",
						Format = "int64"
					},
					AdditionalPropertiesAllowed = true
				}
			},
			Required = new HashSet<string> { "foo" }
		});

		Assert.IsNotNull(type);
		var unit = new CodeCompileUnit();
		unit.Namespaces.Add(new CodeNamespace("Test") { Types = { type } });
		var src = UnityHelper.GenerateCsharp(unit);

		src.AssertSrc(@"
namespace Test
{
    
	[System.SerializableAttribute()]
	public partial class Tuna : Beamable.Serialization.JsonSerializable.ISerializable
	{
		public MapOfLong foo = new MapOfLong();
        public virtual void Serialize(Beamable.Serialization.JsonSerializable.IStreamSerializer s)
        {
			s.SerializeDictionary<MapOfLong, long>(""foo"", ref foo);
		}
	}
}
");
	}

	[Test]
	public void OptionalLongField()
	{
		var type = UnityHelper.GenerateModelDecl("Tuna", new OpenApiSchema
		{
			Type = "object",
			Properties = new Dictionary<string, OpenApiSchema>
			{
				["foo"] = new OpenApiSchema
				{
					Type = "integer",
					Format = "int64"
				}
			},
			Required = new HashSet<string>()
		});

		Assert.IsNotNull(type);
		var unit = new CodeCompileUnit();
		unit.Namespaces.Add(new CodeNamespace("Test") { Types = { type } });
		var src = UnityHelper.GenerateCsharp(unit);

		src.AssertSrc(@"
namespace Test
{
    
	[System.SerializableAttribute()]
	public partial class Tuna : Beamable.Serialization.JsonSerializable.ISerializable
	{
		public OptionalLong foo = new OptionalLong();
        public virtual void Serialize(Beamable.Serialization.JsonSerializable.IStreamSerializer s)
        {
            if ((s.HasKey(""foo"") 
                        || ((foo != default(OptionalLong)) 
                        && foo.HasValue)))
			{
				s.Serialize(""foo"", ref foo.Value);
				foo.HasValue = true;
			}
		}
	}
}");

	}

	[Test]
	public void OptionalLongArrayField()
	{
		var type = UnityHelper.GenerateModelDecl("Tuna", new OpenApiSchema
		{
			Type = "object",
			Properties = new Dictionary<string, OpenApiSchema>
			{
				["foo"] = new()
				{
					Type = "array",
					Items = new OpenApiSchema()
					{
						Type = "integer",
						Format = "int64"
					}
				}
			},
			Required = new HashSet<string>()
		});

		Assert.IsNotNull(type);
		var unit = new CodeCompileUnit();
		unit.Namespaces.Add(new CodeNamespace("Test") { Types = { type } });
		var src = UnityHelper.GenerateCsharp(unit);

		src.AssertSrc(@"
namespace Test
{
    
	[System.SerializableAttribute()]
	public partial class Tuna : Beamable.Serialization.JsonSerializable.ISerializable
	{
		public OptionalArrayOfLong foo = new OptionalArrayOfLong();
        public virtual void Serialize(Beamable.Serialization.JsonSerializable.IStreamSerializer s)
        {
            if ((s.HasKey(""foo"") 
                        || ((foo != default(OptionalArrayOfLong)) 
                        && foo.HasValue)))
			{
				s.SerializeArray(""foo"", ref foo.Value);
				foo.HasValue = true;
			}
		}
	}
}");

	}


	[Test]
	public void OptionalLongMapField()
	{
		var type = UnityHelper.GenerateModelDecl("Tuna", new OpenApiSchema
		{
			Type = "object",
			Properties = new Dictionary<string, OpenApiSchema>
			{
				["foo"] = new()
				{
					Type = "object",
					AdditionalProperties = new OpenApiSchema
					{
						Type = "integer",
						Format = "int64"
					},
					AdditionalPropertiesAllowed = true
				}
			},
			Required = new HashSet<string>()
		});

		Assert.IsNotNull(type);
		var unit = new CodeCompileUnit();
		unit.Namespaces.Add(new CodeNamespace("Test") { Types = { type } });
		var src = UnityHelper.GenerateCsharp(unit);

		src.AssertSrc(@"
namespace Test
{
    
	[System.SerializableAttribute()]
	public partial class Tuna : Beamable.Serialization.JsonSerializable.ISerializable
	{
		public OptionalMapOfLong foo = new OptionalMapOfLong();
        public virtual void Serialize(Beamable.Serialization.JsonSerializable.IStreamSerializer s)
        {
            if ((s.HasKey(""foo"") 
                        || ((foo != default(OptionalMapOfLong)) 
                        && foo.HasValue)))
			{
				s.SerializeDictionary<MapOfLong, long>(""foo"", ref foo.Value);
				foo.HasValue = true;
			}
		}
	}
}");

	}


	[Test]
	public void OptionalMapOfLongArrayField()
	{
		var type = UnityHelper.GenerateModelDecl("Tuna", new OpenApiSchema
		{
			Type = "object",
			Properties = new Dictionary<string, OpenApiSchema>
			{
				["foo"] = new()
				{
					Type = "object",
					AdditionalProperties = new OpenApiSchema
					{
						Type = "array",
						Items = new OpenApiSchema
						{
							Type = "integer",
							Format = "int64"
						}
					},
					AdditionalPropertiesAllowed = true
				}
			},
			Required = new HashSet<string>()
		});

		Assert.IsNotNull(type);
		var unit = new CodeCompileUnit();
		unit.Namespaces.Add(new CodeNamespace("Test") { Types = { type } });
		var src = UnityHelper.GenerateCsharp(unit);

		src.AssertSrc(@"
namespace Test
{
    
	[System.SerializableAttribute()]
	public partial class Tuna : Beamable.Serialization.JsonSerializable.ISerializable
	{
		public OptionalMapOfArrayOfLong foo = new OptionalMapOfArrayOfLong();
        public virtual void Serialize(Beamable.Serialization.JsonSerializable.IStreamSerializer s)
        {
            if ((s.HasKey(""foo"") 
                        || ((foo != default(OptionalMapOfArrayOfLong)) 
                        && foo.HasValue)))
			{
				s.SerializeDictionary<MapOfArrayOfLong, long[]>(""foo"", ref foo.Value);
				foo.HasValue = true;
			}
		}
	}
}");

	}


	[Test]
	public void OptionalArrayOfMapFishField()
	{
		var type = UnityHelper.GenerateModelDecl("Tuna", new OpenApiSchema
		{
			Type = "object",
			Properties = new Dictionary<string, OpenApiSchema>
			{
				["foo"] = new()
				{
					Type = "array",
					Items = new OpenApiSchema
					{
						Type = "object",
						AdditionalProperties = new OpenApiSchema
						{
							Reference = new OpenApiReference { Id = "Fish" }
						},
						AdditionalPropertiesAllowed = true
					},
					AdditionalPropertiesAllowed = true
				}
			},
			Required = new HashSet<string>()
		});

		Assert.IsNotNull(type);
		var unit = new CodeCompileUnit();
		unit.Namespaces.Add(new CodeNamespace("Test") { Types = { type } });
		var src = UnityHelper.GenerateCsharp(unit);

		src.AssertSrc(@"
namespace Test
{
    
	[System.SerializableAttribute()]
	public partial class Tuna : Beamable.Serialization.JsonSerializable.ISerializable
	{
		public OptionalArrayOfMapOfFish foo = new OptionalArrayOfMapOfFish();
        public virtual void Serialize(Beamable.Serialization.JsonSerializable.IStreamSerializer s)
        {
            if ((s.HasKey(""foo"") 
                        || ((foo != default(OptionalArrayOfMapOfFish)) 
                        && foo.HasValue)))
			{
				s.SerializeList(""foo"", ref foo.Value);
				foo.HasValue = true;
			}
		}
	}
}");

	}

	[Test]
	public void MapOfLongArrayField()
	{
		var type = UnityHelper.GenerateModelDecl("Tuna", new OpenApiSchema
		{
			Type = "object",
			Properties = new Dictionary<string, OpenApiSchema>
			{
				["foo"] = new()
				{
					Type = "object",
					AdditionalProperties = new OpenApiSchema
					{
						Type = "array",
						Items = new OpenApiSchema
						{
							Type = "integer",
							Format = "int64"
						}
					},
					AdditionalPropertiesAllowed = true
				}
			},
			Required = new HashSet<string> { "foo" }
		});

		Assert.IsNotNull(type);
		var unit = new CodeCompileUnit();
		unit.Namespaces.Add(new CodeNamespace("Test") { Types = { type } });
		var src = UnityHelper.GenerateCsharp(unit);

		src.AssertSrc(@"
namespace Test
{
    
	[System.SerializableAttribute()]
	public partial class Tuna : Beamable.Serialization.JsonSerializable.ISerializable
	{
		public MapOfArrayOfLong foo = new MapOfArrayOfLong();
        public virtual void Serialize(Beamable.Serialization.JsonSerializable.IStreamSerializer s)
        {
			s.SerializeDictionary<MapOfArrayOfLong, long[]>(""foo"", ref foo);
		}
	}
}");

	}


	[Test]
	public void WithInternalObject()
	{
		var type = UnityHelper.GenerateModelDecl("Tuna", new OpenApiSchema
		{
			Type = "object",
			Properties = new Dictionary<string, OpenApiSchema>
			{
				["foo"] = new()
				{
					Reference = new OpenApiReference
					{
						Id = "Fish"
					}
				}
			},
			Required = new HashSet<string> { "foo" }
		});

		Assert.IsNotNull(type);
		var unit = new CodeCompileUnit();
		unit.Namespaces.Add(new CodeNamespace("Test") { Types = { type } });
		var src = UnityHelper.GenerateCsharp(unit);

		src.AssertSrc(@"
namespace Test
{
    
	[System.SerializableAttribute()]
	public partial class Tuna : Beamable.Serialization.JsonSerializable.ISerializable
	{
		public Fish foo = new Fish();
        public virtual void Serialize(Beamable.Serialization.JsonSerializable.IStreamSerializer s)
        {
			s.Serialize(""foo"", ref foo);
		}
	}
}");

	}
}
