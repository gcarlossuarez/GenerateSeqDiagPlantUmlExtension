using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalyzeCode.MoreComplex.Token
{
    public class NoteOverTokenType
    {
        public string Left { get; private set; }
        public string Operator { get; private set; }
        public string Right { get; private set; }

        public List<string> AttributeList { get; private set; }

        public NoteOverTokenType(string left, string op, string right)
        {
            Left = left;
            Operator = op;
            Right = right;
            AttributeList = new List<string>();
        }

        public NoteOverTokenType(string left, string op, string right, List<string> attributeList)
        {
            Left = left;
            Operator = op;
            Right = right;
            AttributeList = attributeList;
        }
    }
}
