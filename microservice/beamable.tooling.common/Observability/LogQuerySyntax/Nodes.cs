using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Beamable.Server.LogQuerySyntax;


public interface ILogNode
{
    public LogQueryToken StartToken { get; }
    public LogQueryToken EndToken { get; }
    public List<string> Errors { get; }
    void DebugRender(StringBuilder sb);
    IEnumerable<ILogNode> EnumerateChildren();
}

[DebuggerDisplay("[{from.charPosition}:{to.charPosition}] - {messages[0]}")]
public struct LogError
{
    public List<string> messages;
    public LogQueryToken from, to;

    public override string ToString()
    {
        return $"[{from.charPosition}:{to.charPosition}] - {String.Join(", ", messages)}";
    }
}

public static class ILogNodeExtensions
{
    public static List<LogError> GetAllErrors(this ILogNode node)
    {
        var toExplore = new Queue<ILogNode>();
        toExplore.Enqueue(node);
        var errors = new List<LogError>();

        while (toExplore.Count > 0)
        {
            var current = toExplore.Dequeue();

            if (current.Errors.Count > 0)
            {
                errors.Add(new LogError
                {
                    messages = current.Errors,
                    from = current.StartToken,
                    to = current.EndToken
                });
            }

            foreach (var child in current.EnumerateChildren())
            {
                toExplore.Enqueue(child);
            }
        }

        return errors;
    }
    
    public static string DebugRender(this ILogNode node)
    {
        var sb = new StringBuilder();
        node.DebugRender(sb);
        return sb.ToString();
    }
}

public abstract class BaseLogNode : ILogNode
{
    public List<string> Errors { get; set; } = new List<string>();
    public LogQueryToken StartToken { get; set; } 
    public LogQueryToken EndToken { get; set; } 

    public BaseLogNode(){}
    public BaseLogNode(LogQueryToken start, LogQueryToken end)
    {
        StartToken = start;
        EndToken = end;
    }
    
    public abstract void DebugRender(StringBuilder sb);
    public abstract IEnumerable<ILogNode> EnumerateChildren();
}

public class LogQuery : BaseLogNode
{
    public LogQueryOperation root;

    public override void DebugRender(StringBuilder sb)
    {
        root.DebugRender(sb);
    }

    public override IEnumerable<ILogNode> EnumerateChildren()
    {
        yield return root;
    }
}

public interface IQueryPhrase : ILogNode
{
    
}


public class LogQueryOperation : BaseLogNode, IQueryValue
{
    public IQueryValue Left;
    public IQueryValue Right;
    public LogQueryOperationType Operation;

    public bool IsUnary => Right is NoopTextValue;
    
    public override void DebugRender(StringBuilder sb)
    {
        sb.Append("op (");
        Left.DebugRender(sb);
        if (IsUnary)
        {
            sb.Append(")");
            return;
        }
        
        sb.Append($" {Operation} ");
        Right.DebugRender(sb);
        sb.Append(")");
    }

    public override IEnumerable<ILogNode> EnumerateChildren()
    {
        yield return Left;
        yield return Right;
    }
}

public enum LogQueryOperationType
{
    AND, OR
}

public class NoopQueryPhrase : BaseLogNode, IQueryPhrase
{
    public override void DebugRender(StringBuilder sb)
    {
        sb.Append("noop");
    }

    public override IEnumerable<ILogNode> EnumerateChildren()
    {
        yield break;
    }
}

public class LogQueryPhrase : BaseLogNode, IQueryPhrase
{
    public IQueryValue Left;
    public IQueryValue Right;
    
    public override void DebugRender(StringBuilder sb)
    {
        sb.Append("phrase (");
        Left.DebugRender(sb);
        sb.Append(" : ");
        Right.DebugRender(sb);
        sb.Append(")");
    }

    public override IEnumerable<ILogNode> EnumerateChildren()
    {
        yield return Left;
        yield return Right;
    }
}

public interface IQueryType : ILogNode
{
    
}

public class QueryType : BaseLogNode, IQueryType
{
    public LiteralTextValue content;
    public override void DebugRender(StringBuilder sb)
    {
        sb.Append("type (");
        content.DebugRender(sb);
        sb.Append(")");
    }

    public override IEnumerable<ILogNode> EnumerateChildren()
    {
        yield return content;
    }
}

public interface IQueryValue : ILogNode
{
    
}

public class CompoundTextValue : BaseLogNode, IQueryValue
{
    public List<IQueryValue> Values { get; }

    public CompoundTextValue(List<IQueryValue> values)
        :base(values[0].StartToken, values[values.Count-1].EndToken)
    {
        Values = values;
    }

    public override void DebugRender(StringBuilder sb)
    {
        sb.Append("comp (");
        for (var i = 0; i < Values.Count; i++)
        {
            Values[i].DebugRender(sb);
            if (i < Values.Count - 1)
            {
                sb.Append(", ");
            }
        }

        sb.Append(")");
    }

    public override IEnumerable<ILogNode> EnumerateChildren()
    {
        foreach (var v in Values)
        {
            yield return v;
        }
    }
}

public class WildcardTextValue : BaseLogNode, IQueryValue
{
    
    public WildcardTextValue(
        LogQueryToken start, string errorMessage=null) 
        : base(start, start)
    {
        if (!string.IsNullOrEmpty(errorMessage))
        {
            Errors.Add(errorMessage);
        }
    }
    
    public override IEnumerable<ILogNode> EnumerateChildren()
    {
        yield break;
    }

    public override void DebugRender(StringBuilder sb)
    {
        sb.Append($"wild");
    }
}


public class NoopTextValue : BaseLogNode, IQueryValue
{
    
    public NoopTextValue(
        LogQueryToken start, string errorMessage=null) 
        : base(start, start)
    {
        if (!string.IsNullOrEmpty(errorMessage))
        {
            Errors.Add(errorMessage);
        }
    }
    
    public override IEnumerable<ILogNode> EnumerateChildren()
    {
        yield break;
    }

    public override void DebugRender(StringBuilder sb)
    {
        sb.Append("noop");
    }
}

public class LiteralTextValue : BaseLogNode, IQueryValue
{
    public string text;

    public LiteralTextValue(
        LogQueryToken start, 
        LogQueryToken end,
        string text, string errorMessage=null) 
        : base(start, end)
    {
        this.text = text;
        if (!string.IsNullOrEmpty(errorMessage))
        {
            Errors.Add(errorMessage);
        }
    }
    
    public override IEnumerable<ILogNode> EnumerateChildren()
    {
        yield break;
    }

    public override void DebugRender(StringBuilder sb)
    {
        sb.Append($"lit ({text})");
    }
}
