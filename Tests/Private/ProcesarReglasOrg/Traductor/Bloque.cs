using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Traductor
{
    public class Bloque
    {
        public string DescripciónBloque { get; set; } = string.Empty;
        public string ErrorNatura { get; set; } = string.Empty;

        public string ValorErrorNatura { get; set; } = string.Empty;

        public string TipoErrorAfip { get; set; } = string.Empty;

        public string ErrorAfip { get; set; } = string.Empty;

        public string ValorErrorAfip { get; set; } = string.Empty;

        public Bloque()
        {

        }

        internal Bloque(Sentencia sentencia)
        {
            DescripciónBloque = sentencia.DescripciónBloque;
            ErrorNatura = sentencia.ErrorNatura;
            ValorErrorNatura = sentencia.ValorErrorNatura;

            TipoErrorAfip = sentencia.TipoErroOrg;
            ErrorAfip = sentencia.ErrorOrg;
            ValorErrorAfip = sentencia.ValorErrorAfip;
        }

    }
}
