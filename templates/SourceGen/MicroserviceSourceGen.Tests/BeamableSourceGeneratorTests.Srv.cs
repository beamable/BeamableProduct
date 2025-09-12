using Beamable.Microservice.SourceGen;
using Beamable.Microservice.SourceGen.Analyzers;
using Beamable.Microservice.SourceGen.Fixers;
using Beamable.Server;
using Microservice.SourceGen.Tests.Dep;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microservice.SourceGen.Tests;

public partial class BeamableSourceGeneratorTests
{
	[Fact]
	public async Task Test_Diagnostic_Srv_UsesFedFromAnotherProject()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;
using Microservice.SourceGen.Tests.Dep;

namespace TestNamespace;

[Microservice(""TunaService"")]
public partial class TunaService : Beamable.Server.Microservice, IFederatedLogin<ExampleFederationId>
{		
	public Promise<FederatedAuthenticationResponse> Authenticate(string token, string challenge, string solution)
	{
		throw new System.NotImplementedException();
	}
}";

		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		// Microsoft.CodeAnalysis.Testing v1.1.1 used on tests don't have the Net9 Reference, so we need to manually create it
		ctx.ReferenceAssemblies = new ReferenceAssemblies("net9.0",
			new PackageIdentity("Microsoft.NETCore.App.Ref", "9.0.0"), Path.Combine("ref", "net9.0"));
		PrepareForRun(ctx, UserCode);
		
		ctx.TestState.AdditionalReferences.Add(Assembly.GetAssembly(typeof(ExampleFederationId))!);
		
		await ctx.RunAsync();
		
	}

	
	[Fact]
	public async Task Test_Diagnostic_Srv_NoMicroserviceClassesDetected_IsFine()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

public class SomeUserMicroservice
{		
}";
		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		PrepareForRun(ctx, UserCode);

		await ctx.RunAsync();
	}

	[Fact]
	public async Task Test_Diagnostic_Srv_MultipleMicroserviceClassesDetected_IsFine()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

[Microservice(""id"")]
public partial class SomeUserMicroservice : Microservice
{		
}

[Microservice(""id2"")]
public partial class {|#0:SomeOtherUserMicroservice|} : Microservice
{		
}
";
		
		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		PrepareForRun(ctx, UserCode);

		await ctx.RunAsync();
	}
	
	[Fact]
	public async Task Test_Diagnostic_Srv_MultipleMicroserviceClassesDetected_PartialCompatibility()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

[Microservice(""someid"")]
public partial class {|#0:SomeUserMicroservice|} : Microservice
{		
}

public partial class SomeUserMicroservice : Microservice
{		
}
";
		
		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		PrepareForRun(ctx, UserCode);
		
		await ctx.RunAsync();
	}

	[Fact]
	public async Task Test_Diagnostic_Srv_NonPartialMicroserviceClassDetected_IsFine()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

[Microservice(""id"")]
public class {|#0:SomeUserMicroservice|} : Microservice
{		
}
";
		
		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		PrepareForRun(ctx, UserCode);

		await ctx.RunAsync();
	}

	[Fact]
	public async Task Test_Diagnostic_Srv_MissingMicroserviceId_IsFine()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

public partial class {|#0:SomeUserMicroservice|} : Microservice
{		
}
";
		
		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		PrepareForRun(ctx, UserCode);

		await ctx.RunAsync();
	}
	
	
	[Fact]
	public async Task Test_Diagnostic_Srv_ParameterViaInject_IsFine()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

public class TunaSauce : Microservice
{	
	[ClientCallable]
	public void Tuna([Inject]IUserScope scope) {}
}
";
		
		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		PrepareForRun(ctx, UserCode);
		
		await ctx.RunAsync();
	}

	
	[Fact]
	public async Task Test_Diagnostic_Srv_ParameterViaParameterSource_IsFine()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

public class TunaSauce : Microservice
{	
	[ClientCallable]
	public void Tuna([Parameter(source: ParameterSource.Injection)]IUserScope scope) {}
}
";
		
		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		PrepareForRun(ctx, UserCode);
		
		await ctx.RunAsync();
	}

	
	[Fact]
	public async Task Test_Diagnostic_Srv_InvalidAsyncVoidCallableMethod()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

