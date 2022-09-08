using Microsoft.Extensions.Logging;

namespace CSharpSandbox.Parsing;

public interface IParser
{
    Type ResultType { get; }

    string RootName { get; }

    Pattern DefineLiteral(string name, string literal);

    Pattern DefinePattern(string name, string pattern);

    NamedRule DefineRule(string name, string value);

    NamedRule DefineRule(string name, RuleSegment segment);

    INamedRule GetRule(string name);

    LazyNamedRule GetLazyRule(string name);

    string ToString(INamedRule rule, IParseNode node);
}

public interface IMetaParser : IParser
{
    NamedRule ParseRuleDefinition(IParser parser, string ruleName, string rule);
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
    Empty,
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