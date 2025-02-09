using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Traductor
{
    /*
     * S => B
B => d B | d R B | d R RA B | d | d R | d R RA 
B => d RA R B | d RA R
RA => RE RNE | RNE RE | RE | RNE 
R => COD_ERROR B = B NRO
RE => EORG_E B = B NRO
RNE => EORG_NE B =B NRO
B => ' ' B | Lambda
NRO => 0 NR | 1 NR | 2 NR | 3 NR | 4 NR | 5 NR | 6 NR | 7 NR | 8 | NR | 9 NR | ? 
NR => 0 NR | 1 NR | 2 NR | 3 NR | 4 NR | 5 NR | 6 NR | 7 NR | 8 NR | 9 NR | Lambda 

Palabras reservadas: COD_ERROR, EORG_E, EORG_NE

     */
    class AnalizadorSintáctico
    {
        public const string COD_ERROR = AnalizadorLéxico.COD_ERROR;
        public const string ERROR_ORG_EXCLUYENTE = AnalizadorLéxico.ERROR_ORG_EXCLUYENTE;
        public const string ERROR_ORG_NO_EXCLUYENTE = AnalizadorLéxico.ERROR_ORG_NO_EXCLUYENTE;
        public const string ERROR_ORG_INDEFINIDO = AnalizadorLéxico.ERROR_ORG_INDEFINIDO;
        public const string VALOR_ERROR_INDEFINIDO = AnalizadorLéxico.VALOR_ERROR_INDEFINIDO;

        enum EEstadoAnálisis
        {
            Nada = 0,
            PrimerToken = 1,
            SegundoToken = 2,
            TercerToken = 3,
            CuartoToken = 4

        }

        public Sentencia GetSentenciaCabecera(string source)
        {
            return GetSentencia(source, AnalizadorLéxico.PatternValorCorErrorCacebecera);
        }

        public Sentencia GetSentenciaDetalle(string source)
        {
            return GetSentencia(source, AnalizadorLéxico.PatternValorCorErrorDetalle);
        }

        private static Sentencia GetSentencia(string source, string patternValorCodError)
        {
            Sentencia sentencia = null;
            AnalizadorLéxico analizadorLéxico = new AnalizadorLéxico();
            Token token = analizadorLéxico.GetToken(source, 0, patternValorCodError);
            EEstadoAnálisis eEstadoAnálisis = EEstadoAnálisis.PrimerToken;
            switch (token.Id)
            {
                case AnalizadorLéxico.COD_ERROR:
                    sentencia = new Sentencia();
                    sentencia.DescripciónBloque = token.DescripciónBloque;
                    sentencia.ErrorNatura = token.Id + AnalizadorLéxico.SÍMBOLO_IGUAL + token.Valor;
                    sentencia.ValorErrorNatura = token.Valor;
                    // Se cierra el bloque, por si, mas adelante, no encontrara ningún Error ORG.
                    sentencia.PosFinSentencia = token.PosFinBloque;

                    token = analizadorLéxico.GetToken(source, token.PosFinBloque + 1, patternValorCodError);
                    if (token != null)
                    {
                        if ((token.Id != AnalizadorLéxico.COD_ERROR) &&
                            (token.Id != AnalizadorLéxico.DESCRIPCIÓN)
                            )
                        {
                            sentencia.ErrorOrg = token.Id + AnalizadorLéxico.SÍMBOLO_IGUAL + token.Valor;
                            sentencia.ValorErrorAfip = token.Valor;
                            sentencia.PosFinSentencia = token.PosFinBloque;
                            sentencia.TipoErroOrg = token.Id;
                        }
                    }
                    break;

                case AnalizadorLéxico.ERROR_ORG_EXCLUYENTE:
                    CargarDatosBásicosTokenErrorAfipEnSentencia(ref sentencia, token);
                    sentencia.TipoErroOrg = ERROR_ORG_EXCLUYENTE;
                    break;

                case AnalizadorLéxico.ERROR_ORG_NO_EXCLUYENTE:
                    CargarDatosBásicosTokenErrorAfipEnSentencia(ref sentencia, token);
                    sentencia.TipoErroOrg = ERROR_ORG_NO_EXCLUYENTE;
                    break;

                case AnalizadorLéxico.ERROR_ORG_INDEFINIDO:
                    CargarDatosBásicosTokenErrorAfipEnSentencia(ref sentencia, token);
                    sentencia.TipoErroOrg = ERROR_ORG_INDEFINIDO;
                    break;

                case AnalizadorLéxico.DESCRIPCIÓN:
                    sentencia = new Sentencia();
                    sentencia.DescripciónBloque = token.DescripciónBloque;
                    sentencia.PosFinSentencia = token.PosFinBloque;
                    break;
            }

            return sentencia;
        }

        private static void CargarDatosBásicosTokenErrorAfipEnSentencia(ref Sentencia sentencia, Token token)
        {
            sentencia = new Sentencia();
            sentencia.DescripciónBloque = token.DescripciónBloque;
            sentencia.ErrorOrg = token.Id + AnalizadorLéxico.SÍMBOLO_IGUAL + token.Valor;
            sentencia.ValorErrorAfip = token.Valor;
            sentencia.PosFinSentencia = token.PosFinBloque;
        }
    }
}
