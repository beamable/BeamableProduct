namespace cli.Services.Web.CodeGen;

/// <summary>                                                                                        
/// Represents a template literal expression, e.g. `head${expression}tail`.                          
/// </summary>                                                                                       
public class TsTemplateLiteralExpression : TsExpression
{
	/// <summary>                                                                                       
	/// The literal text before the first placeholder.                                                  
	/// </summary>                                                                                      
	public string Head { get; }

	/// <summary>                                                                                       
	/// The spans following placeholders in the template literal.                                       
	/// </summary>                                                                                      
	public List<TsTemplateSpan> Spans { get; } = new();

	/// <summary>                                                                                       
	/// Creates a new template literal expression.                                                      
	/// </summary>                                                                                      
	/// <param name="head">The literal text before the first placeholder.</param>                       
	/// <param name="spans">The template spans following placeholders.</param>                          
	public TsTemplateLiteralExpression(string head, params TsTemplateSpan[] spans)
	{
		Head = head;
		Spans.AddRange(spans);
	}

	public override void Write(TsCodeWriter writer)
	{
		writer.Write("`");
		writer.Write(Head);
		foreach (var span in Spans)
		{
			span.Write(writer);
		}

		writer.Write("`");
	}
}

/// <summary>                                                                                        
/// Represents a template literal span in a template literal expression, e.g. `${expression}tail`.   
/// </summary>                                                                                       
public class TsTemplateSpan : TsNode
{
	/// <summary>                                                                                       
	/// The expression inside the placeholder.                                                          
	/// </summary>                                                                                      
	public TsExpression Expression { get; }

	/// <summary>                                                                                       
	/// The literal text that follows the placeholder.                                                  
	/// </summary>                                                                                      
	public string Tail { get; }

	/// <summary>                                                                                       
	/// Creates a new template literal span.                                                            
	/// </summary>                                                                                      
	/// <param name="expression">The expression to interpolate.</param>                                 
	/// <param name="tail">The literal text to follow the interpolation.</param>                        
	public TsTemplateSpan(TsExpression expression, string tail)
	{
		Expression = expression;
		Tail = tail;
	}

	public override void Write(TsCodeWriter writer)
	{
		writer.Write("${");
		Expression.Write(writer);
		writer.Write("}");
		writer.Write(Tail);
	}
}
