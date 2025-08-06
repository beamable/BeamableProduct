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
	public void Export_Wildcard_WritesExportAll()
	{
		const string expected = "export * from 'module';\n";
		var exp = new TsExport("module");
		var writer = new TsCodeWriter();
		exp.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, exp.Render());
	}

	[Test]
	public void Export_Named_WritesNamedExport()
	{
		const string expected = "export { a, b } from 'mod';\n";
		var exp = new TsExport("mod")
			.AddNamedExport("a")
			.AddNamedExport("b");
		var writer = new TsCodeWriter();
		exp.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, exp.Render());
	}

	[Test]
	public void Export_DefaultAlias_WritesDefaultAliasExport()
	{
		const string expected = "export { default as def } from 'mod';\n";
		var exp = new TsExport("mod", "def");
		var writer = new TsCodeWriter();
		exp.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, exp.Render());
	}

	[Test]
	public void Export_DefaultAndNamed_WritesCombinedExport()
	{
		const string expected = "export { default as def, a, b } from 'mod';\n";
		var exp = new TsExport("mod", "def")
			.AddNamedExport("a")
			.AddNamedExport("b");
		var writer = new TsCodeWriter();
		exp.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, exp.Render());
	}

	[Test]
	public void File_WithImportAndDeclaration_WritesFileContents()
	{
		const string expected = "import 'm';\n" +
		                        "\n" +
		                        "class X {\n" +
		                        "}\n";
		var file = new TsFile("file.ts", false)
			.AddImport(new TsImport("m"))
			.AddDeclaration(new TsClass("X"));
		var writer = new TsCodeWriter();
		file.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, file.Render());
	}

	[Test]
	public void File_WithExport_WritesFileContents()
	{
		const string expected = "export { X } from 'm';\n";
		var file = new TsFile("file.ts", false)
			.AddExport(new TsExport("m").AddNamedExport("X"));
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
			"  \n" +
			"  n(): void {\n" +
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
			.AddMethod(new TsMethod("n"))
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

	[Test]
	public void Declare_Class_WritesDeclareClass()
	{
		const string expected =
			"declare class C {\n" +
			"}\n";
		var decl = new TsDeclare(new TsClass("C"));
		var writer = new TsCodeWriter();
		decl.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, decl.Render());
	}

	[Test]
	public void Module_Minimal_WritesModule()
	{
		const string expected = "module 'm';\n";
		var mb = new TsModule("m");
		var writer = new TsCodeWriter();
		mb.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, mb.Render());
	}

	[Test]
	public void ModuleBlock_WithStatements_WritesStatementsInModule()
	{
		const string expected =
			"module 'm' {\n" +
			"  class C {\n" +
			"  }\n" +
			"  class D {\n" +
			"  }\n" +
			"}\n";
		var mb = new TsModule("m")
			.AddStatement(new TsClass("C"))
			.AddStatement(new TsClass("D"));
		var writer = new TsCodeWriter();
		mb.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, mb.Render());
	}
}
