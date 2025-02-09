using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalyzeCode.MoreComplex
{
    internal class SwitchStatementInfo
    {
        /// <summary>
        /// The expression of the switch statement.
        /// </summary>
        public string Expression { get; set; }

        /// <summary>
        /// Has the value true, if it is the first case of the switch statement
        /// </summary>
        public bool FirstCaseStatement { get; set; }
    }
}
