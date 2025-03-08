using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalyzeCode.MoreComplex.Token
{
    public class SymplexToken : AbstractToken
    {
        public NoteOverTokenType NoteOverTokenType;

        public SymplexToken()
        {
        }

        public SymplexToken(NoteOverTokenType noteOverTokenType)
        {
            NoteOverTokenType = noteOverTokenType;
        }
    }
}
