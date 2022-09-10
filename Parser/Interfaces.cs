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

    ParseNode? RootNode { get; }

    ParseNode CurrentNode { get; }

    IRule CurrentRule { get; }
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
    NamedRule,
}

public interface IRule
{
    IReadOnlyList<IRule> Rules { get; }

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

    ParseNode? Parent { get; set; }

    IRule Rule { get; }

    string ToString();
}