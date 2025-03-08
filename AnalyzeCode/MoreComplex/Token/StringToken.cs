using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalyzeCode.MoreComplex.Token
{
    public class StringToken : AbstractToken
    {
        private string _value;

        public StringToken()
        {

        }
        public StringToken(string value)
        {
            _value = value;
        }

        
        public override string ToString()
        {
            return _value;
        }
    }
}
