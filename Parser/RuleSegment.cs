using CSharpSandbox.Common;

namespace CSharpSandbox.Parsing;

public class RuleSegment : IRule
{
    internal readonly IParser _parser;

    public IReadOnlyList<IRule> Rules { get; }

    public Operator Operator { get; }

    internal RuleSegment(IParser parser, Operator oper, params IRule[] rules)
    {
        _parser = parser;

        Operator = oper;
        Rules = rules;
    }
    public static RuleSegment And(IParser parser, params IRule[] rules) => new(parser, Operator.And, rules);

    public static RuleSegment Or(IParser parser, params IRule[] rules) => new(parser, Operator.Or, rules);

    public static RuleSegment Not(IParser parser, IRule rule) => new(parser, Operator.Not, rule);

    public static RuleSegment Option(IParser parser, IRule rule) => new(parser, Operator.Option, rule);

    public static RuleSegment RepeatRange(IParser parser, IRule rule, int? minimum = null, int? maximum = null) => new RepeatRule(parser, rule, minimum, maximum);

    public static RuleSegment Repeat0(IParser parser, IRule rule) => RepeatRange(parser, rule, 0);

    public static RuleSegment Repeat1(IParser parser, IRule rule) => RepeatRange(parser, rule, 1);

    public override string ToString()
    {
        switch (Operator)
        {
            case Operator.And:
                return string.Join(" ", Rules);
            case Operator.Or:
                return string.Join(" | ", Rules);
            case Operator.Not:
                return $"!{Rules.Single()}";
            case Operator.Option:
                return $"{Rules.Single()}?";
            case Operator.Repeat:
                throw new Exception();
            default:
                throw new Exception();
        };
    }

    public virtual string ToString(IParseNode node)
    {
        var pnode = (ParseNode)node;

        if (pnode.Children.Count != Rules.Count)
        {
            throw new Exception();
        }

        switch (Operator)
        {
            case Operator.And:
                return $"({string.Join(Mundane.EmptyString, Rules.Zip(pnode.Children, (r, n) => r.ToString(n)))})";
            case Operator.Or:
                return string.Join(" | ", Rules.Zip(pnode.Children, (r, n) => r.ToString(n)));
            case Operator.Not:
                return $"!{Rules.Single().ToString(pnode.Children.Single())}";
            case Operator.Option:
                return $"{Rules.Single().ToString(pnode.Children.Single())}?";
            case Operator.Repeat:
                throw new Exception();
            default:
                throw new Exception();
        };
    }
}

internal class RepeatRule : RuleSegment
{
    public int? Minimum { get; }

    public int? Maximum { get; }

    public RepeatRule(IParser parser, IRule rule, int? minimum, int? maximum)
        : base(parser, Operator.Repeat, rule)
    {
        Minimum = minimum;
        Maximum = maximum;
    }

    public override string ToString()
    {
        var rule = Rules.Single();

        if (Maximum == null)
        {
            switch (Minimum)
            {
                case null:
                case 0:
                    return $"{Rules.Single()}*";
                case 1:
                    return $"{Rules.Single()}+";
            }
        }

        var min = Minimum != null ? Minimum.ToString() : Mundane.EmptyString;
        var max = Maximum != null ? Maximum.ToString() : Mundane.EmptyString;

        return $"{rule}{{{min},{max}}}";
    }

    public override string ToString(IParseNode node)
    {
        var pnode = (ParseNode)node;

        if (pnode.Children.Count != Rules.Count)
        {
            throw new Exception();
        }

        var rule = Rules.Single();
        var child = pnode.Children.Single();
        var baseString = rule.ToString(child);

        if (Maximum == null)
        {
            switch (Minimum)
            {
                case null:
                case 0:
                    return $"{baseString}*";
                case 1:
                    return $"{baseString}+";
            }
        }

        var min = Minimum != null ? Minimum.ToString() : Mundane.EmptyString;
        var max = Maximum != null ? Maximum.ToString() : Mundane.EmptyString;

        return $"{baseString}{{{min},{max}}}";
    }
}