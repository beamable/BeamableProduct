using System.Diagnostics;
using System.Text;

namespace Beamable.Server.LogQuerySyntax;

public enum LogQueryLexem
{
    EOF,
    WHITE_SPACE,
    QUOTE, 
    COLON, 
    COMMA, 
    BACK_SLASH,
    FIELD_SEPARATOR,
    TEXT,
    WILDCARD,
    PAREN_OPEN,
    PAREN_CLOSE,
    OP_MINUS,
    OP_PLUS,
    OP_AND,
    OP_OR,
}


[DebuggerDisplay("{lexem} [{charPosition}:{length}]")]
public struct LogQueryToken
{
    public LogQueryLexem lexem;
    public int charPosition;
    public int length;

    public ReadOnlySpan<char> Render(ref ReadOnlySpan<char> fullText)
    {
        return fullText.Slice(charPosition, length);
    }
    
    public ReadOnlySpan<char> RenderBetween(ref LogQueryToken end, ref ReadOnlySpan<char> fullText)
    {
        var startPosition = charPosition + length;
        var endPosition = end.charPosition;
        return fullText.Slice(startPosition, endPosition - startPosition);
    }
    
    public override string ToString()
    {
        return $"{lexem} [{charPosition}:{length}]";
    }
}

public class LogQueryTokenCollection
{
    public List<LogQueryToken> Tokens;
    
    public int Index { get; set; }

    public LogQueryToken Current => !IsEnd ? Tokens[Index] : new LogQueryToken
    {
        lexem = LogQueryLexem.EOF
    };

    public LogQueryToken Advance()
    {
        var c = Current;
        if (!IsEnd)
        {
            Index++;
        }

        return c;
    }

    public bool IsEnd => Index >= Tokens.Count;
}

public static class LogQueryParser
{
    public static LogQuery Parse(LogQueryTokenCollection tokens, ref ReadOnlySpan<char> logQuery)
    {
        // var start = tokens.Advance();

        // var op = ParseOperation(tokens, ref logQuery, out _);

        throw new NotImplementedException();
        // return new LogQuery
        // {
        //     root = op
        // };
    }

    public static void SkipWhiteSpace(LogQueryTokenCollection tokens)
    {
        while (tokens.Current.lexem == LogQueryLexem.WHITE_SPACE)
        {
            tokens.Advance();
        }
    }

    public static bool TryParseOperation(LogQueryTokenCollection tokens, ref ReadOnlySpan<char> logQuery, out LogQueryOperationType op)
    {
        op = LogQueryOperationType.OR;
        while (true)
        {
            var current = tokens.Current;
            switch (current.lexem)
            {
                case LogQueryLexem.WHITE_SPACE:
                    // allow the parser to skip through white-spice while looking for an operation.
                    tokens.Advance();
                    break;
                case LogQueryLexem.OP_OR:
                    op = LogQueryOperationType.OR;
                    tokens.Advance();
                    return true;
                case LogQueryLexem.OP_AND:
                    op = LogQueryOperationType.AND;
                    tokens.Advance();
                    return true;
                default:
                    return false;
            }
        }
    }
    
    public static LogQueryOperation ParseOperation(LogQueryTokenCollection tokens, ref ReadOnlySpan<char> logQuery)
    {
        SkipWhiteSpace(tokens);
        
        // the left side MUST exist.
        var left = ParseValue(tokens, ref logQuery);
     
        SkipWhiteSpace(tokens);
        if (!TryParseOperation(tokens, ref logQuery, out var opType))
        {
            opType = LogQueryOperationType.AND;
        }
        SkipWhiteSpace(tokens);

        // the right side MAY exist
        switch (tokens.Current.lexem)
        {
            case LogQueryLexem.PAREN_CLOSE:
            case LogQueryLexem.EOF:
                
                return new LogQueryOperation
                {
                    Left = left,
                    Right = new NoopTextValue(tokens.Current),
                    Operation = opType
                };
                break;
            
            default:
                var right = ParseOperation(tokens, ref logQuery);

                if (right.IsUnary)
                {
                    return new LogQueryOperation
                    {
                        Left = left,
                        Right = right.Left,
                        Operation = opType
                    };
                }
                
                return new LogQueryOperation
                {
                    Left = left,
                    Right = right,
                    Operation = opType
                };
        }

    }

