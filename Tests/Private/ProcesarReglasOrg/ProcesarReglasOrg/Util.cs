using System;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using System.Collections.Generic;


namespace ProcesarReglasOrg
{
    class Util
    {
        
        public static string GetCellText(TableCell cell)
        {
            List<string> textParts = new List<string>();
            foreach (Text textElement in cell.Descendants<Text>())
            {
                textParts.Add(textElement.Text);
            }
            return string.Join("", textParts);
        }

        public static List<BloqueCod_error> SplitBloque(string cadenaOrigen, string cadenaCOD_ERROR)
        {
            const int POS_NULA = -1;
            List<BloqueCod_error> listBloqueCodError = new List<BloqueCod_error>();
            int posIni = POS_NULA, posIniBloque = 0, posFinBloque = 0;
            int indexIni = cadenaOrigen.IndexOf(cadenaCOD_ERROR, posIni + 1);
            while(indexIni != POS_NULA)
            {
                BloqueCod_error bloqueCod_Error = new BloqueCod_error();
                bloqueCod_Error.PosIniBloqueCodError = posIniBloque;
                bloqueCod_Error.PosFinBloqueCodError = indexIni - 1;
                bloqueCod_Error.DescripBloqueCodErrorRule = 
                    cadenaOrigen.Substring(bloqueCod_Error.PosIniBloqueCodError, bloqueCod_Error.PosFinBloqueCodError - bloqueCod_Error.PosIniBloqueCodError + 1);

                bloqueCod_Error.PosIniCodError = indexIni;
                int cantCaracteresLeídosValorCod_error = 0;
                bloqueCod_Error.ErrorCode = GetValorCod_error(cadenaOrigen, cadenaCOD_ERROR, indexIni, ref cantCaracteresLeídosValorCod_error);

                int cantCaracteresLeídosValorAfipRule = 0;
                (bloqueCod_Error.DescripAfipRule, bloqueCod_Error.ValorAfipRule) =
                    GetErrorAfip(cadenaOrigen, cadenaCOD_ERROR, indexIni + cadenaCOD_ERROR.Length, cantCaracteresLeídosValorCod_error, 
                            ref cantCaracteresLeídosValorAfipRule);

                int indexFin = cadenaOrigen.IndexOf(cadenaCOD_ERROR, indexIni + 1);
                if(indexFin > indexIni)
                {
                    // Faltan procesar
                    posIniBloque = indexIni + cadenaCOD_ERROR.Length + cantCaracteresLeídosValorCod_error + bloqueCod_Error.DescripAfipRule.Length +
                                cantCaracteresLeídosValorAfipRule;

                    indexIni = indexFin;
                }
                else
                {
                    // Llegó al último
                    indexIni = POS_NULA;
                }
                listBloqueCodError.Add(bloqueCod_Error);
            }

            return listBloqueCodError;
        }

        private static string GetValorCod_error(string cadenaOrigen, string cadenaSeparador, int indexIni, ref int cantCaracteresLeídosValorCod_error)
        {
            string valorCod_error = string.Empty;
            string auxiliar = cadenaOrigen.Substring(indexIni + cadenaSeparador.Length);

            cantCaracteresLeídosValorCod_error = GetValorBuscandoDesdeIndex(ref valorCod_error, auxiliar);

            return valorCod_error;
        }

        private static int GetValorBuscandoDesdeIndex(ref string valorCod_error, string auxiliar)
        {
            valorCod_error = BloqueCod_error.VALOR_ERROR_INDEFINIDO;
            int cantCaracteresLeídosValorCod_error = 0;
            if( (auxiliar.Length > 0) && (auxiliar[0].ToString() == BloqueCod_error.VALOR_ERROR_INDEFINIDO))
            {
                cantCaracteresLeídosValorCod_error = 1;
            }
            else
            {
                int i = 0;
                while (i < auxiliar.Length && char.IsDigit(auxiliar[i]))
                {
                    if (i == 0)
                    {
                        valorCod_error = string.Empty;
                    }
                    valorCod_error += auxiliar[i];

                    i++;
                }
                cantCaracteresLeídosValorCod_error = i;
            }
            return cantCaracteresLeídosValorCod_error;
        }

        private static (string, string) GetErrorAfip(string cadenaOrigen, string cadenaSeparador, int indexIni, int cantCaracteresLeídosValorCod_error, 
                ref int cantCaracteresValorAfipRule)
        {
            string descripAfipRule = string.Empty;
            string valorAfipRule = string.Empty;

            string auxiliar = cadenaOrigen.Substring(indexIni + cantCaracteresLeídosValorCod_error).TrimStart();

            if (auxiliar.StartsWith(BloqueCod_error.ERROR_ORG_EXCLUYENTE))
            {
                descripAfipRule = BloqueCod_error.ERROR_ORG_EXCLUYENTE;
                valorAfipRule = GetValorAfipRule(out cantCaracteresValorAfipRule, descripAfipRule, valorAfipRule, auxiliar);
            }
            else if (auxiliar.StartsWith(BloqueCod_error.ERROR_ORG_NO_EXCLUYENTE))
            {
                descripAfipRule = BloqueCod_error.ERROR_ORG_NO_EXCLUYENTE;
                valorAfipRule = GetValorAfipRule(out cantCaracteresValorAfipRule, descripAfipRule, valorAfipRule, auxiliar);
            }
            else if (auxiliar.StartsWith(BloqueCod_error.ERROR_ORG_INDEFINIDO))
            {
                descripAfipRule = BloqueCod_error.ERROR_ORG_INDEFINIDO;
                valorAfipRule = GetValorAfipRule(out cantCaracteresValorAfipRule, descripAfipRule, valorAfipRule, auxiliar);
            }
            
            return (descripAfipRule, valorAfipRule);
        }

        private static string GetValorAfipRule(out int cantCaracteresValorCod_error, string descripAfipRule, string valorAfipRule, string auxiliar)
        {
            cantCaracteresValorCod_error = GetValorBuscandoDesdeIndex(ref valorAfipRule, auxiliar.Substring(0 + descripAfipRule.Length));
            return valorAfipRule;
        }
    }


}
