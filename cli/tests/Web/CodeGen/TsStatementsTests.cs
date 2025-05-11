using NUnit.Framework;
using cli.Services.Web.CodeGen;

namespace tests.Web.CodeGen;

[TestFixture]
public class TsStatementsTests
{
	[Test]
	public void BreakStatement_WritesBreak()
	{
		const string expected = "break;\n";
		var stmt = new TsBreakStatement();
		var writer = new TsCodeWriter();
		stmt.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, stmt.Render());
	}

	[Test]
	public void ContinueStatement_WritesContinue()
	{
		const string expected = "continue;\n";
		var stmt = new TsContinueStatement();
		var writer = new TsCodeWriter();
		stmt.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, stmt.Render());
	}

	[Test]
	public void EmptyStatement_WritesSemicolon()
	{
		const string expected = ";\n";
		var stmt = new TsEmptyStatement();
		var writer = new TsCodeWriter();
		stmt.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, stmt.Render());
	}

	[Test]
	public void ReturnStatement_WithExpression_WritesReturnValue()
	{
		const string expected = "return 42;\n";
		var stmt = new TsReturnStatement(new TsLiteralExpression(42));
		var writer = new TsCodeWriter();
		stmt.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, stmt.Render());
	}

	[Test]
	public void ReturnStatement_WithoutExpression_WritesReturnSemicolon()
	{
		const string expected = "return;\n";
		var stmt = new TsReturnStatement();
		var writer = new TsCodeWriter();
		stmt.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, stmt.Render());
	}

	[Test]
	public void ThrowStatement_WritesThrowExpression()
	{
		const string expected = "throw \"error\";\n";
		var stmt = new TsThrowStatement(new TsLiteralExpression("error"));
		var writer = new TsCodeWriter();
		stmt.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, stmt.Render());
	}

	[Test]
	public void ExpressionStatement_WritesExpressionAndSemicolon()
	{
		const string expected = "true;\n";
		var expr = new TsLiteralExpression(true);
		var stmt = new TsExpressionStatement(expr);
		var writer = new TsCodeWriter();
		stmt.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, stmt.Render());
	}

	[Test]
	public void AwaitStatement_WritesAwaitExpression()
	{
		const string expected = "await foo;\n";
		var stmt = new TsAwaitStatement(new TsIdentifier("foo"));
		var writer = new TsCodeWriter();
		stmt.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, stmt.Render());
	}

	[Test]
	public void BinaryExpression_WritesLeftOperatorRight()
	{
		var expr = new TsBinaryExpression(new TsIdentifier("a"), TsBinaryOperatorType.Plus, new TsIdentifier("b"));
		const string expected = "a + b";
		var writer = new TsCodeWriter();
		expr.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, expr.Render());
	}

	[Test]
	public void ConditionalStatement_WritesIfElseBlocks()
	{
		const string expected =
			"if (i == 1) {\n" +
			"  1;\n" +
			"} else if (i == 2) {\n" +
			"  2;\n" +
			"} else if (i == 3) {\n" +
			"  3;\n" +
			"} else {\n" +
			"  0;\n" +
			"}\n";
		var i = new TsIdentifier("i");
		var one = new TsLiteralExpression(1);
		var two = new TsLiteralExpression(2);
		var three = new TsLiteralExpression(3);
		var zero = new TsLiteralExpression(0);
		var stmt = new TsConditionalStatement(new TsBinaryExpression(i, TsBinaryOperatorType.EqualTo, one))
			.AddThen(new TsExpressionStatement(one))
			.AddElseIf(new TsBinaryExpression(i, TsBinaryOperatorType.EqualTo, two),
				new TsExpressionStatement(two))
			.AddElseIf(new TsBinaryExpression(i, TsBinaryOperatorType.EqualTo, three),
				new TsExpressionStatement(three))
			.AddElse(new TsExpressionStatement(zero));
		var writer = new TsCodeWriter();
		stmt.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, stmt.Render());
	}

	[Test]
	public void TryCatchFinallyStatement_WritesBlocks()
	{
		const string expected =
			"try {\n" +
			"  1;\n" +
			"} catch (e) {\n" +
			"  2;\n" +
			"}\n" +
			"} finally {\n" +
			"  3;\n" +
			"}\n";
		var stmt = new TsTryCatchStatement(new TsIdentifier("e"))
			.AddTry(new TsExpressionStatement(new TsLiteralExpression(1)))
			.AddCatch(new TsExpressionStatement(new TsLiteralExpression(2)))
			.AddFinally(new TsExpressionStatement(new TsLiteralExpression(3)));
		var writer = new TsCodeWriter();
		stmt.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, stmt.Render());
	}

	[Test]
	public void SwitchStatement_WritesCasesAndDefault()
	{
		const string expected =
			"switch (x) {\n" +
			"  case 1:\n" +
			"    \"one\";\n" +
			"  default:\n" +
			"    \"default\";\n" +
			"}\n";
		var switchStmt = new TsSwitchStatement(new TsIdentifier("x"))
			.AddCaseClause(new TsSwitchCaseClause(new TsLiteralExpression(1))
				.AddBody(new TsExpressionStatement(new TsLiteralExpression("one"))))
			.AddDefaultClause(new TsSwitchDefaultClause()
				.AddBody(new TsExpressionStatement(new TsLiteralExpression("default"))));
		var writer = new TsCodeWriter();
		switchStmt.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, switchStmt.Render());
	}

	[Test]
	public void ForStatement_WritesForLoop()
	{
		const string expected = "for (let i = 0; i < 3; i++) {\n" +
		                        "  \"loop\";\n" +
		                        "}\n";
		var init = new TsVariable("i")
			.WithInitializer(new TsLiteralExpression(0))
			.AsExpression();
		var cond = new TsBinaryExpression(init.Identifier, TsBinaryOperatorType.LessThan,
			new TsLiteralExpression(3));
		var iter = new TsUnaryExpression(init.Identifier, TsUnaryOperatorType.Increment,
			TsUnaryOperatorPosition.Postfix);
		var forStmt = new TsForStatement(init, cond, iter)
			.AddBody(new TsExpressionStatement(new TsLiteralExpression("loop")));
		var writer = new TsCodeWriter();
		forStmt.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, forStmt.Render());
	}

	[Test]
	public void ForOfStatement_WritesForOfLoop()
	{
		const string expected = "for (item of arr) {\n" +
		                        "  \"it\";\n" +
		                        "}\n";
		var left = new TsIdentifier("item");
		var right = new TsIdentifier("arr");
		var stmt = new TsForOfStatement(left, right)
			.AddBody(new TsExpressionStatement(new TsLiteralExpression("it")));
		var writer = new TsCodeWriter();
		stmt.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, stmt.Render());
	}

	[Test]
	public void ForInStatement_WritesForInLoop()
	{
		const string expected = "for (key in obj) {\n" +
		                        "  \"k\";\n" +
		                        "}\n";
		var left = new TsIdentifier("key");
		var right = new TsIdentifier("obj");
		var stmt = new TsForInStatement(left, right)
			.AddBody(new TsExpressionStatement(new TsLiteralExpression("k")));
		var writer = new TsCodeWriter();
		stmt.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, stmt.Render());
	}

	[Test]
	public void ObjectDestructureStatement_WritesDestructure()
	{
		const string expected =
			"const {\n" +
			"  id,\n" +
			"  createdTimeMillis,\n" +
			"  updatedTimeMillis,\n" +
			"  privilegedAccount,\n" +
			"  external,\n" +
			"  gamerTags,\n" +
			"  thirdParties,\n" +
			"  ...optionals\n" +
			"} = init;\n";
		string[] identifiers =
		{
			"id", "createdTimeMillis", "updatedTimeMillis", "privilegedAccount", "external", "gamerTags",
			"thirdParties"
		};
		var stmt = new TsObjectDestructureStatement(identifiers, new TsIdentifier("init"))
			.WithRest("optionals");
		var writer = new TsCodeWriter();
		stmt.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, stmt.Render());
	}
}
