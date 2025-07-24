using System;
using NUnit.Framework;
using cli.Services.Web.CodeGen;

namespace tests.Web.CodeGen;

[TestFixture]
public class TsMembersTests
{
	[Test]
	public void Property_WritesProperty()
	{
		const string expected = "private x?: string = undefined;\n";
		var prop = new TsProperty("x", TsType.String)
			.AddModifier(TsModifier.Private)
			.AsOptional()
			.WithInitializer(new TsLiteralExpression("undefined"));
		var writer = new TsCodeWriter();
		prop.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, prop.Render());
	}

	[Test]
	public void Method_Minimal_WritesMethodWithNoBody()
	{
		const string expected = "async m<T>(a: T): void {\n" +
		                        "  await a;\n" +
		                        "}\n";
		var method = new TsMethod("m")
			.AddModifier(TsModifier.Async)
			.AddTypeParameter(new TsGenericParameter("T"))
			.AddParameter(new TsFunctionParameter("a", TsType.Of("T")))
			.AddBody(new TsAwaitStatement(new TsIdentifier("a")))
			.SetReturnType(TsType.Void);
		var writer = new TsCodeWriter();
		method.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, method.Render());
	}

	[Test]
	public void Function_WritesFunctionDeclaration()
	{
		const string expected = "export default function f<T>(a: string, b: T): void {\n" +
		                        "  \"k\";\n" +
		                        "}\n";
		var fn = new TsFunction("f")
			.AddModifier(TsModifier.Export | TsModifier.Default)
			.AddTypeParameter(new TsGenericParameter("T"))
			.AddParameter(new TsFunctionParameter("a", TsType.String))
			.AddParameter(new TsFunctionParameter("b", TsType.Of("T")))
			.AddBody(new TsExpressionStatement(new TsLiteralExpression("k")))
			.SetReturnType(TsType.Void);
		var writer = new TsCodeWriter();
		fn.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, fn.Render());
	}

	[Test]
	public void Variable_Minimal_WritesLetDeclaration()
	{
		const string expected = "let v;\n";
		var v = new TsVariable("v");
		var writer = new TsCodeWriter();
		v.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, v.Render());
	}

	[Test]
	public void Variable__WritesExportConstDeclaration()
	{
		const string expected = "export const greeting: string = \"Hi\";\n";
		var v = new TsVariable("greeting")
			.AddModifier(TsModifier.Export)
			.AsType(TsType.String)
			.AsConst()
			.WithInitializer(new TsLiteralExpression("Hi"));
		;
		var writer = new TsCodeWriter();
		v.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, v.Render());
	}

	[Test]
	public void FunctionParameter_OptionalAndDefault_WritesParam()
	{
		var p = new TsFunctionParameter("x", TsType.Number)
			.AsOptional()
			.WithDefaultValue(new TsLiteralExpression(5));
		var ex = Assert.Throws<InvalidOperationException>(() => p.Write(new TsCodeWriter()));
		Assert.That(ex?.Message, Does.Contain("cannot be optional and have a default value"));
	}

	[Test]
	public void ConstructorParameter_ProtectedReadonly_WritesOptionalParamProperty()
	{
		const string expected = "protected readonly x?: boolean";
		var p = new TsConstructorParameter("x", TsType.Boolean)
			.AddModifier(TsModifier.Protected | TsModifier.Readonly)
			.AsOptional();
		var writer = new TsCodeWriter();
		p.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, p.Render());
	}

	[Test]
	public void ConstructorParameter_Private_WritesDefaultValueParamProperty()
	{
		const string expected = "private x: boolean = true";
		var p = new TsConstructorParameter("x", TsType.Boolean)
			.AddModifier(TsModifier.Private)
			.WithDefaultValue(new TsLiteralExpression(true));
		var writer = new TsCodeWriter();
		p.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, p.Render());
	}

	[Test]
	public void ArrayDestructureParameter_WritesPattern()
	{
		var param = new TsArrayDestructureParameter("a", "b", "c");
		var writer = new TsCodeWriter();
		param.Write(writer);
		const string expected = "[a, b, c]";
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, param.Render());
	}

	[Test]
	public void FunctionParameter_JavaScript_WritesParamOptionalWithoutMarker()
	{
		const string expected = "x";
		var p = new TsFunctionParameter("x", TsType.String, true)
			.AsOptional();
		var writer = new TsCodeWriter();
		p.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, p.Render());
	}

	[Test]
	public void FunctionParameter_JavaScript_WritesParamDefaultWithoutType()
	{
		const string expected = "x = 5";
		var p = new TsFunctionParameter("x", TsType.Number, true)
			.WithDefaultValue(new TsLiteralExpression(5));
		var writer = new TsCodeWriter();
		p.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, p.Render());
	}

	[Test]
	public void Method_JavaScript_Minimal_WritesMethodWithoutTypes()
	{
		const string expected = "async m(a) {\n" +
		                        "  await a;\n" +
		                        "}\n";
		var method = new TsMethod("m", true)
			.AddModifier(TsModifier.Async)
			.AddTypeParameter(new TsGenericParameter("T"))
			.AddParameter(new TsFunctionParameter("a", TsType.Of("T"), true))
			.AddBody(new TsAwaitStatement(new TsIdentifier("a")))
			.SetReturnType(TsType.Void);
		var writer = new TsCodeWriter();
		method.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, method.Render());
	}
}
