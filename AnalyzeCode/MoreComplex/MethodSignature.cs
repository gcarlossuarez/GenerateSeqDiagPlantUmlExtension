using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalyzeCode.MoreComplex
{
    public class MethodSignature
    {
        public string Name { get; }
        public string Parameters { get; }
        public int Line { get; }
        public int Column { get; }

        public MethodSignature(string name, string parameters, int line, int column)
        {
            Name = name;
            Parameters = parameters;
            Line = line;
            Column = column;
        }

        public override bool Equals(object obj)
        {
            if (obj is MethodSignature other)
            {
                return Name == other.Name &&
                       Parameters == other.Parameters &&
                       Line == other.Line &&
                       Column == other.Column;
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked // Allows arithmetic overflow without throwing an exception
            {
                int hash = 17;
                hash = hash * 23 + (Name != null ? Name.GetHashCode() : 0);
                hash = hash * 23 + (Parameters != null ? Parameters.GetHashCode() : 0);
                hash = hash * 23 + Line.GetHashCode();
                hash = hash * 23 + Column.GetHashCode();
                return hash;
            }
        }
    }

}
