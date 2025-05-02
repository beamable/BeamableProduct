using NUnit.Framework;
using cli.Services.Web.CodeGen;

namespace tests.Web.CodeGen;

[TestFixture]
public class TsCommentTests
{
	[Test]
	public void SingleLineComment_WritesDoubleSlash()
	{
		const string expected = "// text\n";
		var comment = new TsComment("text");
		var writer = new TsCodeWriter();
		comment.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, comment.Render());
	}

	[Test]
	public void MultiLineComment_WritesBlockComment()
	{
		const string expected = "/*\n" +
		                        " * a\n" +
		                        " * b\n" +
		                        " */\n";
		var comment = new TsComment("a\nb", TsCommentStyle.MultiLine);
		var writer = new TsCodeWriter();
		comment.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, comment.Render());
	}

	[Test]
	public void DocComment_WritesJSDocComment()
	{
		const string expected = "/**\n" +
		                        " * doc\n" +
		                        " */\n";
		var comment = new TsComment("doc", TsCommentStyle.Doc);
		var writer = new TsCodeWriter();
		comment.Write(writer);
		Assert.AreEqual(expected, writer.ToString());
		Assert.AreEqual(expected, comment.Render());
	}
}
