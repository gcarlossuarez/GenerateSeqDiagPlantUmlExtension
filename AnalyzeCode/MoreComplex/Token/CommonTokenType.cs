using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalyzeCode.MoreComplex.Token
{
    public class CommonTokenType
    {
        public string Caller { get; private set; }

        public string Called { get; private set; }

        public string Arrow { get; private set; }

        public string Invocated { get; private set; }

        public List<string> AttributeList { get; private set; }

        public CommonTokenType(string caller, string called, string arrow, string invocated)
        {
            Caller = caller;
            Called = called;
            Arrow = arrow;
            AttributeList = new List<string>();
            Invocated = invocated;
        }

        public CommonTokenType(string caller, string called, string arrow, string invocated, List<string> attributeList)
        {
            Caller = caller;
            Called = called;
            Arrow = arrow;
            Invocated = invocated;
            AttributeList = attributeList;
        }
    }
}
