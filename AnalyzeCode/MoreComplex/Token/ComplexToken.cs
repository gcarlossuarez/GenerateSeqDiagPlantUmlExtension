using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace AnalyzeCode.MoreComplex.Token
{
    public class ComplexToken : AbstractToken
    {
        public CommonTokenType BaseTokenType;

        public ComplexToken()
        {

        }

        public ComplexToken(CommonTokenType baseTokenType)
        {
            BaseTokenType = baseTokenType;
        }
    }
}