    public static IQueryPhrase ParsePhrase(LogQueryTokenCollection tokens, ref ReadOnlySpan<char> logQuery)
    {
        var left = ParseValue(tokens, ref logQuery);

        switch (tokens.Current.lexem)
        {
            case LogQueryLexem.COLON:
                tokens.Advance(); // consume

                var right = ParseOperation(tokens, ref logQuery);
                return new LogQueryPhrase
                {
                    Left = left,
                    Right = right
                };
                break;
        }

        return new LogQueryPhrase
        {
            Left = new NoopTextValue(tokens.Current),
            Right = left
        };
    }

    public static IQueryValue ParseValue(LogQueryTokenCollection tokens, ref ReadOnlySpan<char> logQuery)
    {

        switch (tokens.Current.lexem)
        {
            case LogQueryLexem.PAREN_OPEN:
                tokens.Advance();
                var op = ParseOperation(tokens, ref logQuery);

                // expect that a closing paren must follow
                if (tokens.Current.lexem == LogQueryLexem.PAREN_CLOSE)
                {
                    tokens.Advance(); // good!
                }
                else
                {
                    op.Errors.Add("expected a closing paren");
                }
                
                return op;
            default:
                break;
        }
        
        var term = ParseValueTerm(tokens, ref logQuery);

        List<IQueryValue> additionalTerms = null;
        var peeking = true;

        while (peeking)
        {
            switch (tokens.Current.lexem)
            {
                // terms are always specified by these lexems
                case LogQueryLexem.QUOTE:
                case LogQueryLexem.WILDCARD:
                case LogQueryLexem.TEXT:
                    var nextTerm = ParseValueTerm(tokens, ref logQuery);
                    if (additionalTerms == null)
                    {
                        additionalTerms = new List<IQueryValue>();
                    }

                    additionalTerms.Add(nextTerm);
                    break;
                default:
                    peeking = false;
                    break;
            }
        }

        if (additionalTerms != null)
        {
            additionalTerms.Insert(0, term);
            return new CompoundTextValue(additionalTerms);
        }

        return term;
    }

    public static IQueryValue ParseValueTerm(LogQueryTokenCollection tokens, ref ReadOnlySpan<char> logQuery)
    {
        var start = tokens.Advance();
        var foundEnd = false;


        switch (start.lexem)
        {
            case LogQueryLexem.QUOTE:

                // need to search for a closing quote!
                while (true)
                {
                    var next = tokens.Advance();
                    switch (next.lexem)
                    {
                        case LogQueryLexem.BACK_SLASH:
                            var escapedToken = tokens.Advance();
                            switch (escapedToken.lexem)
                            {
                                // TODO: What other characters are allowed to be escaped? 
                                // these characters are allowed to be escaped
                                case LogQueryLexem.QUOTE:
                                    // advance to the next
                                    continue;
                                default:
                                    return new LiteralTextValue(start, escapedToken, "", "invalid escape sequence");
                            }

                            break;

                        // we found the end of the string!
                        case LogQueryLexem.QUOTE:
                            return new LiteralTextValue(start, next,
                                start.RenderBetween(ref next, ref logQuery).ToString());

                        // we didn't find a closing quote!
                        case LogQueryLexem.EOF:
                            return new LiteralTextValue(start, next, "", "expected to find a closing double-quote");

                        // in-between quotes, all characters are valid.
                        default:
                            break;
                    }
                }

                break;

            case LogQueryLexem.WILDCARD:
                return new WildcardTextValue(start);

            case LogQueryLexem.TEXT:
                return new LiteralTextValue(start, start, start.Render(ref logQuery).ToString());


            default:
                return new LiteralTextValue(start, start, "", "expected to find a value");
        }

    }




    public static LogQueryTokenCollection Tokenize(ReadOnlySpan<char> logQuery)
    {
        return Tokenize(ref logQuery);
    }
    
