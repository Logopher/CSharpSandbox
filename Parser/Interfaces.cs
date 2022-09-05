using Microsoft.Extensions.Logging;

namespace CSharpSandbox.Parsing;

public interface IParser
{
    Type ResultType { get; }

    string RootName { get; }

    INamedRule DefineLiteral(string name, string literal);

    INamedRule DefinePattern(string name, string pattern);

    INamedRule DefineRule(string name, string value);

    INamedRule DefineRule(string name, RuleSegment segment);

    INamedRule GetRule(string name);

    string ToString(INamedRule rule, IParseNode node);
}

public interface IMetaParser : IParser
{
    IRule ParseRule(IParser parser, string ruleName, string rule);
}

internal interface IMetaParser_internal : IMetaParser
{
    ILogger GetLogger();
}

public enum Operator
{
    And,
    Or,
    Not,
    Option,
    Repeat,
}

public enum NodeType
{
    Segment,
    Token,
    Rule,
}

public interface IRule
{
    string ToString();

    string ToString(IParseNode parseNode);
}

public interface INamedRule : IRule
{
    string Name { get; }
}

public interface IParseNode
{
    NodeType NodeType { get; }

    IRule Rule { get; }

    string ToString();
}