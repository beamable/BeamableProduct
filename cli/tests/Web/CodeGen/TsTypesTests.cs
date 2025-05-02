using System.Collections.Generic;
using NUnit.Framework;
using cli.Services.Web.CodeGen;

namespace tests.Web.CodeGen;

[TestFixture]
public class TsTypesTests
{
	[Test]
	public void Identifier_WritesName()
	{
		const string expected = "foo";
		var id = new TsIdentifier("foo");
		var writer = new TsCodeWriter();
		id.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, id.Render());
	}

	[Test]
	public void GenericParameter_WithConstraint_WritesExtends()
	{
		const string expected = "T extends number";
		var gp = new TsGenericParameter("T", TsType.Number);
		var writer = new TsCodeWriter();
		gp.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, gp.Render());
	}

	[Test]
	public void LiteralExpression_WritesValues()
	{
		int[] arr = { 1, 2, 3 };
		const string obj = "{\n" +
		                   "  \"k1\": \"v1\",\n" +
		                   "  \"k2\": \"v2\"\n" +
		                   "}";
		const string nestedObj = "{\n" +
		                         "  \"k1\": {\n" +
		                         "    \"k2\": \"v2\"\n" +
		                         "  }\n" +
		                         "}";
		Assert.AreEqual("1", new TsLiteralExpression(1).Render());
		Assert.AreEqual("\"s\"", new TsLiteralExpression("s").Render());
		Assert.AreEqual("null", new TsLiteralExpression(null).Render());
		Assert.AreEqual("undefined", new TsLiteralExpression("undefined").Render());
		Assert.AreEqual("true", new TsLiteralExpression(true).Render());
		Assert.AreEqual("[1, 2, 3]", new TsLiteralExpression(arr).Render());
		Assert.AreEqual(obj,
			new TsLiteralExpression(new Dictionary<string, object> { { "k1", "v1" }, { "k2", "v2" } }).Render());
		Assert.AreEqual(nestedObj,
			new TsLiteralExpression(new Dictionary<string, object>
			{
				{ "k1", new Dictionary<string, object> { { "k2", "v2" } } }
			}).Render());
	}

	[Test]
	public void MemberAccessExpression_WritesDotAndBracket()
	{
		var dot = new TsMemberAccessExpression(new TsIdentifier("o"), "p");
		Assert.AreEqual("o.p", dot.Render());
		var br = new TsMemberAccessExpression(new TsIdentifier("o"), "p", false);
		Assert.AreEqual("o['p']", br.Render());
	}

	[Test]
	public void InvokeExpression_WritesCallWithArgs()
	{
		var call = new TsInvokeExpression(new TsIdentifier("f"), new TsLiteralExpression(1),
			new TsLiteralExpression(2));
		Assert.AreEqual("f(1, 2)", call.Render());
	}

	[Test]
	public void UnaryExpression_PrefixAndPostfix_WritesOperator()
	{
		var pre = new TsUnaryExpression(new TsIdentifier("a"), TsUnaryOperatorType.Not);
		Assert.AreEqual("!a", pre.Render());
		var post = new TsUnaryExpression(new TsIdentifier("a"), TsUnaryOperatorType.Increment,
			TsUnaryOperatorPosition.Postfix);
		Assert.AreEqual("a++", post.Render());
	}

	[Test]
	public void UtilityTypes_WritesGenericUtilityTypes()
	{
		var p = TsUtilityType.Partial(TsType.Of("Foo"));
		Assert.AreEqual("Partial<Foo>", p.Render());
		var r = TsUtilityType.Record(TsType.String, TsType.Number);
		Assert.AreEqual("Record<string, number>", r.Render());
	}

	[Test]
	public void TsType_FactoryMethods_WritesExpected()
	{
		Assert.AreEqual("number[]", TsType.ArrayOf(TsType.Number).Render());
		Assert.AreEqual("[number, string]", TsType.Tuple(TsType.Number, TsType.String).Render());
		Assert.AreEqual("number | string", TsType.Union(TsType.Number, TsType.String).Render());
		Assert.AreEqual("number & string", TsType.Intersection(TsType.Number, TsType.String).Render());
		Assert.AreEqual("number | undefined", TsType.Optional(TsType.Number).Render());
		Assert.AreEqual("CustomType", TsType.Of("CustomType").Render());
	}
}
