using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Parsing
{
    public class ParseError
    {
        public IParseNode? NearbyNode { get; }
        public int Position { get; }
        public IRule FailedRule { get; }

        public ParseError(IParseNode? recentNode, int position, IRule currentRule)
        {
            NearbyNode = recentNode;
            Position = position;
            FailedRule = currentRule;
        }
    }
}
