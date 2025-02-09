using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcesarReglasOrg
{
    class BloqueCod_error
    {
        public const string COD_ERROR = "COD_ERROR=";
        public const string ERROR_ORG_EXCLUYENTE = "R_ORG_E=";
        public const string ERROR_ORG_NO_EXCLUYENTE = "R_ORG_NE=";
        public const string ERROR_ORG_INDEFINIDO = "R_ORG=";
        public const string VALOR_ERROR_INDEFINIDO = "?";

        public BloqueCod_error()
        {
            PosIniBloqueCodError = 0;
            PosFinBloqueCodError = 0;

            PosIniCodError = 0;
            ErrorCode = string.Empty;
            
            DescripAfipRule = string.Empty;
            ValorAfipRule = string.Empty;
        }

        public int PosIniBloqueCodError { get; set; }
        public int PosFinBloqueCodError { get; set; }


        public string DescripBloqueCodErrorRule { get; set; }

        public int PosIniCodError { get; set; }

        public string ErrorCode { get; set; }


        public string DescripAfipRule { get; set; }
        public string ValorAfipRule { get; set; }
    }
}
