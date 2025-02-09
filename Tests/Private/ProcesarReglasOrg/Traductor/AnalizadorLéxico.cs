using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Traductor
{
    class AnalizadorLéxico
    {
        private const int POS_NULA = -1;
        public const string SÍMBOLO_IGUAL = "=";
        public const string DESCRIPCIÓN = "d";
        public const string COD_ERROR = "COD_ERROR";
        public const string ERROR_ORG_EXCLUYENTE = "R_ORG_E";
        public const string ERROR_ORG_NO_EXCLUYENTE = "R_ORG_NE";
        public const string ERROR_ORG_INDEFINIDO = "R_ORG";
        public const string VALOR_ERROR_INDEFINIDO = "?";


        private static string PatternValorCodNumérico
        {
            get
            {
                return @"\d+";
            }
        }
        public static string PatternValorCorErrorCacebecera
        {
            get
            {
                return PatternValorCodNumérico;
            }
        }

        public static string PatternValorCorErrorDetalle
        {
            get
            {
                return @"[A-Za-z]\d|\d[A-Za-z]|\d+";
            }
        }

        public Token GetToken(string source, int posActual, string patternValorCodError)
        {
            if (posActual > source.Length)
            {
                return null;
            }

            (int posIniCodError, int posFinCodError, string valorCodError) = FindFirstOcurr(source, posActual, COD_ERROR, patternValorCodError);
            (int posIniErrorAfipExcluyente, int posFinErrorAfipExcluyente, string valorErrorAfipExcluyente) =
                FindFirstOcurr(source, posActual, ERROR_ORG_EXCLUYENTE, PatternValorCodNumérico);
            (int posIniErrorAfipNoExcluyente, int posFinErrorAfipNoExcluyente, string valorErrorAfipNoExcluyente) =
                FindFirstOcurr(source, posActual, ERROR_ORG_NO_EXCLUYENTE, PatternValorCodNumérico);
            (int posIniErrorAfipIndefinido, int posFinErrorAfipIndefinido, string valorErrorAfipIndefinido) =
                FindFirstOcurr(source, posActual, ERROR_ORG_INDEFINIDO, PatternValorCodNumérico);

            return RetornarToken(source, posActual, posIniCodError, posFinCodError, valorCodError, posIniErrorAfipExcluyente, 
                posFinErrorAfipExcluyente, valorErrorAfipExcluyente, posIniErrorAfipNoExcluyente, posFinErrorAfipNoExcluyente, 
                valorErrorAfipNoExcluyente, posIniErrorAfipIndefinido, posFinErrorAfipIndefinido, valorErrorAfipIndefinido);
        }

        private static Token RetornarToken(string source, int posActual,  int posIniCodError, int posFinCodError, string valorCodError, int posIniErrorAfipExcluyente, 
                                int posFinErrorAfipExcluyente, string valorErrorAfipExcluyente, int posIniErrorAfipNoExcluyente, 
                                int posFinErrorAfipNoExcluyente, string valorErrorAfipNoExcluyente, int posIniErrorAfipIndefinido, int posFinErrorAfipIndefinido, string valorErrorAfipIndefinido)
        {
            //string source = sourceParam.Substring(posActual);

            if ((posIniCodError != int.MaxValue) &&
                            (posIniCodError <= posIniErrorAfipExcluyente) &&
                            (posIniCodError <= posIniErrorAfipNoExcluyente) &&
                            (posIniCodError <= posIniErrorAfipIndefinido))
            {
                return new Token(COD_ERROR, valorCodError, source.Substring(posActual, posIniCodError - posActual).Trim(), posFinCodError);
            }
            if ((posIniErrorAfipExcluyente != int.MaxValue) &&
                (posIniErrorAfipExcluyente < posIniCodError) &&
                (posIniErrorAfipExcluyente < posIniErrorAfipNoExcluyente) &&
                (posIniErrorAfipExcluyente < posIniErrorAfipIndefinido))
            {
                return new Token(ERROR_ORG_EXCLUYENTE, valorErrorAfipExcluyente, 
                            source.Substring(posActual, posIniErrorAfipExcluyente - posActual).Trim(), posFinErrorAfipExcluyente);
            }

            if ((posIniErrorAfipNoExcluyente != int.MaxValue) &&
                (posIniErrorAfipNoExcluyente < posIniCodError) &&
                (posIniErrorAfipNoExcluyente < posIniErrorAfipExcluyente) &&
                (posIniErrorAfipNoExcluyente < posIniErrorAfipIndefinido))
            {
                return new Token(ERROR_ORG_NO_EXCLUYENTE, valorErrorAfipNoExcluyente, 
                    source.Substring(posActual, posIniErrorAfipNoExcluyente - posActual).Trim(), posFinErrorAfipNoExcluyente);
            }

            if ((posIniErrorAfipIndefinido != int.MaxValue) &&
                (posIniErrorAfipIndefinido < posIniCodError) &&
                (posIniErrorAfipIndefinido < posIniErrorAfipExcluyente) &&
                (posIniErrorAfipIndefinido < posIniErrorAfipNoExcluyente))
            {
                return new Token(ERROR_ORG_INDEFINIDO, valorErrorAfipIndefinido, 
                    source.Substring(posActual, posIniErrorAfipIndefinido - posActual).Trim(), posFinErrorAfipIndefinido);
            }

            return new Token(DESCRIPCIÓN, string.Empty, source, source.Length - 1);
        }

        private (int, int, string) FindFirstOcurr(string sourceParam, int posActual, string buscado, string patternValorCodError)
        {
            string source = sourceParam.Substring(posActual);

            string pattern = $@"{buscado}\s*{SÍMBOLO_IGUAL}\s*({patternValorCodError}|\{VALOR_ERROR_INDEFINIDO})";

            Match match = Regex.Match(source, pattern);

            if (match.Success)
            {
                int index = posActual + match.Index;
                int endIndex = index + match.Length - 1;
                return (index, endIndex, match.Groups[1].Value);
            }
            return (int.MaxValue, int.MaxValue, null);
        }
    }
}