[Microservice(""some_user_service"")]
public partial class SomeUserMicroservice : Microservice
{
	[ClientCallable]
	public async {|#0:void|} ClientTestAsyncCallable()
	{
	}

	[ServerCallable]
	public async {|#1:void|} ServerTestAsyncCallable()
	{
	}

	[Callable]
	public async {|#2:void|} TestAsyncCallable()
	{
	}
}
";
		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		PrepareForRun(ctx, UserCode);

		ctx.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Srv.InvalidAsyncVoidCallableMethod)
			.WithLocation(0)
			.WithArguments("ClientTestAsyncCallable"));
		ctx.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Srv.InvalidAsyncVoidCallableMethod)
			.WithLocation(1)
			.WithArguments("ServerTestAsyncCallable"));
		ctx.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Srv.InvalidAsyncVoidCallableMethod)
			.WithLocation(2)
			.WithArguments("TestAsyncCallable"));
		
		await ctx.RunAsync();
	}
	
	[Fact]
	public async Task Test_Diagnostic_Srv_ReturnTypeInsideServerScope()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;
using System.Threading.Tasks;
using System;

namespace TestNamespace;



[Microservice(""MyMicroservice"")]
public partial class MyMicroservice : Microservice 
{

	[Beamable.BeamGenerateSchema]
	[Serializable]
	public class {|#0:DTO_Attribute|}
	{
		public int x;
	}

	[Serializable]
	public class DTO_Nested
	{
		public int x;
	}

	[ClientCallable]
	public {|#1:DTO_Nested|} CallService_Nested() 
	{
		return new DTO_Nested{ x = 1 };
	}


	[ClientCallable]
	public async {|#2:Task<DTO_MicroserviceScope>|} CallServiceAsync(DTO_MicroserviceScope {|#3:param|}) 
	{
	return new DTO_MicroserviceScope{ x = 1 };
	}
}

[Serializable]
public class DTO_MicroserviceScope 
{
    public int x;
}
";
		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		PrepareForRun(ctx, UserCode, ["build_property.BeamValidateCallableTypesExistInSharedLibraries = true"]);

		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.ClassBeamGenerateSchemaAttributeIsNested)
				.WithLocation(0)
				.WithArguments("DTO_Attribute"));
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.CallableMethodTypeIsNested)
				.WithLocation(1)
				.WithArguments("DTO_Nested", "CallService_Nested"));
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.CallableTypeInsideMicroserviceScope)
				.WithLocation(1)
				.WithArguments("CallService_Nested", "DTO_Nested"));
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.CallableTypeInsideMicroserviceScope)
				.WithLocation(2)
				.WithArguments("CallServiceAsync", "DTO_MicroserviceScope"));
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.CallableTypeInsideMicroserviceScope)
				.WithLocation(3)
				.WithArguments("CallServiceAsync", "DTO_MicroserviceScope"));
		
		
		await ctx.RunAsync();
	}
	
	[Fact]
	public async Task Test_Diagnostic_Srv_ReturnTypeNonCallableMethod()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;
using System.Threading.Tasks;
using System;

namespace TestNamespace;

[Microservice(""MyMicroservice"")]
public partial class MyMicroservice : Microservice 
{
	[Serializable]
	public class DTO_Nested
	{
		public int x;
	}

	public DTO_Nested CallService_Nested() 
	{
		return new DTO_Nested{ x = 1 };
	}
}
";
		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		PrepareForRun(ctx, UserCode);

		string config = $@"
is_global = true
build_property.BeamValidateCallableTypesExistInSharedLibraries = true
";
		ctx.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", config));
		
		await ctx.RunAsync();
	}
	
	[Fact]
	public async Task Test_Diagnostic_Srv_NonReadonlyStaticField()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;
using System.Threading.Tasks;

namespace TestNamespace;

