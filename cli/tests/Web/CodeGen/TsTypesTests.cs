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
	public void InvokeExpression_WritesTypedCallWithTypeArgumentsAndArgs()
	{
		var call = new TsInvokeExpression(new TsIdentifier("f"), new TsLiteralExpression(1))
			.AddTypeArgument(TsType.String)
			.AddTypeArgument(TsType.Boolean);
		Assert.AreEqual("f<string, boolean>(1)", call.Render());
	}

	[Test]
	public void InvokeExpression_WritesTypedCallWithTypeArgumentsOnly()
	{
		var call = new TsInvokeExpression(new TsIdentifier("f"))
			.AddTypeArgument(TsType.Number);
		Assert.AreEqual("f<number>()", call.Render());
	}

	[Test]
	public void ObjectLiteralExpression_WritesMembers()
	{
		var obj = new TsObjectLiteralExpression()
			.AddMember(new TsIdentifier("a"), new TsLiteralExpression(1))
			.AddMember(new TsIdentifier("b"), new TsLiteralExpression("two"));
		const string expected = "{\n" +
		                        "  a: 1,\n" +
		                        "  b: \"two\"\n" +
		                        "}";
		Assert.AreEqual(expected, obj.Render());
	}

	[Test]
	public void EmptyObjectLiteralExpression_WritesEmptyBraces()
	{
		Assert.AreEqual("{}", new TsObjectLiteralExpression().Render());
	}

	[Test]
	public void ObjectLiteralExpression_AddMemberOverload_WorksWithMember()
	{
		var member = new TsObjectLiteralMember(new TsIdentifier("c"), new TsLiteralExpression(true));
		var obj = new TsObjectLiteralExpression().AddMember(member);
		const string expected = "{\n" +
		                        "  c: true\n" +
		                        "}";
		Assert.AreEqual(expected, obj.Render());
	}

	[Test]
	public void ObjectLiteralExpression_AddMemberOverload_WritesShorthandMember()
	{
		var member = new TsObjectLiteralMember(new TsIdentifier("c"), new TsIdentifier("c"));
		var obj = new TsObjectLiteralExpression().AddMember(member);
		const string expected = "{\n" +
		                        "  c\n" +
		                        "}";
		Assert.AreEqual(expected, obj.Render());
	}

	[Test]
	public void ObjectLiteralExpression_AddMemberOverload_WritesLiteralKeyMember()
	{
		var member = new TsObjectLiteralMember(new TsLiteralExpression("c"), new TsLiteralExpression("c"));
		var obj = new TsObjectLiteralExpression().AddMember(member);
		const string expected = "{\n" +
		                        "  \"c\": \"c\"\n" +
		                        "}";
		Assert.AreEqual(expected, obj.Render());
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

	[Test]
	public void ArrowExpression_WritesArrowExpression()
	{
		var single = new TsArrowExpression(new TsFunctionParameter("x", TsType.Number), new TsIdentifier("y"));
		var writer1 = new TsCodeWriter();
		single.Write(writer1);
		const string expected1 = "(x: number) => y";
		Assert.AreEqual(expected1, writer1.ToString());
		Assert.AreEqual(expected1, single.Render());

		var multiple = new TsArrowExpression(
			new[] { new TsFunctionParameter("a", TsType.String), new TsFunctionParameter("b", TsType.Boolean) },
			new TsLiteralExpression(42));
		var writer2 = new TsCodeWriter();
		multiple.Write(writer2);
		const string expected2 = "(a: string, b: boolean) => 42";
		Assert.AreEqual(expected2, writer2.ToString());
		Assert.AreEqual(expected2, multiple.Render());
	}

	[Test]
	public void TemplateSpan_WritesPlaceholderAndTail()
	{
		var span = new TsTemplateSpan(new TsLiteralExpression(123), "tail");
		var writer = new TsCodeWriter();
		span.Write(writer);
		const string expected = "${123}tail";
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, span.Render());
	}

	[Test]
	public void TemplateLiteralExpression_WritesTemplateLiteral()
	{
		var spans = new[]
		{
			new TsTemplateSpan(new TsIdentifier("a"), "A"), new TsTemplateSpan(new TsLiteralExpression(1), "")
		};
		var expr = new TsTemplateLiteralExpression("head", spans);
		var writer = new TsCodeWriter();
		expr.Write(writer);
		const string expected = "`head${a}A${1}`";
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, expr.Render());
	}
}