    public static LogQueryTokenCollection Tokenize(ref ReadOnlySpan<char> logQuery)
    {

        LogQueryToken HandlePhrase(int buildingIndex, string phraseString, bool isWhiteSpace)
        {
            switch (phraseString)
            {
                case "or":
                    return new LogQueryToken
                    {
                        charPosition = buildingIndex, length = 2, lexem = LogQueryLexem.OP_OR
                    };
                case "and":
                    return new LogQueryToken
                    {
                        charPosition = buildingIndex, length = 2, lexem = LogQueryLexem.OP_AND
                    };
                default:
                    return new LogQueryToken
                    {
                        charPosition = buildingIndex, length = phraseString.Length, lexem = isWhiteSpace ? LogQueryLexem.WHITE_SPACE : LogQueryLexem.TEXT
                    };
            }


        }
        
        var tokens = new List<LogQueryToken>();
        var buildingIndex = -1;
        var isWhiteSpace = false;
        for (var i = 0; i < logQuery.Length; i++)
        {
            var c = logQuery[i];

            if (buildingIndex != -1)
            {
                
                // a longer phrase is being lexed... 
                //  at this moment, we care for a delimiter to split the phrase. 
                switch (c)
                {
                    case '\\':
                    case '"':
                    case ':':
                    case ',':
                    case '.':
                    case '*':
                    case '+':
                    case '(':
                    case ')':
                    case '-':

                        var phrase = logQuery.Slice(buildingIndex, (i - buildingIndex));
                        var phraseString = phrase.ToString().ToLowerInvariant();
                        tokens.Add(HandlePhrase(buildingIndex, phraseString, isWhiteSpace));
                    
                        buildingIndex = -1;
                        break;
                    default:


                        var isCurrentCharWhiteSpace = char.IsWhiteSpace(c);
                        if (isCurrentCharWhiteSpace != isWhiteSpace)
                        {
                            phrase = logQuery.Slice(buildingIndex, (i - buildingIndex));
                            phraseString = phrase.ToString().ToLowerInvariant();
                            tokens.Add(HandlePhrase(buildingIndex, phraseString, isWhiteSpace));
                    
                            buildingIndex = -1;
                            break;
                        }
                        
                        // the phrase keeps rolling, so don't bother trying to lex anything else.
                        continue;
                }
            }
            
            // handle single length tokens
            switch (c)
            {
                case '\\':
                    tokens.Add(new LogQueryToken
                    {
                        charPosition = i, length = 1, lexem = LogQueryLexem.BACK_SLASH
                    });
                    continue;
                case '"':
                    tokens.Add(new LogQueryToken
                    {
                        charPosition = i, length = 1, lexem = LogQueryLexem.QUOTE
                    });
                    continue;
                case ':':
                    tokens.Add(new LogQueryToken
                    {
                        charPosition = i, length = 1, lexem = LogQueryLexem.COLON
                    });
                    continue;
                case ',':
                    tokens.Add(new LogQueryToken
                    {
                        charPosition = i, length = 1, lexem = LogQueryLexem.COMMA
                    });
                    continue;
                case '(':
                    tokens.Add(new LogQueryToken
                    {
                        charPosition = i, length = 1, lexem = LogQueryLexem.PAREN_OPEN
                    });
                    continue;
                case ')':
                    tokens.Add(new LogQueryToken
                    {
                        charPosition = i, length = 1, lexem = LogQueryLexem.PAREN_CLOSE
                    });
                    continue;
                case '.':
                    tokens.Add(new LogQueryToken
                    {
                        charPosition = i, length = 1, lexem = LogQueryLexem.FIELD_SEPARATOR
                    });
                    continue;
                case '*':
                    tokens.Add(new LogQueryToken
                    {
                        charPosition = i, length = 1, lexem = LogQueryLexem.WILDCARD
                    });
                    continue;
                case '+':
                    tokens.Add(new LogQueryToken
                    {
                        charPosition = i, length = 1, lexem = LogQueryLexem.OP_PLUS
                    });
                    continue;
                case '-':
                    tokens.Add(new LogQueryToken
                    {
                        charPosition = i, length = 1, lexem = LogQueryLexem.OP_MINUS
                    });
                    continue;
            }
            
            // handle substring phrases...
            buildingIndex = i;

            isWhiteSpace = char.IsWhiteSpace(c);

        }

        // clean up a dangling phrase
        if (buildingIndex != -1)
        {
            var phrase = logQuery.Slice(buildingIndex, logQuery.Length - buildingIndex);
            var phraseString = phrase.ToString().ToLowerInvariant();
            tokens.Add(HandlePhrase(buildingIndex, phraseString, isWhiteSpace));
        }

        return new LogQueryTokenCollection
        {
            Tokens = tokens,
        };
    }
}