﻿namespace CSharpSandbox.Parser;

public interface IParser
{
    string RootName { get; }

    INamedRule DefineLiteral(string name, string literal);

    INamedRule DefinePattern(string name, string pattern);

    INamedRule DefineRule(string name, string value);

    INamedRule DefineRule(string name, RuleSegment segment);

    INamedRule? GetRule(string name);
}

public interface IParser<TResult> : IParser
{
}

public interface IMetaParser : IParser
{
    IRule ParseRule(IParser parser, string ruleName, string rule);
}

internal interface IMetaParser<TParser, TResult> : IMetaParser
    where TParser : Parser<TResult>, new()
{
    void ParseToken(TParser parser, IParseNode node);
    void ParseRule(TParser parser, IParseNode node);
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