[Microservice(""MyMicroservice"")]
public partial class MyMicroservice : Microservice 
{
	static string {|#0:TestField|} = ""Hello"";
}
";
		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		PrepareForRun(ctx, UserCode);
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.StaticFieldFoundInMicroservice)
				.WithLocation(0)
				.WithArguments("TestField"));
		
		await ctx.RunAsync();
	}
	
	[Fact]
	public async Task Test_Diagnostic_Srv_MissingSerializableOnType()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;
using System.Threading.Tasks;
using System;

namespace TestNamespace;

[Microservice(""MyMicroservice"")]
public partial class MyMicroservice : Microservice 
{
	[ClientCallable]
	public async Task<DTO_AsyncTest> CallServiceAsync() 
	{
		return new DTO_AsyncTest{ x = 1 };
	}

	[ClientCallable]
	public DTO_Test CallService() 
	{
		return new DTO_Test{ x = 1 };
	}

	[ClientCallable]
	public NonSerializableEnum CallServiceEnum() 
	{
		return NonSerializableEnum.None;
	}
}

public struct {|#0:DTO_AsyncTest|}
{
    public int x;
}

public class {|#1:DTO_Test|}
{
    public int x;
	public OtherNonSerializableEnum y;
}

[Beamable.BeamGenerateSchema]
public class {|#2:DTO_BeamGenSchemaAttribute|}
{
	public int x;
}

public enum NonSerializableEnum {
	None = 0,
}

public enum OtherNonSerializableEnum {
	None = 0,
}


";
		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.MissingSerializableAttributeOnType)
				.WithLocation(0)
				.WithArguments("DTO_AsyncTest"));
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.MissingSerializableAttributeOnType)
				.WithLocation(1)
				.WithArguments("DTO_Test"));
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.MissingSerializableAttributeOnType)
				.WithLocation(2)
				.WithArguments("DTO_BeamGenSchemaAttribute"));
		
		PrepareForRun(ctx, UserCode);
		
		await ctx.RunAsync();
	}
	
	[Fact]
	public async Task Test_Diagnostic_Srv_PropertiesFoundInSerializableTypes()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;
using System.Threading.Tasks;
using System;

namespace TestNamespace;

[Microservice(""MyMicroservice"")]
public partial class MyMicroservice : Microservice 
{
	[ClientCallable]
	public async Task<DTO_AsyncTest> CallServiceAsync() 
	{
		return new DTO_AsyncTest{ x = 1 };
	}

	[ClientCallable]
	public async Task<DTO_Test> CallService() 
	{
		return new DTO_Test{ x = 1 };
	}
}

[Serializable]
public class DTO_AsyncTest
{
    public int x;
	public System.Int32 c;
	public int {|#0:Prop1|} {get; set;}
}

[Serializable]
public class DTO_Test
{
    public int x;
	public int {|#1:Prop2|} {get; set;}
}

[Serializable]
[Beamable.BeamGenerateSchema]
public class DTO_BeamGenSchemaAttribute
{
	public int x;
	public int {|#2:Prop3|} {get; set;}
}
";
		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.PropertiesFoundInSerializableTypes)
				.WithLocation(0)
				.WithArguments("DTO_AsyncTest", "Prop1"));
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.PropertiesFoundInSerializableTypes)
				.WithLocation(1)
				.WithArguments("DTO_Test", "Prop2"));
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.PropertiesFoundInSerializableTypes)
				.WithLocation(2)
				.WithArguments("DTO_BeamGenSchemaAttribute", "Prop3"));
		
		PrepareForRun(ctx, UserCode);
		
		await ctx.RunAsync();
	}
	
	[Fact]
	public async Task Test_Diagnostic_Srv_NullableFieldInSerializableTypes()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;
using System.Threading.Tasks;
using System;

namespace TestNamespace;

[Microservice(""MyMicroservice"")]
public partial class MyMicroservice : Microservice 
{
	[ClientCallable]
	public async Task<DTO_AsyncTest> CallServiceAsync() 
	{
		return new DTO_AsyncTest{ nullableInt = 1 };
	}

	[ClientCallable]
	public async Task<DTO_Test> CallService() 
	{
		return new DTO_Test{ nullableBool = true };
	}
}

