namespace CSharpSandbox.Parser
{
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
    }
}