using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalyzeCode.MoreComplex
{
    internal class StackAsignmentInfo
    {
        public string Caller { get; set; } = string.Empty;
        public Stack<string> StackAsignment { get; set; } = new Stack<string>();
    }
}