[Serializable]
public class DTO_AsyncTest
{
    public int? {|#0:nullableInt|};
}

[Serializable]
public class DTO_Test
{
    public bool? {|#1:nullableBool|};
}

[Serializable]
[Beamable.BeamGenerateSchema]
public class DTO_BeamGenSchemaAttribute
{
	public long? {|#2:nullableLong|};
}
";
		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.NullableTypeFoundInMicroservice)
				.WithLocation(0)
				.WithArguments("DTO_AsyncTest", "nullableInt"));
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.NullableTypeFoundInMicroservice)
				.WithLocation(1)
				.WithArguments("DTO_Test", "nullableBool"));
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.NullableTypeFoundInMicroservice)
				.WithLocation(2)
				.WithArguments("DTO_BeamGenSchemaAttribute", "nullableLong"));
		
		PrepareForRun(ctx, UserCode);
		
		await ctx.RunAsync();
	}
	
	[Fact]
	public async Task Test_Diagnostic_Srv_FieldInSerializableTypeIsContentObjectSubtype()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;
using System.Threading.Tasks;
using System;
using Beamable.Common.Content;

namespace TestNamespace;

[Microservice(""MyMicroservice"")]
public partial class MyMicroservice : Microservice 
{
	[ClientCallable]
	public async Task<DTO_AsyncTest> CallServiceAsync() 
	{
		return new DTO_AsyncTest();
	}

	[ClientCallable]
	public async Task<DTO_Test> CallService() 
	{
		return new DTO_Test();
	}
}

[Serializable]
[Beamable.BeamGenerateSchema]
public class OtherContentObject : ContentObject {}

[Serializable]
public class DTO_AsyncTest
{
    public OtherContentObject {|#0:otherContentObject1|};
}

[Serializable]
public class DTO_Test
{
    public OtherContentObject {|#1:otherContentObject2|};
}

[Serializable]
[Beamable.BeamGenerateSchema]
public class DTO_BeamGenSchemaAttribute
{
	public OtherContentObject {|#2:otherContentObject3|};
}
";
		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.InvalidContentObject)
				.WithLocation(0)
				.WithArguments("field otherContentObject1 on DTO_AsyncTest", "OtherContentObject"));
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.InvalidContentObject)
				.WithLocation(1)
				.WithArguments("field otherContentObject2 on DTO_Test", "OtherContentObject"));
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.InvalidContentObject)
				.WithLocation(2)
				.WithArguments("field otherContentObject3 on DTO_BeamGenSchemaAttribute", "OtherContentObject"));
		
		PrepareForRun(ctx, UserCode);
		
		await ctx.RunAsync();
	}
	
	[Fact]
	public async Task Test_Diagnostic_Srv_FieldInBeamSchemaIsMissingAttribute()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;
using System.Threading.Tasks;
using System;

namespace TestNamespace;

[Microservice(""MyMicroservice"")]
public partial class MyMicroservice : Microservice 
{
	
}


