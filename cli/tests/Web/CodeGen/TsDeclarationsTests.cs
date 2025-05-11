using NUnit.Framework;
using cli.Services.Web.CodeGen;
using System.Collections.Generic;

namespace tests.Web.CodeGen;

[TestFixture]
public class TsDeclarationsTests
{
	[Test]
	public void Import_SideEffectOnly_WritesImportStatement()
	{
		const string expected = "import 'module';\n";
		var imp = new TsImport("module");
		var writer = new TsCodeWriter();
		imp.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, imp.Render());
	}

	[Test]
	public void Import_DefaultAndNamed_WritesCombinedImport()
	{
		const string expected = "import def, { a, b } from 'mod';\n";
		var imp = new TsImport("mod", "def")
			.AddNamedImport("a")
			.AddNamedImport("b");
		var writer = new TsCodeWriter();
		imp.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, imp.Render());
	}

	[Test]
	public void File_WithImportAndDeclaration_WritesFileContents()
	{
		const string expected = "import 'm';\n" +
		                        "\n" +
		                        "class X {\n" +
		                        "}\n";
		var file = new TsFile("file.ts")
			.AddImport(new TsImport("m"))
			.AddDeclaration(new TsClass("X"));
		var writer = new TsCodeWriter();
		file.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, file.Render());
	}

	[Test]
	public void Class_Minimal_WritesClassDeclaration()
	{
		const string expected =
			"class C {\n" +
			"}\n";
		var cls = new TsClass("C");
		var writer = new TsCodeWriter();
		cls.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, cls.Render());
	}

	[Test]
	public void Class_WritesFullClass()
	{
		const string expected =
			"export abstract class Foo<T> extends Bar implements IFile {\n" +
			"  p: number;\n" +
			"  q: string;\n" +
			"  \n" +
			"  constructor(\n" +
			"    p: number,\n" +
			"    q: string\n" +
			"  ) {\n" +
			"    this.p = p;\n" +
			"    this.q = q;\n" +
			"  }\n" +
			"  \n" +
			"  m(): void {\n" +
			"  }\n" +
			"}\n";
		var constructorBodies = new List<TsNode>
		{
			new TsAssignmentStatement(
				new TsMemberAccessExpression(new TsIdentifier("this"), "p"),
				new TsIdentifier("p")),
			new TsAssignmentStatement(
				new TsMemberAccessExpression(new TsIdentifier("this"), "q"),
				new TsIdentifier("q"))
		};
		var cls = new TsClass("Foo")
			.AddModifier(TsModifier.Export | TsModifier.Abstract)
			.AddTypeParameter(new TsGenericParameter("T"))
			.AddImplements(new TsIdentifier("IFile"))
			.AddProperty(new TsProperty("p", TsType.Number))
			.AddProperty(new TsProperty("q", TsType.String))
			.SetConstructor(new TsConstructor()
				.AddParameter(new TsConstructorParameter("p", TsType.Number))
				.AddParameter(new TsConstructorParameter("q", TsType.String))
				.AddBody(constructorBodies.ToArray())
			)
			.AddMethod(new TsMethod("m"))
			.SetExtends(new TsIdentifier("Bar"));
		var writer = new TsCodeWriter();
		cls.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, cls.Render());
	}

	[Test]
	public void Interface_WritesInterfaceDeclaration()
	{
		const string expected = "export interface IImageFile<T> extends IFile {\n" +
		                        "  size: number;\n" +
		                        "}\n";
		var i = new TsInterface("IImageFile")
			.AddModifier(TsModifier.Export)
			.AddTypeParameter(new TsGenericParameter("T"))
			.AddExtends(new TsIdentifier("IFile"))
			.AddProperty(new TsProperty("size", TsType.Number));
		;
		var writer = new TsCodeWriter();
		i.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, i.Render());
	}

	[Test]
	public void Enum_Minimal_WritesEnumDeclaration()
	{
		const string expected = "enum E {\n" +
		                        "}\n";
		var e = new TsEnum("E");
		var writer = new TsCodeWriter();
		e.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, e.Render());
	}

	[Test]
	public void Enum_WithConstAndMembers_WritesConstEnum()
	{
		const string expected = "export const enum E {\n" +
		                        "  A,\n" +
		                        "  B = 1,\n" +
		                        "  C = \"2\"\n" +
		                        "}\n";
		var e = new TsEnum("E")
			.AddModifier(TsModifier.Export)
			.AddMember(new TsEnumMember("A"))
			.AddMember(new TsEnumMember("B", "1", TsEnumMemberValueType.Number))
			.AddMember(new TsEnumMember("C", "2"))
			.AsConst();
		var writer = new TsCodeWriter();
		e.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, e.Render());
	}

	[Test]
	public void TypeAlias_Minimal_WritesAliasDeclaration()
	{
		const string expected = "type T = bigint;\n";
		var ta = new TsTypeAlias("T")
			.SetType(TsType.BigInt);
		var writer = new TsCodeWriter();
		ta.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, ta.Render());
	}

	[Test]
	public void TypeAlias_WithModifiersAndParams_WritesFullAlias()
	{
		const string expected = "export type T<U extends string> = Array<U>;\n";
		var ta = new TsTypeAlias("T")
			.AddModifier(TsModifier.Export)
			.AddTypeParameter(new TsGenericParameter("U", TsType.String))
			.SetType(TsType.Generic("Array", TsType.Of("U")));
		var writer = new TsCodeWriter();
		ta.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, ta.Render());
	}
}
