namespace CSharpSandbox.Parser
{
    public class RuleSegment : IRule
    {
        public IReadOnlyList<IRule> Rules { get; }

        public Operator Operator { get; }

        protected RuleSegment(Operator oper, params IRule[] rules)
        {
            Operator = oper;
            Rules = rules;
        }

        public static RuleSegment And(params IRule[] rules) => new(Operator.And, rules);

        public static RuleSegment Or(params IRule[] rules) => new(Operator.Or, rules);

        public static RuleSegment Not(IRule rule) => new(Operator.Not, rule);

        public static RuleSegment Option(IRule rule) => new(Operator.Option, rule);

        public static RuleSegment RepeatRange(IRule rule, int? minimum = null, int? maximum = null) => new RepeatRule(rule, minimum, maximum);

        public static RuleSegment Repeat0(IRule rule) => RepeatRange(rule, 0);

        public static RuleSegment Repeat1(IRule rule) => RepeatRange(rule, 1);
    }

    internal class RepeatRule : RuleSegment
    {
        public int? Minimum { get; }

        public int? Maximum { get; }

        public RepeatRule(IRule rule, int? minimum, int? maximum)
            : base(Operator.Repeat, rule)
        {
            Minimum = minimum;
            Maximum = maximum;
        }
    }
}