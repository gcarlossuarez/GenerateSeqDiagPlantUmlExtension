using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Traductor
{
    class Sentencia
    {
        public string DescripciónBloque { get; set; } = string.Empty;
        public string ErrorNatura { get; set; } = string.Empty;

        public string ValorErrorNatura { get; set; } = string.Empty;

        public string TipoErroOrg { get; set; } = string.Empty;

        public string ErrorOrg { get; set; } = string.Empty;

        public string ValorErrorAfip { get; set; } = string.Empty;

        public int PosFinSentencia { get; set; } = int.MinValue;
    }
}