[Serializable]
public class {|#0:DTO_NonBeamGenSchemaAttribute|}
{
	public int x;
}

[Serializable]
[Beamable.BeamGenerateSchema]
public class DTO_BeamGenSchemaAttribute
{
	public DTO_NonBeamGenSchemaAttribute otherNonBeamGenObj;
}
";
		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.TypeInBeamGeneratedIsMissingBeamGeneratedAttribute)
				.WithLocation(0)
				.WithArguments("DTO_NonBeamGenSchemaAttribute"));
		
		PrepareForRun(ctx, UserCode);
		
		await ctx.RunAsync();
	}
	
	[Fact]
	public async Task Test_Diagnostic_Srv_FullImplementationExample_Success()
	{
		const string UserCode = @"
using Beamable.Common.Content;
using Beamable.Common;
using Beamable.Server;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System;
using System;

namespace Unity.Beamable.Customer.Common
{

	[Serializable]
	public enum SalmonEnum
	{
		None = 0,
		One = 1,
		Two = 2,
	}
	[Serializable]
	public class AddSalmonData
	{
		public int a;
		public int b;
		public SalmonEnum c;
		public List<string> listSubtypeField;
		public Dictionary<string, SalmonEnum> dictSubtypeField;
	}
    
	[Serializable]
	public struct TempStructOptional
	{
		public int a;
		public int b;
		public Optional<AddSalmonData> {|#0:data|};
	}

	[Serializable]
	public class ItemReward
	{
		public string contentID;
		public long instanceID = 0;
		public long amount = 0;
		public Dictionary<string, string> properties;
	}
	
}

namespace TestNamespace {
using Unity.Beamable.Customer.Common;
	[Microservice(""MyMicroservice"")]
	public partial class MyMicroservice : Microservice 
	{
		[ServerCallable]
		public int AddSalmon(int a, int b)
		{
			return a + b;
		}

		[ClientCallable]
		public async Task<List<ItemReward>> AddSalmonAsync(Dictionary<string,int> a, ContentRef<ContentObject> b)
		{
			await Task.Delay(1000);
			return new();
		}

		[ClientCallable]
		public async Task<List<ItemReward>> AddSalmonAsync(int a, int b)
		{
			await Task.Delay(1000);
			return new();
		}
		
		[ClientCallable]
		public int AddSalmonLongInt(long a, int b)
		{
			return (int)a + b;
		}

		[ClientCallable]
		public int AddSalmonData(AddSalmonData data)
		{
			return data.a + data.b;
		}
		
		[Callable]
		public int AddSalmonOptional(int a, int b, DateTime t, Guid? {|#1:guid|} = null, int? {|#2:c|} = null, DateTime? {|#3:d|} = null, AddSalmonData? {|#4:g|} = null, string? {|#5:h|} = null)
		{
			return a + b + ( c ?? 0);
		}
		
		[ClientCallable]
		public int AddSalmonOptionalStruct(OptionalInt a, Optional<TempStructOptional> {|#6:b|}, TempStructOptional c = default)
		{
			return a + b.Value.a + c.a + c.b;
		}

		[ClientCallable]
		public int AddSalmonList(List<int> item)
		{
			return item.Sum(item => item);
		}

		[ClientCallable]
		public int TestSalmonByte(byte[] bytes)
		{
			return bytes.Length;
		}

		[ClientCallable]
		public float AddSalmonFloat(float a, float b)
		{
			return a + b;
		}

		[ClientCallable]
		public double AddSalmonDouble(double a, double b)
		{
			return a + b;
		}

		[ClientCallable]
		public string TestSalmonDate(DateTime date)
		{
			return date.ToString(CultureInfo.InvariantCulture);
		}

		[ClientCallable]
		public string TestSalmonGuid(Guid guid)
		{
			return guid.ToString();
		}

		[ClientCallable]
		public string TestSalmonString(string stringValue)
		{
			return stringValue;
		}

		[ClientCallable]
		public string TestSalmonBool(bool booleanValue)
		{
			return booleanValue.ToString();
		}
		
		[ClientCallable(flags: CallableFlags.SkipGenerateClientFiles)]
		public void TestSalmonSkipGenerate()
		{
			Console.WriteLine(""TestSalmonSkipGenerate"");
		}
	}
}

";
		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		PrepareForRun(ctx, UserCode);
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.InvalidGenericTypeOnMicroservice)
				.WithLocation(0)
				.WithArguments("field data", "TempStructOptional"));
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.NullableTypeFoundInMicroservice)
				.WithLocation(1)
				.WithArguments("parameter guid in AddSalmonOptional", "Guid?"));
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.NullableTypeFoundInMicroservice)
				.WithLocation(2)
				.WithArguments("parameter c in AddSalmonOptional", "int?"));
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.NullableTypeFoundInMicroservice)
				.WithLocation(3)
				.WithArguments("parameter d in AddSalmonOptional", "DateTime?"));
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.NullableTypeFoundInMicroservice)
				.WithLocation(4)
				.WithArguments("parameter g in AddSalmonOptional", "AddSalmonData?"));

		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.NullableTypeFoundInMicroservice)
				.WithLocation(5)
				.WithArguments("parameter h in AddSalmonOptional", "string?"));

		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.InvalidGenericTypeOnMicroservice)
				.WithLocation(6)
				.WithArguments("parameter b", "AddSalmonOptionalStruct"));
		
		await ctx.RunAsync();
	}
	
	[Fact]
	public async Task Test_CodeFixer_Srv_InvalidAsyncVoidCallableMethod()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;
