using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalyzeCode.MoreComplex
{
    internal class Autoincrement
    {
        private static long Value { get; set; } = 0;

        public static long GetNextValue()
        {
            if(Value == (long.MaxValue  - 1)) Value = 0;

            return Value++;
        }
    }
}
