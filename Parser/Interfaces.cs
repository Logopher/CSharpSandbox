namespace CSharpSandbox.Parser;

public interface IParser
{
    Type ResultType { get; }

    string RootName { get; }

    INamedRule DefineLiteral(string name, string literal);

    INamedRule DefinePattern(string name, string pattern);

    INamedRule DefineRule(string name, string value);

    INamedRule DefineRule(string name, RuleSegment segment);

    INamedRule GetRule(string name);
}

public interface IMetaParser : IParser
{
    IRule ParseRule(IParser parser, string ruleName, string rule);
}

public enum Operator
{
    And,
    Or,
    Not,
    Option,
    Repeat,
}

public interface IRule
{
}

public interface INamedRule : IRule
{
    string Name { get; }
}

public interface IParseNode
{
    IRule Rule { get; }
}