using System.Threading.Tasks;

namespace TestNamespace;

[Microservice(""some_user_service"")]
public partial class SomeUserMicroservice : Microservice
{
	[ClientCallable]
	public async {|#0:void|} ClientTestAsyncCallable()
	{
	}

	[ServerCallable]
	public async {|#1:void|} ServerTestAsyncCallable()
	{
	}

	[Callable]
	public async {|#2:void|} TestAsyncCallable()
	{
	}
}
";
		
		const string FixedCode = @"
using Beamable.Server;
using Beamable.Common;
using System.Threading.Tasks;

namespace TestNamespace;

[Microservice(""some_user_service"")]
public partial class SomeUserMicroservice : Microservice
{
	[ClientCallable]
	public async Task ClientTestAsyncCallable()
	{
	}

	[ServerCallable]
	public async Task ServerTestAsyncCallable()
	{
	}

	[Callable]
	public async Task TestAsyncCallable()
	{
	}
}
";
		var ctx = new CSharpCodeFixTest<ServicesAnalyzer, AsyncVoidCallableFixer, DefaultVerifier>();
		
		PrepareForRun(ctx, UserCode, FixedCode, false);

		ctx.TestState.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Srv.InvalidAsyncVoidCallableMethod)
			.WithLocation(0)
			.WithArguments("ClientTestAsyncCallable"));
		ctx.TestState.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Srv.InvalidAsyncVoidCallableMethod)
			.WithLocation(1)
			.WithArguments("ServerTestAsyncCallable"));
		ctx.TestState.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Srv.InvalidAsyncVoidCallableMethod)
			.WithLocation(2)
			.WithArguments("TestAsyncCallable"));
		
		await ctx.RunAsync();
	}
	
	[Fact]
	public async Task Test_CodeFixer_Srv_InvalidMicroserviceID_NonMatchBeamIdProperty()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;
using System.Threading.Tasks;

namespace TestNamespace;

[Microservice(""MyMicroservice"")]
public partial class {|#0:SomeUserMicroservice|} : Microservice
{		
}
";
		
		const string FixedCode = @"
using Beamable.Server;
using Beamable.Common;
using System.Threading.Tasks;

namespace TestNamespace;

[Microservice(""OtherBeamID"")]
public partial class SomeUserMicroservice : Microservice
{		
}
";
		var ctx = new CSharpCodeFixTest<ServicesAnalyzer, InvalidMicroserviceAttributeFixer, DefaultVerifier>();
		
		PrepareForRun(ctx, UserCode, FixedCode, false);

		ctx.TestState.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Srv.MicroserviceIdInvalidFromCsProj)
			.WithLocation(0)
			.WithArguments("MyMicroservice", "OtherBeamID"));
		
		string config = $@"
is_global = true
build_property.beamid = OtherBeamID
";
		ctx.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", config));
		
		await ctx.RunAsync();
	}
	
	[Fact]
	public async Task Test_CodeFixer_Srv_NonReadonlyStaticField()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;
using System.Threading.Tasks;

namespace TestNamespace;

[Microservice(""MyMicroservice"")]
public partial class MyMicroservice : Microservice 
{
	static string {|#0:TestField|} = ""Hello"";
}
";
		
		const string FixedCode = @"
using Beamable.Server;
using Beamable.Common;
using System.Threading.Tasks;

namespace TestNamespace;

[Microservice(""MyMicroservice"")]
public partial class MyMicroservice : Microservice 
{
    static readonly string TestField = ""Hello"";
}
";
		var ctx = new CSharpCodeFixTest<ServicesAnalyzer, MicroserviceNonReadonlyStaticFieldFixer, DefaultVerifier>();
		
		PrepareForRun(ctx, UserCode, FixedCode, false);
		
		ctx.TestState.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.StaticFieldFoundInMicroservice)
				.WithLocation(0)
				.WithArguments("TestField"));
		
		await ctx.RunAsync();
	}
}
