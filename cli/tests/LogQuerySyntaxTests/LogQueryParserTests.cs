using System;
using Beamable.Server.LogQuerySyntax;
using NUnit.Framework;

namespace tests.LogQuerySyntaxTests;

public class LogQueryParserTests
{
    [TestCase("service:tuna", "TEXT [0:7]\nCOLON [7:1]\nTEXT [8:4]\n")]
    [TestCase("*:*tuna", "WILDCARD [0:1]\nCOLON [1:1]\nWILDCARD [2:1]\nTEXT [3:4]")]
    
    // white-space handling
    [TestCase("service:tuna   +", "TEXT [0:7]\nCOLON [7:1]\nTEXT [8:4]\nWHITE_SPACE [12:3]\nOP_PLUS [15:1]")]
    [TestCase("service:tuna -a:b", "TEXT [0:7]\nCOLON [7:1]\nTEXT [8:4]\nWHITE_SPACE [12:1]\nOP_MINUS [13:1]\nTEXT [14:1]\nCOLON [15:1]\nTEXT [16:1]")]
    [TestCase(" a:b ", "WHITE_SPACE [0:1]\nTEXT [1:1]\nCOLON [2:1]\nTEXT [3:1]\nWHITE_SPACE [4:1]")]
    [TestCase(" \ta:b ", "WHITE_SPACE [0:2]\nTEXT [2:1]\nCOLON [3:1]\nTEXT [4:1]\nWHITE_SPACE [5:1]")]
    [TestCase("  \n\ta ", "WHITE_SPACE [0:4]\nTEXT [4:1]\nWHITE_SPACE [5:1]")]
    
    // escaping will be handled at the parser level
    [TestCase("\\-bc", "BACK_SLASH [0:1]\nOP_MINUS [1:1]\nTEXT [2:2]")]
    [TestCase("to\\+st", "TEXT [0:2]\nBACK_SLASH [2:1]\nOP_PLUS [3:1]\nTEXT [4:2]")]
    [TestCase("to\\+", "TEXT [0:2]\nBACK_SLASH [2:1]\nOP_PLUS [3:1]")]
    
    // need to be able to escape the escape-char
    [TestCase("to\\\\a", "TEXT [0:2]\nBACK_SLASH [2:1]\nBACK_SLASH [3:1]\nTEXT [4:1]")]
    [TestCase("to\\\\", "TEXT [0:2]\nBACK_SLASH [2:1]\nBACK_SLASH [3:1]")]
    
    // quotes
    [TestCase("\"hello", "QUOTE [0:1]\nTEXT [1:5]")]
    [TestCase("\\hello", "BACK_SLASH [0:1]\nTEXT [1:5]")]
    [TestCase("\\\"hello", "BACK_SLASH [0:1]\nQUOTE [1:1]\nTEXT [2:5]")]
    
    public void TokenizeDumpTests(string query, string expectedDump)
    {
        // I constructed these test cases by reviewing failed tests and copying the dumps
        //  once I was satisified they were correct. I did not write these dumps by hand, 
        //  and I hope neither do you!
        ReadOnlySpan<char> querySpan = query;

        var collection = LogQueryParser.Tokenize(querySpan);
        var dumped = collection.Dump();
        
        Assert.AreEqual(expectedDump.Trim(), dumped.Trim(), $"'{query}' actually tokenizes to \n{dumped}");
    }

    [TestCase("hello", "lit (hello)")]
    [TestCase("\"hello world\"", "lit (hello world)")]
    [TestCase("\"hello*\"", "lit (hello*)")]
    [TestCase("\"hello+\"", "lit (hello+)")]
    [TestCase("\"hello \\\" \"", "lit (hello \\\" )")]
    [TestCase("\"hello +\"", "lit (hello +)")]
    [TestCase("*", "wild")]
    [TestCase("*hello", "comp (wild, lit (hello))")]
    [TestCase("*hello*", "comp (wild, lit (hello), wild)")]
    [TestCase("*hello*\"toast\"", "comp (wild, lit (hello), wild, lit (toast))")]
    public void ParseValue(string queryText, string expected)
    {
        ReadOnlySpan<char> query = queryText;
        var tokens = LogQueryParser.Tokenize(ref query);
        var value = LogQueryParser.ParseValue(tokens, ref query);
        var dbg = value.DebugRender();
        
        Assert.AreEqual(expected, dbg, $"was actually\n{dbg}");
    }

    [TestCase("*hello or world*", "op (comp (wild, lit (hello)) OR comp (lit (world), wild))")]
    [TestCase("hello", "op (lit (hello))")]
    [TestCase("(hello)", "op (op (lit (hello)))")]
    [TestCase("((hello))", "op (op (op (lit (hello))))")]
    [TestCase("(((hello)))", "op (op (op (op (lit (hello)))))")]
    [TestCase("hello or toast", "op (lit (hello) OR lit (toast))")]
    [TestCase("hello and", "op (lit (hello))")]
    [TestCase("(a or (b and c)) or d", "op (op (lit (a) OR op (lit (b) AND lit (c))) OR lit (d))")]
    [TestCase("a or (b and c) or d", "op (lit (a) OR op (op (lit (b) AND lit (c)) OR lit (d)))")]
    
    [TestCase("goodbye or cruel and world", "op (lit (goodbye) OR op (lit (cruel) AND lit (world)))")]
    [TestCase("goodbye or (cruel) and world", "op (lit (goodbye) OR op (op (lit (cruel)) AND lit (world)))")]
    [TestCase("goodbye or (cruel and world)", "op (lit (goodbye) OR op (lit (cruel) AND lit (world)))")]
    [TestCase("(goodbye or cruel) and world", "op (op (lit (goodbye) OR lit (cruel)) AND lit (world))")]
    
    [TestCase("(\"tuna ) fish\" or salad) and \"bean ( soup\"", "op (op (lit (tuna ) fish) OR lit (salad)) AND lit (bean ( soup))")]
    public void ParseOperation(string queryText, string expected)
    {
        ReadOnlySpan<char> query = queryText;
        var tokens = LogQueryParser.Tokenize(ref query);
        var value = LogQueryParser.ParseOperation(tokens, ref query);
        var dbg = value.DebugRender();

        var errors = value.GetAllErrors();
        Assert.That(errors.Count, Is.Zero, $"parse errors, \n {string.Join(" \n", errors)}");
        
        Assert.AreEqual(expected, dbg, $"was actually\n{dbg}");
    }
    
    [TestCase("service:tuna", "phrase (lit (service) : op (lit (tuna)))")]
    [TestCase("service:tuna and b:c", "op (comp (wild, lit (hello)) OR comp (lit (world), wild))")]
    public void ParsePhrase(string queryText, string expected)
    {
        ReadOnlySpan<char> query = queryText;
        var tokens = LogQueryParser.Tokenize(ref query);
        var value = LogQueryParser.ParsePhrase(tokens, ref query);
        var dbg = value.DebugRender();

        var errors = value.GetAllErrors();
        Assert.That(errors.Count, Is.Zero, $"parse errors, \n {string.Join(" \n", errors)}");
        
        Assert.AreEqual(expected, dbg, $"was actually\n{dbg}");
    }
}